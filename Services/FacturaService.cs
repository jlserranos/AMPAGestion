using Microsoft.EntityFrameworkCore;
using AMPAGestion.Data;
using AMPAGestion.Models;

namespace AMPAGestion.Services;

public class FacturaService
{
    private readonly IDbContextFactory<ApplicationDbContext> _factory;
    private readonly ProveedorService _proveedorSvc;

    public FacturaService(IDbContextFactory<ApplicationDbContext> factory, ProveedorService proveedorSvc)
    {
        _factory      = factory;
        _proveedorSvc = proveedorSvc;
    }

    public async Task<List<Factura>> GetTodasAsync(string? busqueda = null, CategoriaGasto? categoria = null, bool? pagadas = null)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var q = db.Facturas.AsQueryable();
        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            busqueda = busqueda.ToLower();
            q = q.Where(f => f.Proveedor.ToLower().Contains(busqueda) || f.Concepto.ToLower().Contains(busqueda));
        }
        if (categoria.HasValue) q = q.Where(f => f.Categoria == categoria.Value);
        if (pagadas.HasValue)   q = q.Where(f => f.Pagado == pagadas.Value);
        return await q.OrderByDescending(f => f.Fecha).ToListAsync();
    }

    public async Task<Factura?> GetByIdAsync(int id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Facturas
            .Include(f => f.Alertas)
            .FirstOrDefaultAsync(f => f.Id == id);
    }

    public async Task<Factura> CrearAsync(Factura factura)
    {
        await using var db = await _factory.CreateDbContextAsync();
        db.Facturas.Add(factura);
        await db.SaveChangesAsync();
        await _proveedorSvc.GuardarDesdeFacturaAsync(factura.Proveedor, factura.Categoria, factura.PorcentajeIVA);
        return factura;
    }

    public async Task ActualizarAsync(Factura factura)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var alertasAnt = await db.AlertasFecha.Where(a => a.FacturaId == factura.Id).ToListAsync();
        db.AlertasFecha.RemoveRange(alertasAnt);
        db.Facturas.Update(factura);
        await db.SaveChangesAsync();
        await _proveedorSvc.GuardarDesdeFacturaAsync(factura.Proveedor, factura.Categoria, factura.PorcentajeIVA);
    }

    public async Task EliminarAsync(int id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var f = await db.Facturas.FindAsync(id);
        if (f != null) { db.Facturas.Remove(f); await db.SaveChangesAsync(); }
    }

    public async Task MarcarPagadaAsync(int id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var f = await db.Facturas.FindAsync(id);
        if (f != null) { f.Pagado = true; f.FechaPago = DateTime.Today; await db.SaveChangesAsync(); }
    }
}
