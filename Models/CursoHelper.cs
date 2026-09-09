namespace AMPAGestion.Models;

public static class CursoHelper
{
    // Curso actual según la fecha de hoy
    public static string GetCursoActual()
        => GetCursoDeFecha(DateTime.Today);

    // Curso al que pertenece una fecha dada
    public static string GetCursoDeFecha(DateTime fecha)
    {
        int anioInicio = fecha.Month >= 7 ? fecha.Year : fecha.Year - 1;
        return $"{anioInicio}-{anioInicio + 1}";
    }

    // Año de inicio de un curso (ej: "2024-2025" → 2024)
    public static int GetAnioInicioCurso(string curso)
    {
        var partes = curso.Split('-');
        return int.TryParse(partes[0], out var anio) ? anio : DateTime.Today.Year;
    }

    // Fecha inicio del curso (1 de julio del año de inicio)
    public static DateTime GetFechaInicio(string curso)
        => new DateTime(GetAnioInicioCurso(curso), 7, 1);

    // Fecha fin del curso (30 de junio del año siguiente)
    public static DateTime GetFechaFin(string curso)
        => new DateTime(GetAnioInicioCurso(curso) + 1, 6, 30, 23, 59, 59);

    // Lista de cursos disponibles (desde 2020-2021 hasta curso siguiente)
    public static List<string> GetCursosDisponibles()
    {
        var cursos = new List<string>();
        int anioActual = DateTime.Today.Month >= 7 ? DateTime.Today.Year : DateTime.Today.Year - 1;
        // Incluir desde 4 años atrás hasta 1 año adelante
        for (int a = anioActual + 1; a >= anioActual - 4; a--)
            cursos.Add($"{a}-{a + 1}");
        return cursos;
    }

    // Comprueba si una fecha pertenece a un curso
    public static bool FechaEnCurso(DateTime fecha, string curso)
        => fecha >= GetFechaInicio(curso) && fecha <= GetFechaFin(curso);
}
