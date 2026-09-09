using System.ComponentModel.DataAnnotations;

namespace AMPAGestion.Models;

public class Cuota
{
    public int Id { get; set; }

    public DateTime Fecha { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "El concepto es obligatorio")]
    [MaxLength(200)]
    public string Concepto { get; set; } = "Cuota anual";

    [Range(0.01, 9999.99)]
    public decimal Importe { get; set; } = 25m;

    public MetodoPago Metodo { get; set; } = MetodoPago.Bizum;

    [MaxLength(100)]
    public string? Referencia { get; set; }

    // Curso académico al que pertenece esta cuota (ej: "2024-2025")
    [MaxLength(9)]
    public string CursoAcademico { get; set; } = CursoHelper.GetCursoActual();

    [MaxLength(300)]
    public string? Observaciones { get; set; }

    public int SocioId { get; set; }
    public Socio Socio { get; set; } = null!;
}
