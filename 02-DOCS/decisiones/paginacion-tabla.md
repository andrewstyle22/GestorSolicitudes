# Paginación de la tabla de candidaturas

## Intent

Con 200 candidaturas la tabla es una lista interminable: hay que desplegar hasta el
pie y-scrollar. El usuario quiere verla por páginas, de diez filas por defecto, con
posibilidad de 5, 10, 20 o 30 y cuatro flechas (primera, anterior, siguiente,
última).

## Scope

- **Sí**: desplegable de filas por página (5, 10, 20, 30; 10 por defecto), cuatro
  flechas, texto de rango (`1-10 de 47`) y botones que se deshabilitan solos.
- **Sí**: las flechas y el desplegable repaginan la lista ya filtrada, y si la lista
  se encoge la página se recorta.
- **No**: recordar el tamaño de página entre sesiones, elegir páginas concretas
  escribiendo el número, nirecordar qué página se estaba viendo.

## Checklist

- [x] WPF no pagina las vistas de colección: comprobado que `ICollectionView` y
      `ListCollectionView` no tienen `PageSize`, `ItemCount` ni `MoveToPage` (eso
      era Silverlight). El recorte se hace en el ViewModel.
- [x] `Paginar(lista)` deja `Solicitudes` con la página actual y recalcula rango y
      botones. `Recargar()` filtra y recorta: un solo camino para filtros y flechas.
- [x] Las cuatro flechas recortan el destino a las páginas que existen.
- [x] Cambiar el tamaño de página repagina manteniendo la actual si sigue existiendo.
- [x] Lista vacía: `0 de 0`, sin páginas, sin flechas activas.
- [x] 6 claves nuevas (`Paginacion.*`) en es/en/de. Check: test de paridad.
- [x] `dotnet test` en verde (141) y `dotnet format --verify-no-changes` sin cambios.

## Evidence

- `dotnet test` → `Con error: 0, Superado: 141`.
- `dotnet format GestorSolicitudes.slnx --verify-no-changes` → sin salida.
- 7 tests nuevos en `ViewModelTests` (páginas por defecto, lista vacía, siguiente y
  recorte de la última, primera/última, atrás en la primera, cambio de tamaño,
  filtrado que reduce el total).
- Test temporal (no entregado) con la ventana real sobre la base del usuario: los
  cuatro botones y el desplegable existen, sus bindings apuntan a
  `IrAInicioCommand`/`IrAtrasCommand`/`IrAdelanteCommand`/`IrAlFinalCommand` y a
  `PuedeIrAInicio`/`PuedeRetroceder`/`PuedeAvanzar`/`PuedeIrAlFinal`, el desplegable
  a `TamanosPagina`/`TamanoPagina` y el texto a `RangoPagina`; con 13 candidaturas en
  la base el rango sale `1-10 de 13`.
- Sonda en un proyecto aparte: `CollectionViewSource.GetDefaultView` devuelve una
  `ListCollectionView` sin miembros de paginación, y el `Filter` se aplica **después**
  del orden (motivo por el que no se paginó con un filtro sobre la vista).

## Decisiones

- Recortar en el ViewModel y no con un filtro sobre la vista: el filtro sí respetaría
  el orden global, pero hay que recalcular el rango de posiciones ordenadas y se
  rompe en cuanto el DataGrid reordena. Con el recorte, las flechas reutilizan
  `Recargar()` y no hay listas intermedias que se desfasen.
- **Consecuencia asumida**: al pinchar una cabecera para ordenar, el orden se aplica
  solo a las filas de la página actual. Arreglarlo pasa por que el ViewModel sea el
  dueño del orden (replicar el orden por tipo de columna y fiarse de la cabecera solo
  para pintarla). Pendiente de decisión del usuario.
- La exportación a CSV no pasa por `Solicitudes` (consulta la base), así que sigue
  sacando la lista entera.
