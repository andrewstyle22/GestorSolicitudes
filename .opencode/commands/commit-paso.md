---
description: Prepara y hace el commit del paso actual cumpliendo las reglas del plan
---
Lee CONTEXT.md para saber cuál es el paso actual.

0. Ejecuta `git status --short` y `git log --oneline -3`. Si hay cambios sin commitear que no pertenecen a este paso, o el último commit ya corresponde a este paso, para y dímelo.
1. Carga la skill sonarcloud-gate y aplica solo su "Verificación local antes de cada commit": no leas referencia.md salvo que algo falle.
2. Ejecuta `dotnet test`. Debe estar en verde sin avisos (los avisos son errores).
3. Ejecuta `dotnet format GestorSolicitudes.slnx --verify-no-changes`. Si falla, formatea y vuelve a comprobar.
4. Haz el barrido de patrones de la skill sobre los ficheros que has cambiado y corrige lo que encuentres.
5. Si hay XAML nuevo, comprueba que el `<Page>` está declarado en el csproj. Si hay texto de UI nuevo, comprueba que tiene clave en es/en/de en `Traducciones.cs`.
6. Si el commit añade código nuevo, comprueba que lleva sus tests en este mismo commit.
7. Actualiza CONTEXT.md solo en "Siguiente paso concreto" (el siguiente paso del plan) y en la línea de la etapa en curso (añade este paso a los hechos). No marques como hechos pasos que no tengan su propio commit.
8. Haz el commit, con CONTEXT.md incluido, con mensaje en español: el que indica el plan para este paso. Una sola idea por commit.

Reglas:
- NO hagas push. Solo lo haces si te lo pido.
- Fuera del paso 7, no modifiques CONTEXT.md ni ningún otro fichero que no sea del paso.
- No fusiones pasos del plan en un solo commit.
- Si algo falla y no sabes arreglarlo, para y dime qué ha pasado antes de seguir.
- Al terminar, dime el hash del commit y los ficheros que incluye.