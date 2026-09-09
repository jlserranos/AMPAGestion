using System.ComponentModel.DataAnnotations;

namespace AMPAGestion.Models;

public class Documento
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El título es obligatorio")]
    [MaxLength(200)]
    public string Titulo { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Descripcion { get; set; }

    public TipoDocumento Tipo { get; set; } = TipoDocumento.Otro;

    public DateTime FechaSubida { get; set; } = DateTime.Today;
    public DateTime? FechaDocumento { get; set; }

    public bool Publico { get; set; } = false;

    [MaxLength(500)]
    public string? Observaciones { get; set; }

    [MaxLength(500)] public string? Foto1Url { get; set; }
    [MaxLength(500)] public string? Foto2Url { get; set; }
    [MaxLength(500)] public string? Foto3Url { get; set; }
    [MaxLength(500)] public string? Foto4Url { get; set; }
    [MaxLength(500)] public string? Foto5Url { get; set; }
    [MaxLength(500)] public string? Foto6Url { get; set; }

    [MaxLength(200)] public string? Archivo1Nombre { get; set; }
    [MaxLength(200)] public string? Archivo2Nombre { get; set; }
    [MaxLength(200)] public string? Archivo3Nombre { get; set; }
    [MaxLength(200)] public string? Archivo4Nombre { get; set; }
    [MaxLength(200)] public string? Archivo5Nombre { get; set; }
    [MaxLength(200)] public string? Archivo6Nombre { get; set; }

    public List<AlertaFecha> Alertas { get; set; } = new();
}
