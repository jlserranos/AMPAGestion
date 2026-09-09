using Microsoft.EntityFrameworkCore;
using AMPAGestion.Data;
using AMPAGestion.Models;

namespace AMPAGestion.Services;

public class SubvencionService
{
    private readonly IDbContextFactory<ApplicationDbContext> _factory;

    public SubvencionService(IDbContextFactory<ApplicationDbContext> factory)
        => _factory = factory;

    public async Task<List<Subvencion>> GetTodasAsync(string? busqueda = null,
        OrigenSubvencion? origen = null, string? curso = null)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var q = db.Subvenciones.Include(s => s.Alertas).Include(s => s.Contactos).AsQueryable();

        if (!string.IsNullOrEmpty(curso))
        {
            var desde = CursoHelper.GetFechaInicio(curso);
            var hasta = CursoHelper.GetFechaFin(curso);
            q = q.Where(s => s.Fecha >= desde && s.Fecha <= hasta);
        }
        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            busqueda = busqueda.ToLower();
            q = q.Where(s => s.Concepto.ToLower().Contains(busqueda));
        }
        if (origen.HasValue) q = q.Where(s => s.Origen == origen.Value);

        return await q.OrderByDescending(s => s.Fecha).ToListAsync();
    }

    public async Task<Subvencion?> GetByIdAsync(int id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Subvenciones
            .Include(s => s.Alertas)
            .Include(s => s.Contactos)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<Subvencion> CrearAsync(Subvencion s)
    {
        await using var db = await _factory.CreateDbContextAsync();
        db.Subvenciones.Add(s);
        await db.SaveChangesAsync();
        return s;
    }

    public async Task ActualizarAsync(Subvencion subvencion)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var ant = await db.ContactosSubvencion.Where(c => c.SubvencionId == subvencion.Id).ToListAsync();
        db.ContactosSubvencion.RemoveRange(ant);
        var alertasAnt = await db.AlertasFecha.Where(a => a.SubvencionId == subvencion.Id).ToListAsync();
        db.AlertasFecha.RemoveRange(alertasAnt);
        db.Subvenciones.Update(subvencion);
        await db.SaveChangesAsync();
    }

    public async Task EliminarAsync(int id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var s = await db.Subvenciones.FindAsync(id);
        if (s != null) { db.Subvenciones.Remove(s); await db.SaveChangesAsync(); }
    }

    public async Task<List<AlertaFecha>> GetAlertasPendientesAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.AlertasFecha
            .Include(a => a.Subvencion)
            .Include(a => a.Factura)
            .Include(a => a.Documento)
            .Include(a => a.Actividad)
            .Where(a => a.Estado == EstadoAlerta.Pendiente)
            .OrderBy(a => a.FechaAlerta)
            .ToListAsync();
    }

    public async Task MarcarAlertaGestionadaAsync(int id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var a = await db.AlertasFecha.FindAsync(id);
        if (a != null) { a.Estado = EstadoAlerta.Gestionada; await db.SaveChangesAsync(); }
    }

    public async Task<List<string>> GetCursosDisponiblesAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        var fechas = await db.Subvenciones.Select(s => s.Fecha).ToListAsync();
        var cursos = fechas.Select(f => CursoHelper.GetCursoDeFecha(f)).Distinct().OrderByDescending(c => c).ToList();
        var actual = CursoHelper.GetCursoActual();
        if (!cursos.Contains(actual)) cursos.Insert(0, actual);
        return cursos;
    }
}
