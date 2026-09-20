namespace GestorSolicitudes.Tests;

using GestorSolicitudes.ViewModels;
using System.Text.RegularExpressions;
using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

/// <summary>
/// Reglas de arquitectura: las capas solo pueden depender "hacia abajo". El corazón de la
/// regla es el típico MainViewModel: los ViewModels hablan con Models/Data/Helpers, pero
/// jamás con las Vistas ni para lo contrario. Cualquiera que toque esto sin avisar se entera
/// corriendo los tests.
/// </summary>
public class ArquitecturaTests
{
    private const string AppNamespace = @"^GestorSolicitudes\.";

    private static readonly Architecture Arquitectura =
        new ArchLoader().LoadAssemblies(typeof(MainViewModel).Assembly).Build();

    private static readonly IReadOnlyCollection<IType> CapaModelos = EnNamespace("Models");
    private static readonly IReadOnlyCollection<IType> CapaDatos = EnNamespace("Data");
    private static readonly IReadOnlyCollection<IType> CapaHelpers = EnNamespace("Helpers");
    private static readonly IReadOnlyCollection<IType> CapaConversores = EnNamespace("Converters");
    private static readonly IReadOnlyCollection<IType> CapaVistaModelos = EnNamespace("ViewModels");
    private static readonly IReadOnlyCollection<IType> CapaVistas = EnNamespace("Views");

    // Lo que vive en el raíz (App, Localizacion, Idioma...): el "arranque" que sí puede
    // referenciar todo lo demás (crea la ventana, inyecta diccionarios de recursos).
    private static readonly IReadOnlyCollection<IType> Nucleo =
        Arquitectura.Types.Where(t => Regex.IsMatch(t.FullName, @"^GestorSolicitudes\.[^.]+$")).ToArray();

    private static IReadOnlyCollection<IType> EnNamespace(string capa) =>
        Arquitectura.Types
            .Where(t => Regex.IsMatch(t.FullName, $@"^{AppNamespace}{capa}\.[^.]+$"))
            .ToArray();

    private static void Verificar(ICanBeEvaluated regla)
    {
        IReadOnlyCollection<string>? fallos = regla.Evaluate(Arquitectura)
            .Where(resultado => !resultado.Passed)
            .Select(resultado => resultado.ToString())
            .ToArray();
        Assert.True(fallos.Count == 0, string.Join(Environment.NewLine, fallos));
    }

    [Fact]
    public void LosModelosNoDependenDeNingunaOtraCapa() =>
        Verificar(
            Types().That().Are(CapaModelos)
                .Should().NotDependOnAny(CapaDatos)
                .AndShould().NotDependOnAny(CapaHelpers)
                .AndShould().NotDependOnAny(CapaConversores)
                .AndShould().NotDependOnAny(CapaVistaModelos)
                .AndShould().NotDependOnAny(CapaVistas));

    [Fact]
    public void LosDatosSoloDependenDeLosModelos() =>
        Verificar(
            Types().That().Are(CapaDatos)
                .Should().NotDependOnAny(CapaHelpers)
                .AndShould().NotDependOnAny(CapaConversores)
                .AndShould().NotDependOnAny(CapaVistaModelos)
                .AndShould().NotDependOnAny(CapaVistas));

    [Fact]
    public void LosHelpersNoDependenDeDatosNiVistas() =>
        Verificar(
            Types().That().Are(CapaHelpers)
                .Should().NotDependOnAny(CapaDatos)
                .AndShould().NotDependOnAny(CapaConversores)
                .AndShould().NotDependOnAny(CapaVistaModelos)
                .AndShould().NotDependOnAny(CapaVistas));

    [Fact]
    public void LosConversoresNoDependenDeDatosNiDeLasVistas() =>
        Verificar(
            Types().That().Are(CapaConversores)
                .Should().NotDependOnAny(CapaDatos)
                .AndShould().NotDependOnAny(CapaVistaModelos)
                .AndShould().NotDependOnAny(CapaVistas));

    [Fact]
    public void LosVistaModelosNoDependenDeVistasNiConversores() =>
        Verificar(
            Types().That().Are(CapaVistaModelos)
                .Should().NotDependOnAny(CapaVistas)
                .AndShould().NotDependOnAny(CapaConversores));

    [Fact]
    public void NingunaOtraCapaQueNoSeaElNucleoDependeDeLasVistas() =>
        Verificar(
            Types().That().AreNot(CapaVistas).And().AreNot(Nucleo)
                .Should().NotDependOnAny(CapaVistas));
}