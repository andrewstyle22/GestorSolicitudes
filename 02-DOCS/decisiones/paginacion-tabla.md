# Paginación de la tabla de candidaturas

Con 200 candidaturas la tabla era una lista interminable: había que desplegar
hasta el pie y hacer scroll. Ahora se ve por páginas, de diez filas por defecto,
con desplegable de 5, 10, 20 o 30, cuatro flechas (primera, anterior, siguiente,
última), el rango (`1-10 de 47`) y botones que se deshabilitan solos.

El recorte lo hace el ViewModel: `Paginar(lista)` deja `Solicitudes` con la
página actual y recalcula rango y botones, y `Recargar()` filtra y recorta, de
modo que filtros y flechas comparten un único camino. Las flechas recortan el
destino a las páginas que existen, cambiar el tamaño de página repagina
manteniendo la actual si todavía existe, y una lista vacía muestra `0 de 0` sin
páginas ni flechas activas.

## Decisiones

**Recortar en el ViewModel, no filtrar sobre la vista.** Un filtro sobre la vista
sí respetaría el orden global, pero obliga a recalcular el rango de posiciones
ordenadas y se rompe en cuanto el `DataGrid` reordena. Con el recorte, las
flechas reutilizan `Recargar()` y no hay listas intermedias que se desfasen.

**WPF no pagina las vistas de colección.** No existen `PageSize`, `ItemCount` ni
`MoveToPage` en `ICollectionView` ni en `ListCollectionView` —eso era Silverlight—
así que no hay nada nativo que usar. Comprobado además que
`CollectionViewSource.GetDefaultView` devuelve una `ListCollectionView` sin
miembros de paginación, y que su `Filter` se aplica **después** del orden, que es
el motivo por el que no se paginó con un filtro sobre la vista.

**La exportación a CSV no pasa por `Solicitudes`** —consulta la base—, así que
sigue sacando la lista entera.

## Pendiente de decidir

Al pinchar una cabecera para ordenar, el orden se aplica solo a las filas de la
página actual. Arreglarlo pasa por que el ViewModel sea el dueño del orden:
replicar la ordenación por tipo de columna y fiarse de la cabecera únicamente
para pintarla.
