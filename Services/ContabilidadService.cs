using Microsoft.EntityFrameworkCore;
using AMPAGestion.Data;
using AMPAGestion.Models;

namespace AMPAGestion.Services;

public class ContabilidadService
{
    private readonly IDbContextFactory<ApplicationDbContext> _factory;

    public ContabilidadService(IDbContextFactory<ApplicationDbContext> factory)
        => _factory = factory;

    // ── Resumen anual ─────────────────────────────────────────────────────
    public async Task<ResumenContable> GetResumenAsync(int? anio = null)
    {
        anio ??= DateTime.Today.Year;
        await using var db = await _factory.CreateDbContextAsync();

        var cuotas   = await db.Cuotas.Where(c => c.Fecha.Year == anio).ToListAsync();
        var facturas = await db.Facturas.Where(f => f.Fecha.Year == anio).ToListAsync();

        var totalIngresos    = cuotas.Sum(c => c.Importe);
        var totalGastos      = facturas.Where(f => f.Pagado).Sum(f => f.Total);
        var gastosPendientes = facturas.Where(f => !f.Pagado).Sum(f => f.Total);

        var gastosPorCategoria = facturas
            .GroupBy(f => f.Categoria)
            .Select(g => new GastoCategoria
            {
                Categoria = g.Key,
                Total     = g.Sum(f => f.Total),
                Pagado    = g.Where(f => f.Pagado).Sum(f => f.Total),
                Cantidad  = g.Count()
            })
            .OrderByDescending(g => g.Total)
            .ToList();

        var movimientosMensuales = Enumerable.Range(1, 12).Select(mes =>
        {
            var ingresos = cuotas.Where(c => c.Fecha.Month == mes).Sum(c => c.Importe);
            var gastos   = facturas.Where(f => f.Fecha.Month == mes && f.Pagado).Sum(f => f.Total);
            return new MovimientoMensual { Mes = mes, Ingresos = ingresos, Gastos = gastos };
        }).ToList();

        // Saldo acumulado año a año
        var todosAnios = await db.Cuotas.Select(c => c.Fecha.Year)
            .Union(db.Facturas.Select(f => f.Fecha.Year))
            .Distinct().OrderBy(a => a).ToListAsync();

        decimal saldoAcumulado = 0;
        foreach (var a in todosAnios.Where(a => a < anio))
        {
            var ing = await db.Cuotas.Where(c => c.Fecha.Year == a).SumAsync(c => c.Importe);
            var gas = await db.Facturas.Where(f => f.Fecha.Year == a && f.Pagado).SumAsync(f => f.BaseImponible + f.IVA);
            saldoAcumulado += ing - gas;
        }

        return new ResumenContable
        {
            Anio                 = anio.Value,
            TotalIngresos        = totalIngresos,
            TotalGastos          = totalGastos,
            GastosPendientes     = gastosPendientes,
            Saldo                = totalIngresos - totalGastos,
            SaldoAcumulado       = saldoAcumulado + (totalIngresos - totalGastos),
            NumCuotas            = cuotas.Count,
            NumFacturas          = facturas.Count,
            FacturasPendientes   = facturas.Count(f => !f.Pagado),
            GastosPorCategoria   = gastosPorCategoria,
            MovimientosMensuales = movimientosMensuales
        };
    }

    // ── Libro de cuentas (movimientos unificados) ─────────────────────────
    public async Task<List<ApunteContable>> GetLibroAsync(int? anio = null, TipoApunte? tipo = null)
    {
        await using var db = await _factory.CreateDbContextAsync();

        var cuotas       = db.Cuotas.Include(c => c.Socio).AsQueryable();
        var facturas     = db.Facturas.AsQueryable();
        var subvenciones = db.Subvenciones.AsQueryable();

        if (anio.HasValue)
        {
            cuotas       = cuotas.Where(c => c.Fecha.Year == anio.Value);
            facturas     = facturas.Where(f => f.Fecha.Year == anio.Value);
            subvenciones = subvenciones.Where(s => s.Fecha.Year == anio.Value);
        }

        var apuntesCuotas = (await cuotas.ToListAsync()).Select(c => new ApunteContable
        {
            Fecha      = c.Fecha,
            Tipo       = TipoApunte.Ingreso,
            Concepto   = c.Concepto,
            Detalle    = c.Socio?.NombreCompleto ?? "",
            Referencia = c.Referencia ?? "",
            Debe       = 0,
            Haber      = c.Importe,
            Origen     = "Cuota",
            OrigenId   = c.Id
        });

        var apuntesSubvenciones = (await subvenciones.ToListAsync()).Select(s => new ApunteContable
        {
            Fecha      = s.FechaCobro ?? s.Fecha,
            Tipo       = TipoApunte.Ingreso,
            Concepto   = s.Concepto,
            Detalle    = s.OrigenDescripcion,
            Referencia = "",
            Debe       = 0,
            Haber      = s.Cobrado ? s.Importe : 0,
            Pendiente  = !s.Cobrado,
            Categoria  = "Subvención",
            Origen     = "Subvencion",
            OrigenId   = s.Id
        });

        var apuntesFacturas = (await facturas.ToListAsync()).Select(f => new ApunteContable
        {
            Fecha      = f.Pagado && f.FechaPago.HasValue ? f.FechaPago.Value : f.Fecha,
            Tipo       = TipoApunte.Gasto,
            Concepto   = f.Concepto,
            Detalle    = f.Proveedor,
            Referencia = f.NumeroFactura ?? "",
            Debe       = f.Total,
            Haber      = 0,
            Pendiente  = !f.Pagado,
            Categoria  = f.Categoria.ToString(),
            Origen     = "Factura",
            OrigenId   = f.Id
        });

        var todos = apuntesCuotas.Concat(apuntesSubvenciones).Concat(apuntesFacturas);

        if (tipo.HasValue)
            todos = todos.Where(a => a.Tipo == tipo.Value);

        // Calcular saldo acumulado
        var lista = todos.OrderBy(a => a.Fecha).ThenBy(a => a.Tipo).ToList();
        decimal saldo = 0;
        foreach (var a in lista)
        {
            saldo += a.Haber - a.Debe;
            a.SaldoAcumulado = saldo;
        }

        return lista.OrderByDescending(a => a.Fecha).ToList();
    }

