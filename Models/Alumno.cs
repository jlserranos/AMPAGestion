using System.ComponentModel.DataAnnotations;

namespace AMPAGestion.Models;

public class Alumno
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio")]
    [MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Apellidos { get; set; }

    public CursoEscolar Curso { get; set; }

    [MaxLength(10)]
    public string? Grupo { get; set; }

    public DateTime? FechaNacimiento { get; set; }

    [MaxLength(500)]
    public string? Alergias { get; set; }

    [MaxLength(500)]
    public string? Observaciones { get; set; }

    public bool Activo { get; set; } = true;

    // Clave foránea
    public int SocioId { get; set; }
    public Socio Socio { get; set; } = null!;

    public string NombreCompleto => string.IsNullOrEmpty(Apellidos)
        ? Nombre
        : $"{Nombre} {Apellidos}";

    // Orden de presentación (el orden del enum se mantiene por compatibilidad con la BD)
    public static readonly CursoEscolar[] CursosOrdenados =
    {
        CursoEscolar.PrimeroESO, CursoEscolar.SegundoESO, CursoEscolar.TerceroESO, CursoEscolar.CuartoESO,
        CursoEscolar.PrimeroBachiller, CursoEscolar.SegundoBachiller,
        CursoEscolar.PrimeroGradoMedio, CursoEscolar.SegundoGradoMedio,
        CursoEscolar.PrimeroGradoSuperior, CursoEscolar.SegundoGradoSuperior
    };

    public string CursoDescripcion => Curso switch
    {
        CursoEscolar.PrimeroESO           => "1º ESO",
        CursoEscolar.SegundoESO           => "2º ESO",
        CursoEscolar.TerceroESO           => "3º ESO",
        CursoEscolar.CuartoESO            => "4º ESO",
        CursoEscolar.PrimeroBachiller     => "1º Bachillerato",
        CursoEscolar.SegundoBachiller     => "2º Bachillerato",
        CursoEscolar.PrimeroGradoMedio    => "1º Grado Medio",
        CursoEscolar.SegundoGradoMedio    => "2º Grado Medio",
        CursoEscolar.PrimeroGradoSuperior => "1º Grado Superior",
        CursoEscolar.SegundoGradoSuperior => "2º Grado Superior",
        _ => Curso.ToString()
    };
}
