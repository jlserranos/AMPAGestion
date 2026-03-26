using System.ComponentModel.DataAnnotations;

namespace AMPAGestion.Models;

public class Subvencion
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El concepto es obligatorio")]
    [MaxLength(200)]
    public string Concepto { get; set; } = string.Empty;

    public OrigenSubvencion Origen { get; set; } = OrigenSubvencion.Ayuntamiento;

    [MaxLength(200)]
    public string? OrigenPersonalizado { get; set; }

    [Range(0.01, 999999.99)]
    public decimal Importe { get; set; }

    public DateTime Fecha { get; set; } = DateTime.Today;
    public DateTime? FechaConcesion { get; set; }
    public DateTime? FechaJustificacion { get; set; }

    public bool Cobrado { get; set; } = false;
    public DateTime? FechaCobro { get; set; }

    [MaxLength(1000)]
    public string? Comentarios { get; set; }

    [MaxLength(1000)]
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

    public string OrigenDescripcion => Origen switch
    {
        OrigenSubvencion.Diputacion     => "Diputación",
        OrigenSubvencion.JuntaAndalucia => "Junta de Andalucía",
        OrigenSubvencion.Ayuntamiento   => "Ayuntamiento",
        OrigenSubvencion.Ministerio     => "Ministerio",
        OrigenSubvencion.FondosEuropeos => "Fondos Europeos",
        OrigenSubvencion.SaldoAnterior  => "Saldo año anterior",
        OrigenSubvencion.Donacion       => "Donación",
        OrigenSubvencion.Otro           => OrigenPersonalizado ?? "Otro",
        _ => Origen.ToString()
    };

    public List<ContactoSubvencion> Contactos { get; set; } = new();
    public List<AlertaFecha>        Alertas   { get; set; } = new();
}
