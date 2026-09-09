using System.ComponentModel.DataAnnotations;

namespace AMPAGestion.Models;

public enum TipoApunteManual
{
    AperturaEjercicio,    // Saldo inicial del curso
    CierreEjercicio,      // Cierre del curso
    ComisionBancaria,     // Gastos bancarios
    InteresesBancarios,   // Ingresos por intereses
    Devolucion,           // Devolución de pago
    Ajuste,               // Corrección/ajuste contable
    Transferencia,        // Transferencia entre cuentas
    Otro                  // Cualquier otro apunte
}

public class ApunteManual
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El concepto es obligatorio")]
    [MaxLength(200)]
    public string Concepto { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Descripcion { get; set; }

    public DateTime Fecha { get; set; } = DateTime.Today;

    [MaxLength(9)]
    public string CursoAcademico { get; set; } = CursoHelper.GetCursoActual();

    public TipoApunte Tipo { get; set; } = TipoApunte.Ingreso;

    public TipoApunteManual TipoOperacion { get; set; } = TipoApunteManual.Otro;

    [Range(0.01, 999999.99)]
    public decimal Importe { get; set; }

    [MaxLength(100)]
    public string? Referencia { get; set; }

    // Generado automáticamente en cierres y aperturas
    public bool EsAutomatico { get; set; } = false;

    public string TipoOperacionDescripcion => TipoOperacion switch
    {
        TipoApunteManual.AperturaEjercicio  => "Apertura de ejercicio",
        TipoApunteManual.CierreEjercicio    => "Cierre de ejercicio",
        TipoApunteManual.ComisionBancaria   => "Comisión bancaria",
        TipoApunteManual.InteresesBancarios => "Intereses bancarios",
        TipoApunteManual.Devolucion         => "Devolución",
        TipoApunteManual.Ajuste             => "Ajuste contable",
        TipoApunteManual.Transferencia      => "Transferencia",
        TipoApunteManual.Otro               => "Apunte manual",
        _ => "Apunte manual"
    };
}
