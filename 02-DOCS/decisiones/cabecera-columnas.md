# Fondo de las cabeceras de columnas

Los nombres de las columnas se perdían contra las filas blancas: el `DataGrid`
arrastraba el fondo del tema de Windows. Las cabeceras llevan ahora un gris de la
paleta (`Cabecera`, #E2E8F0), el texto en `Texto` y `SemiBold` a 14 px —las filas
van a 13— y un borde inferior y derecho en `Borde`. El estilo `CabeceraColumna`
(`DataGridColumnHeader`) vive en `App.xaml` y se aplica con `ColumnHeaderStyle` en
el `DataGrid` de `MainWindow.xaml`; el `TextBlock` de cada `HeaderTemplate` hereda
el color y el grosor.

## Decisiones

**Nada de `HeaderStyle` por columna.** Si una columna necesita un tooltip, va
dentro de su `HeaderTemplate`. Un estilo propio no se suma al
`ColumnHeaderStyle` del `DataGrid`, lo sustituye: esa columna se queda sin fondo
ni fuente. Es lo que pasó dos veces con la columna **Días**, que llevaba su
propio `HeaderStyle` para el tooltip. El tooltip se mudó al `TextBlock` de su
`HeaderTemplate` y el estilo desapareció. Lo vigila un test.

**El gris comparte tono con `Borde` pero tiene su propia clave.** Así, si algún
día se oscurece el borde de las tablas, las cabeceras no se mueven con él.

**Sin plantilla propia.** La de WPF ya pinta `Background` y `Padding`, así que
bastan unos setters.

**14 px porque lo pidió el usuario tras probar la app**, en tres pasos:
12 → 13 → 14.

## Qué lo protege

`PestanasTests.AppXaml_LosEstilosConPlantillaSeComponenAlPintar` es el único test
que puede crear la `Application` (WPF solo admite una por AppDomain), así que
concentra las comprobaciones del estilo: que existe, que su fondo es el brush
`Cabecera`, su texto `Texto`, el grosor `SemiBold`, y que una
`DataGridColumnHeader` con ese estilo pinta ese fondo una vez compuesto el
layout. El mismo test guarda la regresión del `HeaderStyle` por columna: el XAML,
sin comentarios, no puede contenerlo.
