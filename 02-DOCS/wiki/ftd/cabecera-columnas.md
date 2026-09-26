# Fondo de las cabeceras de columnas

## Intent

Los nombres de las columnas se perdían contra las filas blancas: el `DataGrid`
arrastraba el fondo del tema de Windows. El usuario pide un fondo "un poco más
oscuro" para que los nombres resalten, y después que la letra sea algo mayor.

## Scope

- **Sí**: un gris de la paleta (`Cabecera`, #E2E8F0) de fondo, texto en `Texto`,
  `SemiBold` a 13 px (el mismo tamaño que las filas) y un borde inferior y a la
  derecha en `Borde`.
- **No**: cabeceras en dos líneas, indicador de orden propio, agrupar por color las
  columnas fijas frente a las ocultables.

## Checklist

- [x] `SolidColorBrush` `Cabecera` en la paleta de `App.xaml`.
- [x] Estilo `CabeceraColumna` (`DataGridColumnHeader`) en `App.xaml`, aplicado con
      `ColumnHeaderStyle` en el `DataGrid` de `MainWindow.xaml`.
- [x] El `TextBlock` de cada `HeaderTemplate` hereda el color y el grosor.
- [x] Segunda vuelta (el usuario): la columna **Días** se quedaba sin el fondo. Tenía
      su propio `HeaderStyle` (para el tooltip) y un estilo por columna **sustituye**
      al `ColumnHeaderStyle` del `DataGrid` en vez de heredarlo. El tooltip se mudó al
      `TextBlock` de su `HeaderTemplate` y el `HeaderStyle` desapareció.
- [x] Segunda vuelta: letra de 12 a 13 px.
- [x] `dotnet test` en verde (141) y `dotnet format --verify-no-changes` sin cambios.

## Evidence

- `dotnet test` → `Con error: 0, Superado: 141`.
- Aserciones añadidas a `PestanasTests.AppXaml_LosEstilosConPlantillaSeComponenAlPintar`
  (el único test que puede crear la `Application`): el estilo existe, su fondo es el
  brush `Cabecera`, su texto es `Texto`, el grosor es `SemiBold` y una
  `DataGridColumnHeader` con ese estilo pinta ese fondo tras componerse el layout.
- Guarda de regresión en ese mismo test: el XAML, sin comentarios, no puede contener
  un `HeaderStyle` por columna (es el fallo que se coló dos veces).
- Test temporal (no entregado) con la ventana real: las 11 `DataGridColumnHeader` del
  visual tree llevan el fondo `Cabecera`, 13 px y `SemiBold`. La ventana tiene que
  estar `Show()` para que el DataGrid genere las cabeceras; con solo
  `Measure/Arrange` no hay ninguna.

## Decisiones

- El gris es el mismo tono que `Borde` pero con su propia clave: si algún día se
  oscurece el borde de las tablas, las cabeceras no se mueven con él.
- Sin plantilla propia: la de WPF ya pinta el `Background` y el `Padding`, así que
  solo hacen falta setters.
- **Regla**: nada de `HeaderStyle` por columna. Si una columna necesita un tooltip (u
  otra cosa), va en su `HeaderTemplate`; un estilo propio se come el del `DataGrid`
  y esa columna se queda sin estilo. Lo vigila el test.
- 13 px y no 14: queda a la altura de las filas, que van a 13.


