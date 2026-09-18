# Gestor de candidaturas

Aplicación de escritorio para llevar el control de las ofertas de empleo a las que te inscribes:
a quién, cuándo, qué te contestaron y cuándo toca volver a insistir.

## Stack

- **.NET 8** + **WPF** (MVVM)
- **Entity Framework Core 8** sobre **SQLite** (fichero local, sin servidor)
- **CommunityToolkit.Mvvm** para `ObservableObject` y `RelayCommand` por generadores de código
- **Hardcodet.NotifyIcon.Wpf** para el icono y los avisos de la bandeja del sistema
- **LiveCharts2** (skia) para la gráfica de embudo mensual

## Cómo ejecutarlo

```bash
cd GestorSolicitudes
dotnet restore
dotnet run
```

O abre `GestorSolicitudes.csproj` en Visual Studio y pulsa F5.

La base de datos se crea sola en el primer arranque en:

```
%APPDATA%\GestorSolicitudes\solicitudes.db
```

Está fuera de `bin/` a propósito: puedes recompilar, mover o reinstalar la app sin perder los datos.
Ese fichero `.db` es todo tu histórico, así que cópialo de vez en cuando a OneDrive o a un pendrive.
En el arranque, la app añade con un `ALTER TABLE` las columnas nuevas que no existan en una base
ya creada por una versión anterior (porque `EnsureCreated()` no versiona el esquema).

## Qué se guarda de cada candidatura

**La oferta**: empresa, puesto, enlace directo (con botón para abrirlo en el navegador), portal de
origen, ubicación, modalidad, tecnologías pedidas, horquilla salarial, tu pretensión y un nivel de
interés del 1 al 5 (con estrellas clicables).

**El proceso**: estado dentro del embudo, fecha de envío, fecha de la primera contestación, día de
la entrevista, fecha de cierre, fecha de próximo seguimiento y el texto de la respuesta de la empresa.
Si dejas el próximo seguimiento en blanco, al guardar se propone automáticamente una fecha 7 días
después del envío, para que el aviso de "seguimiento pendiente" no dependa de que te acuerdes de
rellenarlo a mano.

**El contacto**: nombre y email del recruiter o la persona técnica.

**El historial**: una lista de hitos con fecha y tipo (llamada, prueba técnica, entrevista, oferta,
rechazo...). Esto es lo que hace que funcione cuando un proceso tiene tres entrevistas en vez de una:
el campo "día de la entrevista" te da la próxima de un vistazo y el historial guarda todas.

**Los adjuntos**: el CV y la carta de presentación concretos que enviaste a cada oferta. Al pulsar
*Adjuntar* se **copian** a `%APPDATA%\GestorSolicitudes\adjuntos` con un nombre interno único (no se
enlaza el original, así que sobreviven aunque muevas o borres el fichero de origen); el nombre que
ves en pantalla es el original tal como lo elegiste, no el nombre interno. Se borran junto con la
candidatura, al pulsar *Quitar*, o si cancelas una candidatura nueva sin llegar a guardarla.

## Detalles que quizá no se vean a simple vista

- Las filas con **seguimiento vencido** (la fecha de próximo seguimiento ya pasó y el proceso sigue
  abierto) se pintan en ámbar, y hay un filtro para ver solo esas. Es el disparador para escribir
  el clásico "¿hay alguna novedad sobre el proceso?".
- La cabecera calcula **tasa de respuesta** y **media de días hasta la primera contestación**. Con
  treinta o cuarenta candidaturas esos dos números te dicen bastante sobre si el CV está filtrando
  bien o si estás disparando a ofertas equivocadas.
- Las candidaturas cerradas se muestran en gris, y el filtro *Solo abiertas* las esconde.
- **Duplicar** (en el panel de detalle de una candidatura ya guardada) crea una candidatura nueva con
  los mismos datos de la oferta —empresa, puesto, ubicación, tecnologías, salario, interés— pero sin
  fechas de proceso, contacto ni adjuntos: pensado para reaplicar a la misma empresa en otro puesto,
  o a una oferta calcada de otra fuente.
- **Exportar CSV** saca todo con `;` y UTF-8 con BOM, así que Excel en español lo abre en columnas
  directamente sin el asistente de importación.
