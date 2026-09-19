// <copyright file="Evento.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace GestorSolicitudes.Models;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Hito dentro de un proceso de selección: una llamada, una prueba, una entrevista...
/// </summary>
public class Evento
{
    public int Id { get; set; }

    public int SolicitudId { get; set; }

    public Solicitud? Solicitud { get; set; }

    public DateTime Fecha { get; set; } = DateTime.Today;

    public TipoEvento Tipo { get; set; } = TipoEvento.Nota;

    [MaxLength(500)]
    public string Descripcion { get; set; } = string.Empty;
}
