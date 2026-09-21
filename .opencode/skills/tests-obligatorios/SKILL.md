---
name: tests-obligatorios
description: Aplica cuando se va a CREAR, EDITAR o ELIMINAR una función o método en este repo (GestorSolicitudes): todo cambio de función obliga a crear, editar o eliminar su test xUnit correspondiente y a dejar `dotnet test` en verde. Gatillos: "crear función", "añadir método", "modificar", "refactorizar", "borrar/quitar función", "tests", "dotnet test", "no romper los tests".
---

# Tests obligatorios con cada cambio de función

En este repo todo cambio de **función/método** (crear, editar, eliminar) arrastra
su **test xUnit** correspondiente. Un commit que toque lógica y deje los tests
igual es un error: o falta un test nuevo, o el existente quedó mintiendo sobre
el comportamiento real.

## Reglas

- **Crear una función** (`public`, `internal` o privada con lógica) → añade un
  test que cubra su comportamiento. Los estáticos puros de `Helpers` y la lógica
  del ViewModel son los casos fáciles: un `[Fact]` o `[Theory]` con el caso clave.
- **Editar una función** → actualiza sus tests: si cambió el comportamiento, se
  ajusta el test o se añade el caso nuevo. Si solo se renombró, se renombra el test.
- **Eliminar una función** → elimina su test (o el caso). Prohibido dejar tests
  que referencian código que ya no existe.
- La tarea no termina hasta que `dotnet test` está en verde.

## Dónde va el test

- Mira antes el fichero `*Tests.cs` que ya cubre esa clase o método
  (`ViewModelTests`, `ViewModelStaticTests` —los estáticos, incluidos los de
  `CsvHelper`—, `ConvertidorTests`, `LocalizacionTests`, `SolicitudTests`,
  `AdjuntosHelperTests`, ...) y añade junto a los existentes. No crees un fichero
  nuevo si ya existe el natural.
- Nombre del test: `Método_ComportamientoEsperado`.
- Un método con varias ramas → `[Theory]`/`[InlineData]` con los casos borde, no
  un `[Fact]` por rama.

## Material que usan los tests de este repo

- Tests del ViewModel: construye el ViewModel con un contexto SQLite temporal
  (`using var db = TestDb.NuevoContexto();`). Nunca toques `%APPDATA%`.
- Tests que dependen del dispatcher WPF: `[StaFact]` (Xunit.StaFact).
- Los tests ven miembros `internal` vía `InternalsVisibleTo("GestorSolicitudes.Tests")`:
  no hace falta hacerlos `public` para probarlos.

## Verificar

- `dotnet test`, todo en verde.
- `dotnet format GestorSolicitudes.slnx --verify-no-changes` si has tocado formato.
- Si la app está abierta y bloquea `bin\`, compila a una carpeta temporal:
  `dotnet test -p:OutDir=$env:TEMP\gestor_build`.