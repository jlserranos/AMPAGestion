using Microsoft.EntityFrameworkCore;
using AMPAGestion.Data;
using AMPAGestion.Models;

namespace AMPAGestion.Services;

public class ProveedorService
{
    private readonly IDbContextFactory<ApplicationDbContext> _factory;

    public ProveedorService(IDbContextFactory<ApplicationDbContext> factory)
        => _factory = factory;

    public async Task<List<Proveedor>> GetTodosAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Proveedores
            .OrderByDescending(p => p.UltimoUso)
            .ToListAsync();
    }

    public async Task<List<string>> GetNombresAsync(string? filtro = null)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var q = db.Proveedores.AsQueryable();
        if (!string.IsNullOrWhiteSpace(filtro))
            q = q.Where(p => p.Nombre.ToLower().Contains(filtro.ToLower()));
        return await q.OrderByDescending(p => p.UltimoUso)
                      .Select(p => p.Nombre)
                      .ToListAsync();
    }

    public async Task<Proveedor?> GetByNombreAsync(string nombre)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Proveedores
            .FirstOrDefaultAsync(p => p.Nombre.ToLower() == nombre.ToLower());
    }

    // Guarda o actualiza el proveedor automáticamente al registrar una factura
    public async Task GuardarDesdeFacturaAsync(string nombre, CategoriaGasto categoria, int pctIVA)
    {
        if (string.IsNullOrWhiteSpace(nombre)) return;
        await using var db = await _factory.CreateDbContextAsync();
        var existente = await db.Proveedores
            .FirstOrDefaultAsync(p => p.Nombre.ToLower() == nombre.ToLower());
        if (existente != null)
        {
            existente.UltimoUso         = DateTime.Today;
            existente.CategoriaHabitual = categoria;
            existente.IVAHabitual       = pctIVA;
        }
        else
        {
            db.Proveedores.Add(new Proveedor
            {
                Nombre            = nombre,
                CategoriaHabitual = categoria,
                IVAHabitual       = pctIVA,
                UltimoUso         = DateTime.Today
            });
        }
        await db.SaveChangesAsync();
    }

    public async Task EliminarAsync(int id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var p = await db.Proveedores.FindAsync(id);
        if (p != null) { db.Proveedores.Remove(p); await db.SaveChangesAsync(); }
    }
}
