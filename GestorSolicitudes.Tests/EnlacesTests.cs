namespace GestorSolicitudes.Tests;

using GestorSolicitudes.Helpers;

public class EnlacesTests
{
    [Theory]
    [InlineData("https://example.com/oferta")]
    [InlineData("http://example.com")]
    [InlineData("https://example.com/@usuario")]
    public void EsWeb_AceptaHttpYHttps(string url) =>
        Assert.True(Enlaces.EsWeb(url));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("file:///C:/Windows/System32/calc.exe")]
    [InlineData("javascript:alert(1)")]
    [InlineData("example.com")]
    [InlineData("mailto:alguien@example.com")]
    [InlineData("https://usuario:clave@example.com/oferta")]
    [InlineData("ms-msdt:/id PCWDiagnostic")]
    [InlineData(@"C:\Windows\System32\calc.exe")]
    [InlineData(@"\\servidor\recurso")]
    [InlineData("//host")]
    [InlineData("https%3A%2F%2Fexample.com")]
    public void EsWeb_RechazaLoQueNoEsWeb(string? url) =>
        Assert.False(Enlaces.EsWeb(url));

    [Fact]
    public void Canonica_DevuelveLaFormaCanonica() =>
        Assert.Equal("https://example.com/Oferta", Enlaces.Canonica("HTTPS://EXAMPLE.COM/Oferta"));
}
