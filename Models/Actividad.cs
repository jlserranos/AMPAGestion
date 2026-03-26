using System.ComponentModel.DataAnnotations;

namespace AMPAGestion.Models;

public class Actividad
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio")]
    [MaxLength(150)]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Descripcion { get; set; }

    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }

    [MaxLength(200)]
    public string? Lugar { get; set; }

    [Range(0, 9999)]
    public decimal? Precio { get; set; }

    public int? PlazasMaximas { get; set; }
    public bool Activa { get; set; } = true;

    [MaxLength(500)]
    public string? Observaciones { get; set; }

    [MaxLength(500)] public string? FotoUrl { get; set; }
    [MaxLength(500)] public string? Foto2Url { get; set; }

    public List<AlertaFecha> Alertas { get; set; } = new();
}
