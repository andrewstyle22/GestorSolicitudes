# Documentos en la base de datos

El CV y la carta de presentación se guardaban como ficheros en `%APPDATA%\GestorSolicitudes\adjuntos`, y la base de datos solo apuntaba a ellos con una ruta. Pasan a guardarse dentro de la base de datos, en una tabla aparte. Esta decisión forma parte de `plan-refactor-ponytail.md` (Etapa E).

## Decisiones

**Una tabla aparte, no dos columnas en `Solicitudes`.** `Recargar` lee todas las candidaturas completas en cada recarga. Con dos documentos de hasta 2 MB por fila, cada recarga arrastraría cientos de megabytes. La tabla `DocumentosSolicitud` tiene:

| Columna | Tipo | Notas |
|---------|------|-------|
| `SolicitudId` | entero | Clave primaria y clave foránea a `Solicitudes(Id)`, con borrado en cascada |
| `Cv` | BLOB, anulable | Contenido del CV |
| `Carta` | BLOB, anulable | Contenido de la carta |

La entidad no tiene propiedad de navegación desde `Solicitud`: nada la carga al leer una candidatura.

**El contenido se lee solo cuando hace falta.** Abrir el documento lanza una consulta sin seguimiento que proyecta únicamente su columna; ver una candidatura no toca la tabla. Guardar cambios en una candidatura existente sí carga su fila de `DocumentosSolicitud` para actualizarla o vaciarla.

**`Solicitud` conserva el nombre del documento.** `NombreOriginalCv` y `NombreOriginalCarta` siguen en `Solicitudes`; `TieneCv` y `TieneCarta` salen de que ese nombre no sea nulo. Así la interfaz muestra el nombre y activa el botón sin cargar los bytes.

**El contenido nuevo vive en el editor.** Al adjuntar, el fichero se lee entero a memoria (máximo 2 MB) y se guarda en `CvContenido` o `CartaContenido` del editor, junto con su nombre. No se copia nada a disco. Al guardar, se escribe en `DocumentosSolicitud`: en una candidatura existente, en el mismo `SaveChanges` que el resto de sus datos; en una nueva, en un segundo `SaveChanges` inmediato, porque la fila necesita el `Id` que asigna el primero. Cancelar o descartar solo tiene que soltar la memoria: ya no hay copias en disco que borrar.

**Límite de 2 MB.** 2 × 1024 × 1024 bytes; un fichero de exactamente ese tamaño se admite. Si pesa más, no se adjunta y sale el aviso «El documento no puede ser mayor a 2 megabytes». El límite se aplica al adjuntar y no a los ficheros que ya existían.

**Abrir un documento escribe un fichero temporal.** El visor trabaja con una ruta, así que los bytes se dejan en `%TEMP%\GestorSolicitudes\adjuntos\<guid>\<nombre saneado>`. La carpeta se limpia al cerrar la aplicación y al arrancar, por si la sesión anterior terminó mal.

## Qué aporta y qué no

- **No ahorra espacio.** Un BLOB ocupa los mismos bytes que el fichero, y además la base de datos crece. Lo que aporta es que toda la información queda en un solo fichero (la copia de seguridad es copiar `GestorSolicitudes.db`), que no hay adjuntos huérfanos en disco y que borrar una candidatura borra sus documentos.
- **Cuesta más al abrir.** Cada apertura lee el BLOB y escribe un temporal.

## Migración de los ficheros existentes

Al arrancar, después de `EsquemaDb`:

1. Se crea la tabla si no existe. Si la base ya es de esta versión y no tiene `RutaCv` ni `RutaCarta`, no se hace nada.
2. Con SQL (no con el modelo) se leen las filas que aún tienen `RutaCv` o `RutaCarta`.
3. Para cada fichero que existe, se copian sus bytes a `DocumentosSolicitud` dentro de una transacción y se comprueba que el tamaño coincide. `NombreOriginalCv` o `NombreOriginalCarta` se rellenan con el nombre del fichero si estaban vacíos, y la ruta pasa a `NULL`.
4. Un fichero que ya no existe deja el campo vacío y se registra en el fichero de errores.
5. Solo tras confirmar la transacción se borra el fichero migrado si está dentro de la carpeta `adjuntos` (nunca una ruta arbitraria de la base) y, si queda vacía, la carpeta.

Es idempotente: una fila ya migrada tiene la ruta a `NULL` y no se vuelve a tocar. Los ficheros de más de 2 MB, si los hubiera, también se migran: el límite se aplica al adjuntar. Esta parte se mejorará en otra refactorización.

## Consecuencias

- Las columnas `RutaCv` y `RutaCarta` dejan de existir en el modelo, pero siguen en las bases de datos antiguas sin usarse; no se borran porque quitar columnas en SQLite es más arriesgado que dejarlas.
- `GestorAdjuntos`, que llevaba la cuenta de las copias en disco, desaparece.
- El CSV no incluía las rutas, así que no cambia.

## Fuera de alcance

- Vista previa de imágenes en el visor.
- Versionado de documentos (guardar el CV anterior tras una reapertura).
