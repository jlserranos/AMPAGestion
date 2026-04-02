using Microsoft.EntityFrameworkCore;
using AMPAGestion.Data;
using AMPAGestion.Models;

namespace AMPAGestion.Services;

public class ActividadService
{
    private readonly IDbContextFactory<ApplicationDbContext> _factory;

    public ActividadService(IDbContextFactory<ApplicationDbContext> factory)
        => _factory = factory;

    public async Task<List<Actividad>> GetTodasAsync(string? busqueda = null, bool? activas = null)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var q = db.Actividades.AsQueryable();
        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            busqueda = busqueda.ToLower();
            q = q.Where(a => a.Nombre.ToLower().Contains(busqueda));
        }
        if (activas.HasValue) q = q.Where(a => a.Activa == activas.Value);
        return await q.OrderByDescending(a => a.FechaInicio).ToListAsync();
    }

    public async Task<Actividad?> GetByIdAsync(int id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Actividades.Include(a => a.Alertas).FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<Actividad> CrearAsync(Actividad a)
    {
        await using var db = await _factory.CreateDbContextAsync();
        db.Actividades.Add(a); await db.SaveChangesAsync(); return a;
    }

    public async Task ActualizarAsync(Actividad actividad)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var ant = await db.AlertasFecha.Where(a => a.ActividadId == actividad.Id).ToListAsync();
        db.AlertasFecha.RemoveRange(ant);
        db.Actividades.Update(actividad);
        await db.SaveChangesAsync();
    }

    public async Task EliminarAsync(int id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var a = await db.Actividades.FindAsync(id);
        if (a != null) { db.Actividades.Remove(a); await db.SaveChangesAsync(); }
    }
}
