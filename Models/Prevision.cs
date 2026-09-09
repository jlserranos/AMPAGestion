using System.ComponentModel.DataAnnotations;

namespace AMPAGestion.Models;

public enum TipoPrevision { Ingreso, Gasto }

public class Prevision
{
    public int Id { get; set; }

    public int Anio { get; set; } = DateTime.Today.Year;

    public TipoPrevision Tipo { get; set; } = TipoPrevision.Ingreso;

    [Required(ErrorMessage = "El concepto es obligatorio")]
    [MaxLength(200)]
    public string Concepto { get; set; } = string.Empty;

    // Categoría de gasto (si Tipo == Gasto)
    public CategoriaGasto? Categoria { get; set; }

    // Importe previsto total anual
    [Range(0, 999999)]
    public decimal ImportePrevisto { get; set; }

    // Distribución mensual prevista (null = distribuir uniformemente)
    public decimal? Enero    { get; set; }
    public decimal? Febrero  { get; set; }
    public decimal? Marzo    { get; set; }
    public decimal? Abril    { get; set; }
    public decimal? Mayo     { get; set; }
    public decimal? Junio    { get; set; }
    public decimal? Julio    { get; set; }
    public decimal? Agosto   { get; set; }
    public decimal? Septiembre { get; set; }
    public decimal? Octubre  { get; set; }
    public decimal? Noviembre { get; set; }
    public decimal? Diciembre { get; set; }

    [MaxLength(500)]
    public string? Observaciones { get; set; }

    public decimal GetMes(int mes) => mes switch
    {
        1  => Enero     ?? 0,
        2  => Febrero   ?? 0,
        3  => Marzo     ?? 0,
        4  => Abril     ?? 0,
        5  => Mayo      ?? 0,
        6  => Junio     ?? 0,
        7  => Julio     ?? 0,
        8  => Agosto    ?? 0,
        9  => Septiembre ?? 0,
        10 => Octubre   ?? 0,
        11 => Noviembre ?? 0,
        12 => Diciembre ?? 0,
        _  => 0
    };

    public void SetMes(int mes, decimal valor)
    {
        switch (mes)
        {
            case 1:  Enero      = valor; break;
            case 2:  Febrero    = valor; break;
            case 3:  Marzo      = valor; break;
            case 4:  Abril      = valor; break;
            case 5:  Mayo       = valor; break;
            case 6:  Junio      = valor; break;
            case 7:  Julio      = valor; break;
            case 8:  Agosto     = valor; break;
            case 9:  Septiembre = valor; break;
            case 10: Octubre    = valor; break;
            case 11: Noviembre  = valor; break;
            case 12: Diciembre  = valor; break;
        }
    }

    // Distribuye el importe anual uniformemente entre los 12 meses
    public void DistribuirUniforme()
    {
        var mensual = Math.Round(ImportePrevisto / 12, 2);
        for (int m = 1; m <= 12; m++) SetMes(m, mensual);
    }
}
