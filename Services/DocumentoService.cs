using Microsoft.EntityFrameworkCore;
using AMPAGestion.Data;
using AMPAGestion.Models;

namespace AMPAGestion.Services;

public class DocumentoService
{
    private readonly IDbContextFactory<ApplicationDbContext> _factory;

    public DocumentoService(IDbContextFactory<ApplicationDbContext> factory)
        => _factory = factory;

    public async Task<List<Documento>> GetTodosAsync(string? busqueda = null, TipoDocumento? tipo = null)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var q = db.Documentos.AsQueryable();
        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            busqueda = busqueda.ToLower();
            q = q.Where(d => d.Titulo.ToLower().Contains(busqueda));
        }
        if (tipo.HasValue) q = q.Where(d => d.Tipo == tipo.Value);
        return await q.OrderByDescending(d => d.FechaSubida).ToListAsync();
    }

    public async Task<Documento?> GetByIdAsync(int id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        // Include Alertas para que AlertasEditor no falle
        return await db.Documentos
            .Include(d => d.Alertas)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<Documento> CrearAsync(Documento documento)
    {
        await using var db = await _factory.CreateDbContextAsync();
        db.Documentos.Add(documento);
        await db.SaveChangesAsync();
        return documento;
    }

    public async Task ActualizarAsync(Documento documento)
    {
        await using var db = await _factory.CreateDbContextAsync();
        // Eliminar alertas anteriores y reinsertar
        var ant = await db.AlertasFecha
            .Where(a => a.DocumentoId == documento.Id).ToListAsync();
        db.AlertasFecha.RemoveRange(ant);
        db.Documentos.Update(documento);
        await db.SaveChangesAsync();
    }

    public async Task EliminarAsync(int id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var d = await db.Documentos.FindAsync(id);
        if (d != null) { db.Documentos.Remove(d); await db.SaveChangesAsync(); }
    }
}
