using System.ComponentModel.DataAnnotations;

namespace AMPAGestion.Models;

public class Proveedor
{
    public int Id { get; set; }

    [Required]
    [MaxLength(150)]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Email { get; set; }

    [MaxLength(20)]
    public string? Telefono { get; set; }

    [MaxLength(200)]
    public string? Direccion { get; set; }

    // Categoría habitual para prerellenar
    public CategoriaGasto? CategoriaHabitual { get; set; }

    // Porcentaje de IVA habitual
    public int IVAHabitual { get; set; } = 21;

    public DateTime UltimoUso { get; set; } = DateTime.Today;
}
