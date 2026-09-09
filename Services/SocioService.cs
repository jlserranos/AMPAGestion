using Microsoft.EntityFrameworkCore;
using AMPAGestion.Data;
using AMPAGestion.Models;

namespace AMPAGestion.Services;

public class SocioService
{
    private readonly IDbContextFactory<ApplicationDbContext> _factory;

    public SocioService(IDbContextFactory<ApplicationDbContext> factory)
        => _factory = factory;

    // Devuelve todos los socios con su estado calculado para el curso indicado
    public async Task<List<SocioConCurso>> GetTodosPorCursoAsync(
        string curso, string? busqueda = null, EstadoPago? estadoFiltro = null)
    {
        await using var db = await _factory.CreateDbContextAsync();

        var socios = await db.Socios
            .Include(s => s.Alumnos)
            .Include(s => s.Cuotas)
            .OrderBy(s => s.Apellidos).ThenBy(s => s.Nombre)
            .ToListAsync();

        // Filtrar por búsqueda
        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            busqueda = busqueda.ToLower();
            socios = socios.Where(s =>
                s.Apellidos.ToLower().Contains(busqueda) ||
                s.Nombre.ToLower().Contains(busqueda) ||
                (s.Email != null && s.Email.ToLower().Contains(busqueda)) ||
                (s.DNI != null && s.DNI.Contains(busqueda))
            ).ToList();
        }

        // Calcular estado por curso a partir de cuotas
        var resultado = socios.Select(s =>
        {
            var cuotaCurso = s.Cuotas.FirstOrDefault(c => c.CursoAcademico == curso);
            var estado = cuotaCurso != null ? EstadoPago.Pagado : EstadoPago.Pendiente;
            // Respetar exento y baja del estado global del socio
            if (s.Estado == EstadoPago.Exento) estado = EstadoPago.Exento;
            if (s.Estado == EstadoPago.Baja)   estado = EstadoPago.Baja;
            return new SocioConCurso
            {
                Socio       = s,
                Curso       = curso,
                EstadoCurso = estado,
                Cuota       = cuotaCurso
            };
        }).ToList();

        // Filtrar por estado
        if (estadoFiltro.HasValue)
            resultado = resultado.Where(r => r.EstadoCurso == estadoFiltro.Value).ToList();

        return resultado;
    }

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
        if (estado.HasValue) q = q.Where(s => s.Estado == estado.Value);
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
        if (socio != null) { db.Socios.Remove(socio); await db.SaveChangesAsync(); }
    }

    public async Task<ResumenSocios> GetResumenAsync(string? curso = null)
    {
        curso ??= CursoHelper.GetCursoActual();
        await using var db = await _factory.CreateDbContextAsync();

        var socios = await db.Socios.Include(s => s.Cuotas).ToListAsync();
        int pagados = 0, pendientes = 0, exentos = 0, bajas = 0;

        foreach (var s in socios)
        {
            if (s.Estado == EstadoPago.Baja)   { bajas++;    continue; }
            if (s.Estado == EstadoPago.Exento) { exentos++;  continue; }
            var tieneCuota = s.Cuotas.Any(c => c.CursoAcademico == curso);
            if (tieneCuota) pagados++; else pendientes++;
        }

        return new ResumenSocios
        {
            Total      = socios.Count,
            Pagados    = pagados,
            Pendientes = pendientes,
            Exentos    = exentos,
            Bajas      = bajas,
            Curso      = curso
        };
    }
}

// DTO para socio con estado calculado por curso
public class SocioConCurso
{
    public Socio Socio       { get; set; } = null!;
    public string Curso      { get; set; } = string.Empty;
    public EstadoPago EstadoCurso { get; set; }
    public Cuota? Cuota      { get; set; }

    // Delegaciones de conveniencia
    public int Id                => Socio.Id;
    public string NombreCompleto => Socio.NombreCompleto;
    public string Apellidos      => Socio.Apellidos;
    public string Nombre         => Socio.Nombre;
    public string? Email         => Socio.Email;
    public string? Telefono      => Socio.Telefono;
    public string? DNI           => Socio.DNI;
    public DateTime FechaAlta    => Socio.FechaAlta;
    public List<Alumno> Alumnos  => Socio.Alumnos;
    public List<Cuota> Cuotas    => Socio.Cuotas;
    public EstadoPago EstadoGlobal => Socio.Estado;
}

public class ResumenSocios
{
    public int Total      { get; set; }
    public int Pagados    { get; set; }
    public int Pendientes { get; set; }
    public int Exentos    { get; set; }
    public int Bajas      { get; set; }
    public string Curso   { get; set; } = string.Empty;
}
