namespace GestorSolicitudes.Views;

using System;
using System.Windows.Controls;

/// <summary>Pestaña "Adjuntos e historial" del panel de detalle.</summary>
public partial class PestanaAdjuntos : UserControl
{
    public PestanaAdjuntos() => this.InitializeComponent();

    /// <summary>
    /// La fila pulsada la reenvía tal cual: quien abre el visor vive en la ventana, que es
    /// la que tiene el servicio de diálogos y puede ser su propietaria.
    /// </summary>
    public event EventHandler? AbrirSolicitado;

    private void AbrirAdjunto_Click(object sender, EventArgs e) => this.AbrirSolicitado?.Invoke(sender, e);
}
