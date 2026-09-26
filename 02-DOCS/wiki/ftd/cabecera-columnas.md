# Fondo de las cabeceras de columnas

## Intent

Los nombres de las columnas se perdÃ­an contra las filas blancas: el `DataGrid`
arrastraba el fondo del tema de Windows. El usuario pide un fondo "un poco mÃ¡s
oscuro" para que los nombres resalten.

## Scope

- **SÃ­**: un gris de la paleta (`Cabecera`, #E2E8F0) de fondo, texto en `Texto`,
  `SemiBold` a 12 px y un borde inferior y a la derecha en `Borde`.
- **No**: cabeceras en dos lÃ­neas, indicador de orden propio, agrupar por color las
  columnas fijas frente a las ocultables.

## Checklist

- [x] `SolidColorBrush` `Cabecera` en la paleta de `App.xaml`.
- [x] Estilo `CabeceraColumna` (`DataGridColumnHeader`) en `App.xaml`, aplicado con
      `ColumnHeaderStyle` en el `DataGrid` de `MainWindow.xaml`.
- [x] El `TextBlock` de cada `HeaderTemplate` hereda el color y el grosor.
- [x] `dotnet test` en verde (141) y `dotnet format --verify-no-changes` sin cambios.

## Evidence

- `dotnet test` â†’ `Con error: 0, Superado: 141`.
- Aserciones aÃ±adidas a `PestanasTests.AppXaml_LosEstilosConPlantillaSeComponenAlPintar`
  (el Ãºnico test que puede crear la `Application`): el estilo existe, su fondo es el
  brush `Cabecera`, su texto es `Texto`, el grosor es `SemiBold` y una
  `DataGridColumnHeader` con ese estilo pinta ese fondo tras componerse el layout.

## Decisiones

- El gris es el mismo tono que `Borde` pero con su propia clave: si algÃºn dÃ­a se
  oscurece el borde de las tablas, las cabeceras no se mueven con Ã©l.
- Sin plantilla propia: la de WPF ya pinta el `Background` y el `Padding`, asÃ­ que
  solo hacen falta setters.

