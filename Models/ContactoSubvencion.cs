using System.ComponentModel.DataAnnotations;

namespace AMPAGestion.Models;

public class ContactoSubvencion
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio")]
    [MaxLength(150)]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Cargo { get; set; }

    [MaxLength(100)]
    public string? Organismo { get; set; }

    [Phone]
    [MaxLength(20)]
    public string? Telefono { get; set; }

    [EmailAddress]
    [MaxLength(100)]
    public string? Email { get; set; }

    [MaxLength(300)]
    public string? Observaciones { get; set; }

    public int SubvencionId { get; set; }
    public Subvencion Subvencion { get; set; } = null!;
}
