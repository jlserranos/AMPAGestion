using System.ComponentModel.DataAnnotations;

namespace AMPAGestion.Models;

public class Factura
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "El proveedor es obligatorio")]
    [MaxLength(150)]
    public string Proveedor { get; set; } = string.Empty;

    [Required(ErrorMessage = "El concepto es obligatorio")]
    [MaxLength(300)]
    public string Concepto { get; set; } = string.Empty;

    [Range(0.01, 99999.99)]
    public decimal BaseImponible { get; set; }

    public int PorcentajeIVA { get; set; } = 21;

    [Range(0, 99999.99)]
    public decimal IVA { get; set; }

    public decimal Total => BaseImponible + IVA;

    public CategoriaGasto Categoria { get; set; }
    public bool Pagado { get; set; }
    public DateTime? FechaPago { get; set; }

    [MaxLength(100)]
    public string? NumeroFactura { get; set; }

    [MaxLength(300)]
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
