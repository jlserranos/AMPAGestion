namespace AMPAGestion.Models;

public static class CursoHelper
{
    // El curso va del 1 de julio al 30 de junio
    // Ej: si hoy es agosto 2024 → curso 2024-2025
    //     si hoy es marzo 2025  → curso 2024-2025

    public static string GetCursoActual()
        => GetCursoDeFecha(DateTime.Today);

    public static string GetCursoDeFecha(DateTime fecha)
    {
        var anioInicio = fecha.Month >= 7 ? fecha.Year : fecha.Year - 1;
        return $"{anioInicio}-{anioInicio + 1}";
    }

    public static int GetAnioInicioCurso(string curso)
    {
        var partes = curso.Split('-');
        return partes.Length >= 1 && int.TryParse(partes[0], out var a) ? a : DateTime.Today.Year;
    }

    public static DateTime GetInicioDelCurso(string curso)
        => new DateTime(GetAnioInicioCurso(curso), 7, 1);

    public static DateTime GetFinDelCurso(string curso)
        => new DateTime(GetAnioInicioCurso(curso) + 1, 6, 30);

    // Genera lista de cursos disponibles (actual + 4 anteriores + 1 próximo)
    public static List<string> GetCursosDisponibles()
    {
        var hoy = DateTime.Today;
        var anioInicio = hoy.Month >= 7 ? hoy.Year : hoy.Year - 1;
        var cursos = new List<string>();
        // Próximo + actual + 4 anteriores
        for (int i = 1; i >= -4; i--)
            cursos.Add($"{anioInicio - i}-{anioInicio - i + 1}");
        return cursos;
    }
}
