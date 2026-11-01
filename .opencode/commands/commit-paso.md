---
description: Prepara y hace el commit del paso actual cumpliendo las reglas del plan
---
Lee CONTEXT.md para saber cuál es el paso actual.

1. Carga la skill sonarcloud-gate y aplica solo su "Verificación local antes de cada commit": no leas referencia.md salvo que algo falle.
2. Ejecuta `dotnet test`. Debe estar en verde sin avisos (los avisos son errores).
3. Ejecuta `dotnet format GestorSolicitudes.slnx --verify-no-changes`. Si falla, formatea y vuelve a comprobar.
4. Haz el barrido de patrones de la skill sobre los ficheros que has cambiado y corrige lo que encuentres.
5. Si hay XAML nuevo, comprueba que el `<Page>` está declarado en el csproj. Si hay texto de UI nuevo, comprueba que tiene clave en es/en/de en `Traducciones.cs`.
6. Si el commit añade código nuevo, comprueba que lleva sus tests en este mismo commit.
7. Haz el commit con mensaje en español, el que indica el plan para este paso. Una sola idea por commit.

Reglas:
- NO hagas push. Solo lo haces si te lo pido.
- No modifiques CONTEXT.md; para eso están /avanzar-paso y /cerrar-etapa.
- Si algo falla y no sabes arreglarlo, para y dime qué ha pasado antes de seguir.
- Al terminar, dime el hash del commit y los ficheros que incluye.