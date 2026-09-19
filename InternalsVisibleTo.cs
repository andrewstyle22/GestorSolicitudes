// <copyright file="InternalsVisibleTo.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

using System.Runtime.CompilerServices;

// Permite que el ensamblado de tests vea los miembros internal de la aplicación
// (métodos de parseo de CSV, fechas, etc.) sin hacerlos públicos.
[assembly: InternalsVisibleTo("GestorSolicitudes.Tests")]