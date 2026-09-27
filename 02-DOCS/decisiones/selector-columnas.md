# Selector de columnas de la tabla de candidaturas

La tabla tiene diez columnas y en pantallas normales no caben: había que
desplazarse en horizontal para ver las fechas. El buscador se reduce a la mitad
(~240 px) y un botón «Columnas» decide cuáles de las seis columnas secundarias
se ven, recordando la elección al reabrir la app. Empresa, Puesto, Estado y
Enviada siempre visibles; ocultables son Días, 1ª respuesta, Entrevista,
Seguimiento, Interés y Portal.

## Decisiones

**ToggleButton + Popup, no ComboBox.** El ComboBox cierra el desplegable al
pulsar un elemento, y evitarlo exige un truco en code-behind. El Popup se cierra
solo al pulsar fuera y no se cierra al marcar. Que la lista de checkboxes se
cierre al pulsar fuera es justo lo que documenta `StaysOpen=false`.

**`x:Name` en cada `DataGridColumn`.** El compilador XAML genera un campo por
cada una y el code-behind las alcanza con un `switch`: si se renombra una
columna, deja de compilar.

**Se renombró `Primera_respuesta` → `PrimeraRespuesta`** para que la clave
guardada, el nombre del diccionario (`Columna.PrimeraRespuesta`) y el `x:Name`
coincidan.

**La preferencia vive en `%APPDATA%\GestorSolicitudes\columnas.txt`** y
`ColumnasHelper` la lee y escribe; si no hay fichero, todas las columnas están
visibles. Los tests nunca tocan `%APPDATA%`: usan rutas temporales.

## Colocación

El buscador se queda a la izquierda, pegado al borde, y todo el grupo de filtros
—botón Columnas, desplegable de Estados y las dos casillas— pegado al borde
derecho del panel, con el hueco en medio. Las medidas que lo fijan: buscador a
x < 40 px, más de 40 px de hueco entre el buscador y Columnas, y el grupo
terminando a menos de 300 px del borde. El buscador mide entre 241 y 360 px según
el ancho de la ventana.

El placeholder «Buscar...» es un `TextBlock` colocado encima, visible solo con el
campo vacío, porque el `TextBox` de WPF no tiene placeholder. Los textos de estas
columnas van por `Localizacion` en los tres idiomas: `Columnas.Boton` y
`Filtro.Buscar`.

## Fuera de alcance

Reordenar columnas, guardar anchos, mover el botón fuera de la fila de filtros y
agrupar columnas con Groups.
