# Selector de columnas de la tabla de candidaturas

## Intent

La tabla tiene 10 columnas y en pantallas normales no caben: el usuario tiene que
desplazarse en horizontal para ver fechas. Además el buscador se lleva casi media
fila de la barra de filtros. Se reduce el buscador a la mitad y se añade un botón
"Columnas" que decide cuáles de las 6 columnas secundarias se ven, y se recuerda
la elección al reabrir la app.

## Scope

- **Sí**: buscador a ~240 px; botón "Columnas" con checkboxes; 6 columnas
  ocultables (Días, 1ª respuesta, Entrevista, Seguimiento, Interés, Portal);
  preferencia en `%APPDATA%\GestorSolicitudes\columnas.txt`.
- **Sí**: Empresa, Puesto, Estado y Enviada siempre visibles (no son ocultables).
- **No**: reordenar columnas, guardar anchos, mover el botón fuera de la fila
  de filtros, ni Groups para varias columnas a la vez.

## Checklist

- [x] `ColumnasHelper` lee y escribe `columnas.txt`; si no hay fichero, todas
      visibles. Check: `dotnet test` → 6 tests de `ColumnasHelper` en verde.
- [x] `ColumnaItem` expone `Visible` (dos vías) y `Nombre` del diccionario.
      Check: 3 tests del item.
- [x] El ViewModel expone las 6 columnas, guarda al cambiar y avisa a la vista.
      Check: 5 tests con ruta temporal (nunca `%APPDATA%`).
- [x] El buscador ocupa la mitad; el botón y los filtros quedan a la derecha.
      Check: test temporal con la ventana real: el buscador mide 241 px.
- [x] Tercera vuelta (corrección del usuario): el buscador vuelve a su sitio de
      origen, a la izquierda, y los dos desplegables (Columnas y Estados) se van a
      su derecha, con el hueco detrás.
      Check: test temporal por posición real en X: buscador < Columnas < Estados;
      el buscador entre 241 y 360 px; la ✕ pegada al buscador.
- [x] Cuarta vuelta: los desplegables no basta con que estén detrás del buscador;
      el grupo entero (Columnas, Estados, las dos casillas) va pegado al borde
      derecho y el hueco se queda en medio.
      Check: test temporal: buscador a x < 40; más de 40 px de hueco entre el
      buscador y Columnas; el grupo termina a menos de 300 px del borde del panel.
- [x] Placeholder "Buscar..." en el buscador: un `TextBlock` encima que se ve
      solo con el campo vacío (el `TextBox` de WPF no tiene Placeholder).
      Check: test temporal con la ventana real: visible vacío, oculto al escribir,
      vuelve al borrar. Clave `Filtro.Buscar` en es/en/de.
- [x] El botón abre una lista de checkboxes que se cierra al pulsar fuera.
      Check: test temporal con la ventana real (6 checkboxes, popup abierto al
      pulsar el botón). El cierre por fuera es lo que-documenta-`StaysOpen=false`;
      verificado que la propiedad vale false, no el clic en sí.
- [x] Marcar una columna la oculta de verdad en el `DataGrid`.
      Check: test temporal: desmarcar "Entrevista" colapsó la columna 6 y marcarla
      la restauró. Confirma que el `x:Name` de cada `DataGridColumn` genera campo.
- [x] Clave `Columnas.Boton` en los tres idiomas. Check: test de paridad.
- [x] `dotnet test` en verde (167) y `dotnet format --verify-no-changes` sin cambios.

## Evidence

- `dotnet test` → `Con error: 0, Superado: 167`.
- `dotnet format GestorSolicitudes.slnx --verify-no-changes` → sin salida.
- Test temporal (no entregado, borrado después) con la ventana real montada:
  10 columnas, las 6 ocultables visibles al arrancar; desmarcar "Entrevista"
  → `Visibility.Collapsed` en la columna 6; volver a marcar → `Visible`; el
  `ToggleButton` con estilo `BotonDesplegable` abre el `Popup`; 6 checkboxes
  dentro;   `StaysOpen == false`; el buscador mide entre 241 y 360 px según el
  ancho de la ventana; buscador pegado a la izquierda (x < 40) y grupo de
  filtros pegado al borde derecho del panel.
- El test del estilo se fusionó en `PestanasTests.AppXaml_LosEstilosConPlantillaSeComponenAlPintar`:
  WPF solo admite una `Application` por AppDomain y ese test ya la creaba.

## Decisiones

- ToggleButton + Popup en vez de ComboBox: el ComboBox cierra el desplegable al
  pulsar un elemento, y evitarlo exige un truco en code-behind; el Popup se cierra
  al pulsar fuera solo y no se cierra al marcar.
- `x:Name` de las columnas: el compilador XAML genera un campo por cada una, y el
  code-behind las alcanza con un `switch` (si se renombra una columna, no compila).
- Se renombró `Primera_respuesta` → `PrimeraRespuesta` para que la clave guardada,
  el nombre del diccionario (`Columna.PrimeraRespuesta`) y el `x:Name` coincidan.

