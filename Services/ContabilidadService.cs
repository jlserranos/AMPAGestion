using Microsoft.EntityFrameworkCore;
using AMPAGestion.Data;
using AMPAGestion.Models;

namespace AMPAGestion.Services;

public class ContabilidadService
{
    private readonly IDbContextFactory<ApplicationDbContext> _factory;

    public ContabilidadService(IDbContextFactory<ApplicationDbContext> factory)
        => _factory = factory;

    // ── Resumen por curso (incluye apuntes manuales) ──────────────────────
    public async Task<ResumenContable> GetResumenAsync(string? curso = null)
    {
        curso ??= CursoHelper.GetCursoActual();
        var desde = CursoHelper.GetFechaInicio(curso);
        var hasta = CursoHelper.GetFechaFin(curso);

        await using var db = await _factory.CreateDbContextAsync();

        var cuotas   = await db.Cuotas.Where(c => c.Fecha >= desde && c.Fecha <= hasta).ToListAsync();
        var facturas = await db.Facturas.Where(f => f.Fecha >= desde && f.Fecha <= hasta).ToListAsync();
        var subs     = await db.Subvenciones.Where(s => s.Cobrado && s.Fecha >= desde && s.Fecha <= hasta).ToListAsync();

        // Apuntes manuales del curso (excluir CierreEjercicio para no duplicar)
        var manuales = await db.ApuntesManuales
            .Where(a => a.CursoAcademico == curso &&
                        a.TipoOperacion != TipoApunteManual.CierreEjercicio)
            .ToListAsync();

        var ingManuales = manuales.Where(a => a.Tipo == TipoApunte.Ingreso).Sum(a => a.Importe);
        var gasManuales = manuales.Where(a => a.Tipo == TipoApunte.Gasto).Sum(a => a.Importe);

        var totalIngresos    = cuotas.Sum(c => c.Importe) + subs.Sum(s => s.Importe) + ingManuales;
        var totalGastos      = facturas.Where(f => f.Pagado).Sum(f => f.Total) + gasManuales;
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

        var movimientosMensuales = Enumerable.Range(1, 12).Select(i =>
        {
            int mes  = ((i - 1 + 6) % 12) + 1;
            int anio = mes >= 7 ? CursoHelper.GetAnioInicioCurso(curso) : CursoHelper.GetAnioInicioCurso(curso) + 1;
            var ing  = cuotas.Where(c => c.Fecha.Month == mes && c.Fecha.Year == anio).Sum(c => c.Importe)
                     + subs.Where(s => s.Fecha.Month == mes && s.Fecha.Year == anio).Sum(s => s.Importe)
                     + manuales.Where(a => a.Tipo == TipoApunte.Ingreso && a.Fecha.Month == mes && a.Fecha.Year == anio).Sum(a => a.Importe);
            var gas  = facturas.Where(f => f.Fecha.Month == mes && f.Fecha.Year == anio && f.Pagado).Sum(f => f.Total)
                     + manuales.Where(a => a.Tipo == TipoApunte.Gasto && a.Fecha.Month == mes && a.Fecha.Year == anio).Sum(a => a.Importe);
            return new MovimientoMensual { Mes = mes, Anio = anio, Ingresos = ing, Gastos = gas };
        }).ToList();

        // Saldo acumulado de cursos anteriores
        var cursosAnt = CursoHelper.GetCursosDisponibles()
            .Where(c => CursoHelper.GetAnioInicioCurso(c) < CursoHelper.GetAnioInicioCurso(curso))
            .ToList();

        decimal saldoAcumulado = 0;
        foreach (var ca in cursosAnt)
        {
            var d = CursoHelper.GetFechaInicio(ca);
            var h = CursoHelper.GetFechaFin(ca);
            var ing = await db.Cuotas.Where(c => c.Fecha >= d && c.Fecha <= h).SumAsync(c => c.Importe)
                    + await db.Subvenciones.Where(s => s.Cobrado && s.Fecha >= d && s.Fecha <= h).SumAsync(s => s.Importe)
                    + await db.ApuntesManuales.Where(a => a.CursoAcademico == ca && a.Tipo == TipoApunte.Ingreso && a.TipoOperacion != TipoApunteManual.CierreEjercicio).SumAsync(a => a.Importe);
            var gas = await db.Facturas.Where(f => f.Pagado && f.Fecha >= d && f.Fecha <= h).SumAsync(f => f.BaseImponible + f.IVA)
                    + await db.ApuntesManuales.Where(a => a.CursoAcademico == ca && a.Tipo == TipoApunte.Gasto).SumAsync(a => a.Importe);
            saldoAcumulado += ing - gas;
        }

        return new ResumenContable
        {
            Curso                = curso,
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

    // ── Libro de cuentas ──────────────────────────────────────────────────
    public async Task<List<ApunteContable>> GetLibroAsync(string? curso = null)
    {
        await using var db = await _factory.CreateDbContextAsync();

        IQueryable<Cuota>       cuotas;
        IQueryable<Factura>     facturas;
        IQueryable<Subvencion>  subvenciones;
        IQueryable<ApunteManual> manuales;

        if (!string.IsNullOrEmpty(curso))
        {
            var desde = CursoHelper.GetFechaInicio(curso);
            var hasta = CursoHelper.GetFechaFin(curso);
            cuotas       = db.Cuotas.Include(c => c.Socio).Where(c => c.Fecha >= desde && c.Fecha <= hasta);
            facturas     = db.Facturas.Where(f => f.Fecha >= desde && f.Fecha <= hasta);
            subvenciones = db.Subvenciones.Where(s => s.Fecha >= desde && s.Fecha <= hasta);
            manuales     = db.ApuntesManuales.Where(a => a.CursoAcademico == curso);
        }
        else
        {
            cuotas       = db.Cuotas.Include(c => c.Socio);
            facturas     = db.Facturas;
            subvenciones = db.Subvenciones;
            manuales     = db.ApuntesManuales;
        }

        var apuntesCuotas = (await cuotas.ToListAsync()).Select(c => new ApunteContable
        {
            Fecha      = c.Fecha,
            Tipo       = TipoApunte.Ingreso,
            Concepto   = c.Concepto,
            Detalle    = c.Socio?.NombreCompleto ?? "",
            Referencia = c.Referencia ?? "",
            Haber      = c.Importe,
            Origen     = "Cuota",
            OrigenId   = c.Id
        });

        var apuntesSubs = (await subvenciones.ToListAsync()).Select(s => new ApunteContable
        {
            Fecha      = s.FechaCobro ?? s.Fecha,
            Tipo       = TipoApunte.Ingreso,
            Concepto   = s.Concepto,
            Detalle    = s.OrigenDescripcion,
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
            Pendiente  = !f.Pagado,
            Categoria  = f.Categoria.ToString(),
            Origen     = "Factura",
            OrigenId   = f.Id
        });

        var apuntesManuales = (await manuales.ToListAsync()).Select(a => new ApunteContable
        {
            Fecha      = a.Fecha,
            Tipo       = a.Tipo,
            Concepto   = a.Concepto,
            Detalle    = a.TipoOperacionDescripcion,
            Referencia = a.Referencia ?? "",
            Haber      = a.Tipo == TipoApunte.Ingreso ? a.Importe : 0,
            Debe       = a.Tipo == TipoApunte.Gasto   ? a.Importe : 0,
            Categoria  = a.TipoOperacionDescripcion,
            Origen     = "Manual",
            OrigenId   = a.Id
        });

        // Calcular saldo acumulado en orden ASCENDENTE (del más antiguo al más nuevo)
        var lista = apuntesCuotas
            .Concat(apuntesSubs)
            .Concat(apuntesFacturas)
            .Concat(apuntesManuales)
            .OrderBy(a => a.Fecha)
            .ThenBy(a => a.OrigenId)   // desempate consistente dentro del mismo día
            .ToList();

        decimal saldo = 0;
        foreach (var a in lista)
        {
            saldo += a.Haber - a.Debe;
            a.SaldoAcumulado = saldo;
        }

        // Para mostrar: orden DESCENDENTE por fecha y dentro del mismo día
        // por SaldoAcumulado descendente → el último apunte del día aparece primero
        return lista
            .OrderByDescending(a => a.Fecha)
            .ThenByDescending(a => a.SaldoAcumulado)
            .ToList();
    }

    // ── Cuotas ────────────────────────────────────────────────────────────
    public async Task<List<Cuota>> GetCuotasAsync(string? curso = null)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var q = db.Cuotas.Include(c => c.Socio).AsQueryable();
        if (!string.IsNullOrEmpty(curso))
        {
            var desde = CursoHelper.GetFechaInicio(curso);
            var hasta = CursoHelper.GetFechaFin(curso);
            q = q.Where(c => c.Fecha >= desde && c.Fecha <= hasta);
        }
        return await q.OrderByDescending(c => c.Fecha).ToListAsync();
    }

    public async Task<Cuota> RegistrarCuotaAsync(Cuota cuota)
    {
        await using var db = await _factory.CreateDbContextAsync();
        db.Cuotas.Add(cuota);
        var socio = await db.Socios.FindAsync(cuota.SocioId);
        if (socio != null) socio.Estado = EstadoPago.Pagado;
        await db.SaveChangesAsync();
        return cuota;
    }

    public async Task EliminarCuotaAsync(int id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var cuota = await db.Cuotas.FindAsync(id);
        if (cuota != null) { db.Cuotas.Remove(cuota); await db.SaveChangesAsync(); }
    }

    // ── Cursos disponibles ────────────────────────────────────────────────
    public async Task<List<string>> GetCursosDisponiblesAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        var fechas = await db.Cuotas.Select(c => c.Fecha)
            .Union(db.Facturas.Select(f => f.Fecha))
            .Union(db.Subvenciones.Select(s => s.Fecha))
            .ToListAsync();

        var cursos = fechas
            .Select(f => CursoHelper.GetCursoDeFecha(f))
            .Distinct()
            .OrderByDescending(c => c)
            .ToList();

        var actual = CursoHelper.GetCursoActual();
        if (!cursos.Contains(actual)) cursos.Insert(0, actual);
        return cursos;
    }
}

