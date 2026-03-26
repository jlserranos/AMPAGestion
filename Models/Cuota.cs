using System.ComponentModel.DataAnnotations;

namespace AMPAGestion.Models;

public class Cuota
{
    public int Id { get; set; }

    public DateTime Fecha { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "El concepto es obligatorio")]
    [MaxLength(200)]
    public string Concepto { get; set; } = "Cuota anual";

    [Range(0.01, 9999.99, ErrorMessage = "El importe debe ser mayor que 0")]
    public decimal Importe { get; set; }

    public MetodoPago Metodo { get; set; } = MetodoPago.Bizum;

    [MaxLength(100)]
    public string? Referencia { get; set; }

    [MaxLength(200)]
    public string? Observaciones { get; set; }

    public string? RegistradoPor { get; set; }

    // Clave foránea
    public int SocioId { get; set; }
    public Socio Socio { get; set; } = null!;
}
