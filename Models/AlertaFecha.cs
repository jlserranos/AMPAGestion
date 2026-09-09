using System.ComponentModel.DataAnnotations;

namespace AMPAGestion.Models;

public enum EstadoAlerta { Pendiente, Gestionada, Vencida }
public enum TipoEntidadAlerta { Subvencion, Factura, Documento, Actividad }

public class AlertaFecha
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El concepto de la alerta es obligatorio")]
    [MaxLength(200)]
    public string Concepto { get; set; } = string.Empty;

    public DateTime FechaAlerta { get; set; } = DateTime.Today.AddDays(30);

    [MaxLength(500)]
    public string? Descripcion { get; set; }

    public EstadoAlerta Estado { get; set; } = EstadoAlerta.Pendiente;

    // Tipo de entidad origen
    public TipoEntidadAlerta TipoEntidad { get; set; } = TipoEntidadAlerta.Subvencion;

    // FKs nullables — solo una estará rellena
    public int? SubvencionId { get; set; }
    public Subvencion? Subvencion { get; set; }

    public int? FacturaId { get; set; }
    public Factura? Factura { get; set; }

    public int? DocumentoId { get; set; }
    public Documento? Documento { get; set; }

    public int? ActividadId { get; set; }
    public Actividad? Actividad { get; set; }

    public int DiasRestantes => (FechaAlerta - DateTime.Today).Days;
    public bool EsUrgente   => DiasRestantes is >= 0 and <= 15;
    public bool EstaVencida => DiasRestantes < 0 && Estado == EstadoAlerta.Pendiente;

    // Título de la entidad origen para mostrar en dashboard
    public string EntidadTitulo => TipoEntidad switch
    {
        TipoEntidadAlerta.Subvencion => Subvencion?.Concepto ?? "Subvención",
        TipoEntidadAlerta.Factura    => Factura != null ? $"{Factura.Proveedor} — {Factura.Concepto}" : "Factura",
        TipoEntidadAlerta.Documento  => Documento?.Titulo ?? "Documento",
        TipoEntidadAlerta.Actividad  => Actividad?.Nombre ?? "Actividad",
        _ => ""
    };

    public string EntidadUrl => TipoEntidad switch
    {
        TipoEntidadAlerta.Subvencion => $"/subvenciones/{SubvencionId}/editar",
        TipoEntidadAlerta.Factura    => $"/facturas/{FacturaId}/editar",
        TipoEntidadAlerta.Documento  => $"/documentos/{DocumentoId}/editar",
        TipoEntidadAlerta.Actividad  => $"/actividades/{ActividadId}/editar",
        _ => "/"
    };
}
