# Auditoría Ponytail

Salida de la skill *ponytail* sobre el proyecto tras las etapas 0 a 5 de `plan-refactor-edicion.md`. Se conserva tal cual, sin los bloques de código que envolvían cada párrafo. El plan que la atiende es `plan-refactor-ponytail.md`.

---

I have finished tracing. No files were changed.

What this repo does: a Windows desktop app (WPF, .NET 8) that keeps a personal
list of job applications ("candidaturas"). It stores everything in one local
SQLite file in %APPDATA%\GestorSolicitudes\ and copies CV/cover-letter files
into %APPDATA%\GestorSolicitudes\adjuntos. Around that core it adds: a
filterable, paged table; a detail panel with three tabs (offer,

follow-up/history, attachments); a fixed list of states with a reopen action and
a close reason; a funnel chart and a few metrics; CSV export; a tray icon that
reminds you of due follow-ups; and three UI languages (es/en/de). Tests are
xUnit plus an architecture test that enforces the layering.
Assumed load (the only load I judge against): one Windows user, one app window,
one local database with dozens to a few hundred applications, all work on the UI
thread (the single thread that draws the screen), no server, no network calls,
no background jobs beyond a 5-minute reminder timer.
Must fix

1. Saving "Oferta aceptada" from an already-closed row kills the app
MainViewModel.cs:831-846 → SolicitudEdicionViewModel.cs:458-459 →
Solicitud.cs:261-265; combo at Views/PestanaSeguimiento.xaml:9-13; no handler in
App.xaml.cs:19-61.

- What this is: the state drop-down lists every state, with no filtering.
CambiarEstado deliberately throws an InvalidOperationException (a hard error)
when a closed row is moved to "Oferta aceptada". Saving goes TryGuardar →
AplicarCambiosDelEditor → AplicarA → AplicarEstado → CambiarEstado, and only
Persistir (the actual write) has a try/catch around it.

- Problem: concrete case: a row in "Descartado por la empresa" with "Oferta
aceptada" picked in the combo → Guardar throws on the UI thread with no handler,
so the process dies. The open editor buffer (and any unsaved text) is gone. The
model test CambiarEstado_CerradaAOfertaAceptada_Lanza proves the throw; no
ViewModel test covers the save path, which is why it survived.
- Fix: block it before writing: in Validar() (or AplicarEstado) refuse "closed →
Oferta aceptada" with the existing "faltan datos" style message, and
hide/disable that combo entry when the original state is closed. Add one
ViewModel test that saves this exact combination and asserts the panel stays
open.

- If we skip it: the app exits without a message on a reachable two-click path,
and users learn to distrust the save button.

2. A failed save is written later, from nowhere

MainViewModel.cs:856-866 (reload after failure), 894-899 (prompt skipped), 294-
321 / 764-780 / 782-801 (leaving a row), 1064-1081 (ChangeTracker.Clear only on
"Descartar"); SolicitudEdicionViewModel.cs:409.

- What this is: EF Core keeps a "change tracker" — the in-memory list of objects
it will write on the next SaveChanges. When a save fails, the code reloads the
editor buffer from the entity, which sets HayCambios = false (:409). The entity
itself is still queued in the tracker.

- Problem: concrete case: you fill a new row, Save fails (file briefly locked by
a backup), you give up, click another row and save that instead —
ResolverCambiosPendientes sees no pending changes, Seleccionar only attaches and
never clears, and the next successful SaveChanges inserts the abandoned row too
(or applies the abandoned edit to an existing one). BuscarCandidaturaRepetida
reads db.Solicitudes.Local (:1010), so the ghost row also starts producing false
"duplicate application" warnings. The existing test
Guardar_SiSaveChangesFalla_...PermiteReintentar covers only the retry, never
leaving.

- Fix: one line in ResolverCambiosPendientes: treat a dirty tracker as unsaved
work — if (this.Editor?.HayCambios != true && !
this.db.ChangeTracker.HasChanges()) return true;. "Descartar" already calls
ChangeTracker.Clear(), so the abandoned row disappears. Add one test: fail a
save, select another row, save, assert only one row exists.

- If we skip it: silent duplicate or unrequested rows in the database, produced
by a recovery path the user thinks is safe.

3. Ctrl+S and Ctrl+N ignore the box you are typing in