    // ── Cuotas ────────────────────────────────────────────────────────────
    public async Task<List<Cuota>> GetCuotasAsync(int? anio = null)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var q = db.Cuotas.Include(c => c.Socio).AsQueryable();
        if (anio.HasValue)
            q = q.Where(c => c.Fecha.Year == anio.Value);
        return await q.OrderByDescending(c => c.Fecha).ToListAsync();
    }

    public async Task<Cuota> RegistrarCuotaAsync(Cuota cuota)
    {
        await using var db = await _factory.CreateDbContextAsync();
        db.Cuotas.Add(cuota);
        var socio = await db.Socios.FindAsync(cuota.SocioId);
        if (socio != null)
            socio.Estado = EstadoPago.Pagado;
        await db.SaveChangesAsync();
        return cuota;
    }

    public async Task EliminarCuotaAsync(int id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var cuota = await db.Cuotas.FindAsync(id);
        if (cuota != null)
        {
            db.Cuotas.Remove(cuota);
            await db.SaveChangesAsync();
        }
    }

    // ── Cursos académicos disponibles ──────────────────────────────────────
    public async Task<List<string>> GetCursosDisponiblesAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        var aniosCuotas   = await db.Cuotas.Select(c => c.Fecha.Year).Distinct().ToListAsync();
        var aniosFacturas = await db.Facturas.Select(f => f.Fecha.Year).Distinct().ToListAsync();
        var anios = aniosCuotas.Union(aniosFacturas).OrderByDescending(a => a).ToList();

        // Convertir años a cursos académicos (julio-junio)
        var cursos = anios
            .SelectMany(a => new[] { $"{a-1}-{a}", $"{a}-{a+1}" })
            .Distinct()
            .OrderByDescending(c => c)
            .ToList();

        return cursos.Any() ? cursos : AMPAGestion.Models.CursoHelper.GetCursosDisponibles();
    }

    // ── Años disponibles ──────────────────────────────────────────────────
    public async Task<List<int>> GetAniosDisponiblesAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        var aniosCuotas   = await db.Cuotas.Select(c => c.Fecha.Year).Distinct().ToListAsync();
        var aniosFacturas = await db.Facturas.Select(f => f.Fecha.Year).Distinct().ToListAsync();
        return aniosCuotas.Union(aniosFacturas)
            .OrderByDescending(a => a)
            .ToList();
    }
}

// ── DTOs ──────────────────────────────────────────────────────────────────

public class ResumenContable
{
    public int Anio { get; set; }
    public decimal TotalIngresos { get; set; }
    public decimal TotalGastos { get; set; }
    public decimal GastosPendientes { get; set; }
    public decimal Saldo { get; set; }
    public decimal SaldoAcumulado { get; set; }
    public int NumCuotas { get; set; }
    public int NumFacturas { get; set; }
    public int FacturasPendientes { get; set; }
    public List<GastoCategoria> GastosPorCategoria { get; set; } = new();
    public List<MovimientoMensual> MovimientosMensuales { get; set; } = new();
}

public class GastoCategoria
{
    public CategoriaGasto Categoria { get; set; }
    public decimal Total { get; set; }
    public decimal Pagado { get; set; }
    public int Cantidad { get; set; }
}

public class MovimientoMensual
{
    public int Mes { get; set; }
    public decimal Ingresos { get; set; }
    public decimal Gastos { get; set; }
    public string NombreMes => new DateTime(2000, Mes, 1).ToString("MMM");
}

public enum TipoApunte { Ingreso, Gasto }

public class ApunteContable
{
    public DateTime Fecha { get; set; }
    public TipoApunte Tipo { get; set; }
    public string Concepto { get; set; } = string.Empty;
    public string Detalle { get; set; } = string.Empty;
    public string Referencia { get; set; } = string.Empty;
    public decimal Debe { get; set; }
    public decimal Haber { get; set; }
    public decimal SaldoAcumulado { get; set; }
    public bool Pendiente { get; set; }
    public string? Categoria { get; set; }
    public string Origen { get; set; } = string.Empty;
    public int OrigenId { get; set; }
}
