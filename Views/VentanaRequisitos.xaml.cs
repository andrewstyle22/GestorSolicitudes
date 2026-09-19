// <copyright file="VentanaRequisitos.xaml.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

namespace GestorSolicitudes.Views;

using System.Windows;

public partial class VentanaRequisitos : Window
{
    public VentanaRequisitos(string texto)
    {
        this.InitializeComponent();
        this.TextoRequisitos.Text = texto;
        this.Owner = Application.Current.MainWindow;
    }

    private void Cerrar_Click(object? sender, RoutedEventArgs e) => this.Close();
}