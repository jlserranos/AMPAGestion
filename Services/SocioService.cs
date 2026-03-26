using Microsoft.EntityFrameworkCore;
using AMPAGestion.Data;
using AMPAGestion.Models;

namespace AMPAGestion.Services;

public class SocioService
{
    private readonly IDbContextFactory<ApplicationDbContext> _factory;

    public SocioService(IDbContextFactory<ApplicationDbContext> factory)
        => _factory = factory;

    public async Task<List<Socio>> GetTodosAsync(string? busqueda = null, EstadoPago? estado = null)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var q = db.Socios.Include(s => s.Alumnos).Include(s => s.Cuotas).AsQueryable();

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            busqueda = busqueda.ToLower();
            q = q.Where(s =>
                s.Apellidos.ToLower().Contains(busqueda) ||
                s.Nombre.ToLower().Contains(busqueda) ||
                (s.Email != null && s.Email.ToLower().Contains(busqueda)) ||
                (s.DNI != null && s.DNI.Contains(busqueda)));
        }

        if (estado.HasValue)
            q = q.Where(s => s.Estado == estado.Value);

        return await q.OrderBy(s => s.Apellidos).ThenBy(s => s.Nombre).ToListAsync();
    }

    public async Task<Socio?> GetByIdAsync(int id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Socios
            .Include(s => s.Alumnos)
            .Include(s => s.Cuotas)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<Socio> CrearAsync(Socio socio)
    {
        await using var db = await _factory.CreateDbContextAsync();
        db.Socios.Add(socio);
        await db.SaveChangesAsync();
        return socio;
    }

    public async Task ActualizarAsync(Socio socio)
    {
        await using var db = await _factory.CreateDbContextAsync();
        db.Socios.Update(socio);
        await db.SaveChangesAsync();
    }

    public async Task EliminarAsync(int id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var socio = await db.Socios.FindAsync(id);
        if (socio != null)
        {
            db.Socios.Remove(socio);
            await db.SaveChangesAsync();
        }
    }

    public async Task<ResumenSocios> GetResumenAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        var socios = await db.Socios.ToListAsync();
        return new ResumenSocios
        {
            Total      = socios.Count,
            Pagados    = socios.Count(s => s.Estado == EstadoPago.Pagado),
            Pendientes = socios.Count(s => s.Estado == EstadoPago.Pendiente),
            Exentos    = socios.Count(s => s.Estado == EstadoPago.Exento),
            Bajas      = socios.Count(s => s.Estado == EstadoPago.Baja)
        };
    }
}

public class ResumenSocios
{
    public int Total { get; set; }
    public int Pagados { get; set; }
    public int Pendientes { get; set; }
    public int Exentos { get; set; }
    public int Bajas { get; set; }
}