Views/MainWindow.xaml.cs:42-44 (shortcuts), Views/MainWindow.xaml:669,677 and
the three detail tabs (bindings without UpdateSourceTrigger).

- What this is: a "binding" is the link between a text box and a C# property. By

default a text box only pushes its text when it loses focus ("LostFocus").
Ctrl+S runs the save command directly, without moving focus first.
- Problem: concrete case: you type a new company name in the Empresa box and
press Ctrl+S. Editor.Empresa still holds the old text, the save writes the old
value, the panel closes and the typed name is gone. Ctrl+N is worse: HayCambios
is still false, so there is not even a "save your changes?" prompt — the text
vanishes silently. Clicking the Guardar button works only because the click
moves focus first.

- Fix: add UpdateSourceTrigger=PropertyChanged to the editable text boxes in the
detail tabs (one attribute each, no new code), so every keystroke reaches the
buffer. Keep the existing validation attributes; they work the same with this
trigger.

- If we skip it: a documented shortcut silently loses data, which is the kind of
bug users blame on "the app forgot my edit".
Should fix

4. A second copy of the app silently overwrites the first
App.xaml.cs:19-61 (no single-instance lock), MainViewModel.cs:517-519 (main
query), AppDbContext.cs:25-47 (one long-lived context).

- What this is: nothing stops two copies of the app from opening the same
database file. The app keeps one database context for the whole session, so rows
it has already read stay cached in memory ("identity resolution": a second query
returns the old object, not the new row values).

- Problem: concrete case: the app lives in the tray, you double-click the
shortcut and get a second window. The second window changes a state; the first
window's Recargar still shows the old value (only new rows appear — that query
has no AsNoTracking, unlike SeguimientosVencidosAhora at :641-643), and the next
save from the first window writes its stale copy back. No warning, no conflict
detection.

- Fix: add a named Mutex (a Windows-wide lock) in OnStartup; if it is already
taken, show the existing window and exit — about five lines, no new dependency.
Alternatively make Recargar use AsNoTracking() so at least reads are fresh. Pick
the mutex: it matches the documented single-user design.
- If we skip it: a plausible everyday action (double-click while minimised)
produces edits that quietly disappear.

5. Dates can contradict each other and metrics go negative

SolicitudEdicionViewModel.cs:342-364 (validation) and 461-465; Solicitud.cs:493-
506 (DiasProceso, DiasHastaRespuesta); used at EstadisticasSolicitud.cs:24-25
and CsvExportador.cs:28.

- What this is: each date field is validated alone. Nothing checks that "first
response", "interview" or "close" are on or after the application date.
- Problem: concrete case: you set Fecha solicitud = 10 May but Fecha primera
respuesta = 1 May (easy when copying from an email thread). DiasHastaRespuesta
returns −9, the "average days to response" metric drops, the Días column shows a
negative number, and the CSV exports it.

- Fix: one cross-field rule in Validar(): no date may be before FechaSolicitud.
Reuse the existing error-message pattern with one new translation key in
es/en/de, and add a test per field.

- If we skip it: the numbers the whole app exists to show can be nonsense, with
no warning.

6. Two CI pipelines build and test the same pull request
.github/workflows/dotnet-desktop.yml:1-35 and
.github/workflows/sonarcloud.yml:1-38.
- What this is: CI ("continuous integration") is the automated build that runs
on every push. Both workflows run windows-latest, install .NET, build and run
the full test suite on the same pull request.

- Problem: every PR pays the build and test cost twice, and two independent
results for the same commit can disagree (one green, one red) for no product
reason. The first workflow's only unique job is the format check and the 90%
coverage threshold; the second needs the tests for Sonar coverage anyway.
- Fix: delete dotnet-desktop.yml and move its dotnet format ... --verify-no-
changes step plus the threshold switches into the SonarCloud workflow. One file
fewer, same guarantees.

- If we skip it: wasted minutes on every change, and a red/green split that
makes "is this PR OK?" ambiguous.

# `Nice to have`

7. The schema helper builds its connection string by hand
EsquemaDb.cs:21 and :72 versus the correct AppDbContext.cs:44-46.

- What this is: $"Data Source={ruta}" is not escaped, while AppDbContext already
uses SqliteConnectionStringBuilder for exactly this reason.

