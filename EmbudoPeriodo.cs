namespace GestorSolicitudes.Models;

/// <summary>
/// Una fila del embudo: un periodo (mes, semana o día) con las cuatro series (enviadas,
/// respondidas, entrevistas, ofertas) y la altura de cada barra, ya escalada a porcentaje
/// del valor máximo del periodo.
/// </summary>
public record EmbudoPeriodo(string Etiqueta, double Enviadas, double Respondidas, double Entrevistas, double Ofertas)
{
    public double AlturaEnviadas => this.Altura(this.Enviadas);

    public double AlturaRespondidas => this.Altura(this.Respondidas);

    public double AlturaEntrevistas => this.Altura(this.Entrevistas);

    public double AlturaOfertas => this.Altura(this.Ofertas);

    private double Maximo => Math.Max(Math.Max(this.Enviadas, this.Respondidas), Math.Max(this.Entrevistas, this.Ofertas));

    private double Altura(double valor) => this.Maximo == 0 ? 0 : valor / this.Maximo * 100.0;
}