- **Importar LinkedIn** lee el CSV de "Mis candidaturas" que exporta LinkedIn (Ajustes → Privacidad de
  datos → *Obtener una copia de tus datos*) y crea candidaturas con empresa, puesto, fecha, ubicación,
  estado traducido y enlace a la oferta. Las filas que ya existan (misma empresa + puesto + fecha) se
  omiten y al final te dice cuántas entraron y cuántas se saltaron.
- **Icono en la bandeja del sistema**: cerrar la ventana (la X, Alt+F4) no cierra la aplicación, la
  oculta a la bandeja — solo *Salir* desde el menú del icono la cierra de verdad. Cada 5 minutos (y a
  los pocos segundos de arrancar) revisa si hay seguimientos vencidos y avisa con un globo junto al
  reloj de Windows; el menú del icono permite reabrir la ventana, comprobar manualmente y salir. Cada
  candidatura avisa una sola vez por sesión.
- **Gráfica de embudo** (botón *Ver gráfica* de la cabecera): enviadas → respondidas → entrevistas →
  ofertas de los últimos 12 meses, contadas por el historial (hito "Solicitud enviada", primera
  contestación, hitos de entrevista y de oferta), así cada mes cuenta lo que pasó ese mes. Se refresca
  sola si guardas, borras o importas candidaturas mientras está abierta.
- Atajos: `Ctrl+N` nueva candidatura, `Ctrl+S` guardar.

## Qué no hacer una vez (lección aprendida)

El icono de la bandeja usa `System.Drawing`, por eso el proyecto tiene `UseWindowsForms=true` además
de WPF. Para no arrastrar el infierno de ambigüedades (`Application`, `MessageBox`, `Color`...),
el `.csproj` quita los usings implícitos de WinForms y de `System.Drawing` con `<Using Remove=...>`.

## Estructura

```
Models/          Solicitud, Evento y enums con [Description] para los textos de la UI
Data/            AppDbContext (SQLite, EnsureCreated + ALTER TABLE de columnas nuevas)
ViewModels/      MainViewModel: filtros, CRUD, métricas, embudo, adjuntos, duplicar e importación
Views/           MainWindow: lista maestra + panel de detalle + gráfica + icono de bandeja
Converters/      enum → texto, null → Visibility, estado → color, interés → color de estrella
Helpers/         EnumHelper: lee los [Description] por reflexión
                 AdjuntosHelper: copia/borra CV y carta en %APPDATA%\...\adjuntos
```

## Decisiones que conviene conocer antes de tocarlo

**Un solo `DbContext` durante toda la sesión.** Es una app monousuario, así que se aprovecha el
change tracking de EF: el formulario edita la entidad que ya está en seguimiento y *Guardar* es un
`SaveChanges()` limpio. *Cancelar* desengancha todo el `ChangeTracker` y recarga de disco. Si algún
día esto se convierte en multiusuario o multiventana, el patrón correcto pasa a ser un contexto por
operación con `IDbContextFactory`.

**Las entidades son POCOs sin `INotifyPropertyChanged`.** Por eso la lista se recarga entera
después de cada guardado, y el panel de detalle se "repinta" reasignando `Edicion = null; Edicion =
actual;` tras adjuntar un fichero o tocar una estrella, en vez de refrescar solo ese campo. Con unos
cientos de registros ni se nota; si algún día crece, el cambio natural es meter `ObservableObject`
en `Solicitud`.

**`EnsureCreated()` en lugar de migraciones.** Va bien para empezar, pero no versiona el esquema:
si añades una propiedad nueva al modelo, la tabla existente no se actualiza sola. Cuando quieras
evolucionarlo sin borrar datos:

```bash
dotnet tool install --global dotnet-ef
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet ef migrations add Inicial
dotnet ef database update
```

y cambia `EnsureCreated()` por `db.Database.Migrate()` en `App.xaml.cs`.

**Los `MessageBox` viven en el ViewModel.** No es MVVM de manual; lo ortodoxo sería un
`IDialogService` inyectado. Para una herramienta personal es ruido, pero si la usas como proyecto
de portfolio, extraer ese servicio es la primera mejora que un revisor va a buscar.

## Ideas para seguir

- Recordatorios incluso cuando la app está cerrada del todo (arrancar un proceso en segundo plano con Windows)
- Un `.ico` propio para la bandeja, en vez del icono genérico de aplicación de Windows
- Migrar a Avalonia si algún día quieres ejecutarlo también en macOS o Linux