// ── DTOs ──────────────────────────────────────────────────────────────────
public class ResumenContable
{
    public string  Curso             { get; set; } = "";
    public decimal TotalIngresos     { get; set; }
    public decimal TotalGastos       { get; set; }
    public decimal GastosPendientes  { get; set; }
    public decimal Saldo             { get; set; }
    public decimal SaldoAcumulado    { get; set; }
    public int     NumCuotas         { get; set; }
    public int     NumFacturas       { get; set; }
    public int     FacturasPendientes { get; set; }
    public List<GastoCategoria>    GastosPorCategoria   { get; set; } = new();
    public List<MovimientoMensual> MovimientosMensuales { get; set; } = new();
}

public class GastoCategoria
{
    public CategoriaGasto Categoria { get; set; }
    public decimal Total    { get; set; }
    public decimal Pagado   { get; set; }
    public int     Cantidad { get; set; }
}

public class MovimientoMensual
{
    public int     Mes      { get; set; }
    public int     Anio     { get; set; }
    public decimal Ingresos { get; set; }
    public decimal Gastos   { get; set; }
    public string  NombreMes => new DateTime(Anio, Mes, 1).ToString("MMM yyyy");
}

public class ApunteContable
{
    public DateTime   Fecha           { get; set; }
    public TipoApunte Tipo            { get; set; }
    public string     Concepto        { get; set; } = "";
    public string     Detalle         { get; set; } = "";
    public string     Referencia      { get; set; } = "";
    public decimal    Debe            { get; set; }
    public decimal    Haber           { get; set; }
    public decimal    SaldoAcumulado  { get; set; }
    public bool       Pendiente       { get; set; }
    public string?    Categoria       { get; set; }
    public string     Origen          { get; set; } = "";
    public int        OrigenId        { get; set; }
}
