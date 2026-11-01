---
description: Actualiza solo el siguiente paso concreto en CONTEXT.md
---
Lee CONTEXT.md y la sección de la etapa en curso de 02-DOCS/plan-refactor-edicion.md.

1. Ejecuta `git log --oneline` y averigua cuál fue el último commit del plan que se hizo.
2. En CONTEXT.md cambia solo la sección "Siguiente paso concreto": pon el siguiente commit pendiente del plan, con su número y una descripción de una línea.
3. Si el último commit hecho era el final de la etapa en curso, no cambies nada y dime que conviene usar /cerrar-etapa.

Reglas:
- No toques ninguna otra sección de CONTEXT.md.
- No modifiques el plan ni ningún otro fichero.
- No hagas commit.
- Al terminar, enséñame el diff de CONTEXT.md.