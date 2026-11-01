---
description: Cierra la etapa en curso y prepara la siguiente
---
Lee CONTEXT.md y la sección de la etapa en curso de 02-DOCS/plan-refactor-edicion.md.

1. Ejecuta `git log --oneline` y comprueba que los commits de esa etapa están hechos. Si falta alguno, dímelo y no cambies nada.
2. Actualiza CONTEXT.md:
   - Marca la etapa en curso como ✅ con un resumen de una sola línea.
   - Marca la siguiente etapa como 🔄 (en curso).
   - En "Siguiente paso concreto", pon el primer commit de la etapa siguiente según el plan.
   - En "Decisiones vigentes", añade solo las decisiones tomadas que afecten al código futuro, cada una en una línea con su motivo. Quita las que ya no apliquen.
   - Actualiza "Puntos de control pendientes" si alguno se ha resuelto.
3. No copies detalles técnicos de la etapa cerrada a CONTEXT.md. El detalle ya está en el plan y en git.

Reglas:
- CONTEXT.md debe ocupar menos de 300 palabras.
- No modifiques el plan ni ningún otro fichero.
- No hagas commit.
- Al terminar, enséñame el diff de CONTEXT.md para revisarlo.