- Problem: a path containing ; or a quote would produce a broken connection
string and abort startup. On Windows ; is not a legal file-name character, so
this cannot happen today — it is duplication with a live trap, not a live bug.
- Fix: add one internal static string CadenaConexion(string ruta) next to
RutaBaseDatos and call it from both places. Net zero lines, one source of truth.
- If we skip it: the next person who moves the database to a path the builder
tolerates will hit a startup failure the other code already knows how to avoid.
8. The attachment viewer dumps unknown file types as text
Views/VisorAdjuntoWindow.xaml.cs:59-76, DocumentoHelper.cs:26-39, filter at
Traducciones.cs:182 (offers "Todos los archivos (.)").

- What this is: the viewer switches on the extension; the default branch does
File.ReadAllTextAsync on whatever it gets, and the DOCX/ODT path does ReadToEnd
on the zip entry with no size cap.

- Problem: concrete case: you attach a scanned CV as .jpg (the file picker
allows it and AdjuntosHelper.Copiar accepts any extension); "Ver" shows
thousands of replacement characters instead of an image. A corrupted or hostile
DOCX whose word/document.xml expands to gigabytes is read fully into memory; the
try/catch only turns the out-of-memory crash into an "cannot preview" message
after the damage.

- Fix: smallest useful change: let default show the existing "no se puede
previsualizar" notice instead of reading bytes (deletes a line), and cap the
decompressed length before ReadToEnd. Image preview can wait until someone asks.
- If we skip it: users see garbage where a preview should be, and a single bad
file can freeze the app.

9. The reminder timer clears the table's selected row but leaves the editor open
MainViewModel.cs:238-249 (Paginar replaces the whole ObservableCollection), 294-
300 (Seleccionar(null) returns early), Views/MainWindow.xaml.cs:89-99,138 (timer
→ Recargar).

- What this is: every 5 minutes the reminder check calls Recargar, which builds
a new list; WPF then clears SelectedItem, and the null is deliberately ignored,
so solicitudAbierta and the editor stay while SolicitudSeleccionada becomes
null.

- Problem: concrete case: you are reading a row's history; the timer fires; the
table loses its highlight while the detail panel still shows the row. If you
then click the same row, ReferenceEquals(value, this.solicitudAbierta) is false,
so you get a spurious "unsaved changes" prompt for changes you do not have.
- Fix: in Paginar, after rebuilding the list, put the selection back on
solicitudAbierta if it is still on the page (the same reponiendoSeleccion` trick
the code already uses).

- If we skip it: a small, recurring UI inconsistency that makes the app look
like it forgot what you were doing.

10. CSV formula guard only looks at the first character
TextoHelper.cs:31-48, specifically :41.

- What this is: Escapar puts an apostrophe before text starting with =, +, -, @,
tab or carriage return, so a spreadsheet shows it as text instead of running it.
It checks valor[0] of the original string.

- Problem: a value like " =2+2" (leading space) is not neutralised. In practice
Excel treats a leading space before = as plain text, so I could not build a real
exploit; the gap is real but low risk, and the check is on the untrimmed string
while the output is trimmed of newlines.

- Fix: test the first non-space character, or valor.TrimStart() before the
check. One line, and it removes the argument entirely.

- If we skip it: a theoretical hole stays open in the one place the code
explicitly claims to defend against it.

Verdict: the architecture is sound for what it is — thin model with business
methods, one editor buffer, dialogs behind IDialogoService, layering enforced by
a test — and it is unusually well covered for a personal project. The risk is
concentrated in three save/navigation paths where a user action reaches a throw
or a still-dirty DbContext, not in the file layout. Fix findings 1–3 before

calling the branch done; 4–6 are small, cheap hardening; 7–10 can wait.
Lean: -40 lines, -0 dependencies possible (nearly all of it is deleting one of
the two CI workflows; the three Must-fix items add roughly 25 lines. All four
runtime packages are load-bearing, so none can go).
Not checked: I did not run dotnet test or the app, so every finding is from
reading the code and the existing tests, not from a live repro; I did not
observe the LostFocus behaviour at runtime (it is the documented WPF default);
no profiling, no large-database test (the "hundreds of rows" assumption is
untested above that); no accessibility pass (screen reader, tab order,
contrast); no review of installer, backup or code-signing concerns; localization
parity was not re-verified by hand (it is enforced by an existing test); and I
did not exercise SQLite with two processes, or confirm in Excel that a leading
space really blocks formula evaluation. No shortcut: or ponytail: comments exist
in the product code, so no finding was suppressed by that rule.


