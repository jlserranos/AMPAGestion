using System.ComponentModel.DataAnnotations;

namespace AMPAGestion.Models;

public class Socio
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Los apellidos son obligatorios")]
    [MaxLength(100)]
    public string Apellidos { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre es obligatorio")]
    [MaxLength(60)]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(9)]
    public string? DNI { get; set; }

    [Phone(ErrorMessage = "Teléfono no válido")]
    [MaxLength(15)]
    public string? Telefono { get; set; }

    [EmailAddress(ErrorMessage = "Email no válido")]
    [MaxLength(100)]
    public string? Email { get; set; }

    [MaxLength(200)]
    public string? Direccion { get; set; }

    public DateTime FechaAlta { get; set; } = DateTime.Today;

    public EstadoPago Estado { get; set; } = EstadoPago.Pendiente;

    [MaxLength(500)]
    public string? Observaciones { get; set; }

    public string NombreCompleto => $"{Apellidos}, {Nombre}";

    // Navegación
    public List<Alumno> Alumnos { get; set; } = new();
    public List<Cuota> Cuotas { get; set; } = new();
}
