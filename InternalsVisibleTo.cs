using System.Runtime.CompilerServices;

// Permite que el ensamblado de tests vea los miembros internal de la aplicación
// (métodos de parseo de CSV, fechas, etc.) sin hacerlos públicos.
[assembly: InternalsVisibleTo("GestorSolicitudes.Tests")]