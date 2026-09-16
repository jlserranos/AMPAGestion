using Microsoft.EntityFrameworkCore;
using AMPAGestion.Data;
using AMPAGestion.Models;

namespace AMPAGestion.Services;

public class AlumnoService
{
    private readonly IDbContextFactory<ApplicationDbContext> _factory;

    public AlumnoService(IDbContextFactory<ApplicationDbContext> factory)
        => _factory = factory;

    // cursoEscolar = nivel (PrimeroESO, SegundoESO...) NO el año académico
    public async Task<List<Alumno>> GetTodosAsync(
        string? busqueda = null,
        CursoEscolar? cursoEscolar = null)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var q = db.Alumnos.Include(a => a.Socio).Where(a => a.Activo);

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            busqueda = busqueda.ToLower();
            q = q.Where(a =>
                a.Nombre.ToLower().Contains(busqueda) ||
                (a.Apellidos != null && a.Apellidos.ToLower().Contains(busqueda)));
        }

        if (cursoEscolar.HasValue)
            q = q.Where(a => a.Curso == cursoEscolar.Value);

        return await q.OrderBy(a => a.Apellidos).ThenBy(a => a.Nombre).ToListAsync();
    }

    public async Task<Alumno?> GetByIdAsync(int id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Alumnos.Include(a => a.Socio).FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<Alumno> CrearAsync(Alumno alumno)
    {
        await using var db = await _factory.CreateDbContextAsync();
        db.Alumnos.Add(alumno);
        await db.SaveChangesAsync();
        return alumno;
    }

    public async Task ActualizarAsync(Alumno alumno)
    {
        await using var db = await _factory.CreateDbContextAsync();
        db.Alumnos.Update(alumno);
        await db.SaveChangesAsync();
    }

    public async Task EliminarAsync(int id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var a = await db.Alumnos.FindAsync(id);
        if (a != null) { db.Alumnos.Remove(a); await db.SaveChangesAsync(); }
    }
}
