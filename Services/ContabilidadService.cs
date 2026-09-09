using Microsoft.EntityFrameworkCore;
using AMPAGestion.Data;
using AMPAGestion.Models;

namespace AMPAGestion.Services;

public class ContabilidadService
{
    private readonly IDbContextFactory<ApplicationDbContext> _factory;

    public ContabilidadService(IDbContextFactory<ApplicationDbContext> factory)
        => _factory = factory;

    // ── Resumen anual por curso ────────────────────────────────────────────
    public async Task<ResumenContable> GetResumenAsync(string? curso = null)
    {
        curso ??= CursoHelper.GetCursoActual();
        var desde = CursoHelper.GetFechaInicio(curso);
        var hasta = CursoHelper.GetFechaFin(curso);

        await using var db = await _factory.CreateDbContextAsync();

        var cuotas   = await db.Cuotas.Where(c => c.Fecha >= desde && c.Fecha <= hasta).ToListAsync();
        var facturas = await db.Facturas.Where(f => f.Fecha >= desde && f.Fecha <= hasta).ToListAsync();
        var subs     = await db.Subvenciones.Where(s => s.Cobrado && s.Fecha >= desde && s.Fecha <= hasta).ToListAsync();

        var totalIngresos    = cuotas.Sum(c => c.Importe) + subs.Sum(s => s.Importe);
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

        var movimientosMensuales = Enumerable.Range(1, 12).Select(i =>
        {
            // Los meses del curso van de julio(7) a junio(6)
            int mes     = ((i - 1 + 6) % 12) + 1; // jul=1, ago=2, ... jun=12
            int anio    = mes >= 7 ? CursoHelper.GetAnioInicioCurso(curso) : CursoHelper.GetAnioInicioCurso(curso) + 1;
            var ingresos = cuotas.Where(c => c.Fecha.Month == mes && c.Fecha.Year == anio).Sum(c => c.Importe)
                         + subs.Where(s => s.Fecha.Month == mes && s.Fecha.Year == anio).Sum(s => s.Importe);
            var gastos   = facturas.Where(f => f.Fecha.Month == mes && f.Fecha.Year == anio && f.Pagado).Sum(f => f.Total);
            return new MovimientoMensual { Mes = mes, Anio = anio, Ingresos = ingresos, Gastos = gastos };
        }).ToList();

        // Saldo acumulado de cursos anteriores
        var cursosAnteriores = CursoHelper.GetCursosDisponibles()
            .Where(c => CursoHelper.GetAnioInicioCurso(c) < CursoHelper.GetAnioInicioCurso(curso))
            .ToList();

        decimal saldoAcumulado = 0;
        foreach (var ca in cursosAnteriores)
        {
            var desdeCa = CursoHelper.GetFechaInicio(ca);
            var hastaCa = CursoHelper.GetFechaFin(ca);
            var ing = await db.Cuotas.Where(c => c.Fecha >= desdeCa && c.Fecha <= hastaCa).SumAsync(c => c.Importe);
            var gas = await db.Facturas.Where(f => f.Fecha >= desdeCa && f.Fecha <= hastaCa && f.Pagado).SumAsync(f => f.BaseImponible + f.IVA);
            var sub = await db.Subvenciones.Where(s => s.Cobrado && s.Fecha >= desdeCa && s.Fecha <= hastaCa).SumAsync(s => s.Importe);
            saldoAcumulado += ing + sub - gas;
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

    // ── Libro de cuentas unificado ─────────────────────────────────────────
    public async Task<List<ApunteContable>> GetLibroAsync(string? curso = null)
    {
        await using var db = await _factory.CreateDbContextAsync();

        IQueryable<Cuota>      cuotas;
        IQueryable<Factura>    facturas;
        IQueryable<Subvencion> subvenciones;

        if (!string.IsNullOrEmpty(curso))
        {
            var desde = CursoHelper.GetFechaInicio(curso);
            var hasta = CursoHelper.GetFechaFin(curso);
            cuotas       = db.Cuotas.Include(c => c.Socio).Where(c => c.Fecha >= desde && c.Fecha <= hasta);
            facturas     = db.Facturas.Where(f => f.Fecha >= desde && f.Fecha <= hasta);
            subvenciones = db.Subvenciones.Where(s => s.Fecha >= desde && s.Fecha <= hasta);
        }
        else
        {
            cuotas       = db.Cuotas.Include(c => c.Socio);
            facturas     = db.Facturas;
            subvenciones = db.Subvenciones;
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

        var apuntesSubs = (await subvenciones.ToListAsync()).Select(s => new ApunteContable
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

        // Apuntes manuales (comisiones, intereses, aperturas, cierres, etc.)
        var apuntesManuales = (await (curso != null
            ? db.ApuntesManuales.Where(a => a.CursoAcademico == curso)
            : db.ApuntesManuales).ToListAsync()).Select(a => new ApunteContable
        {
            Fecha      = a.Fecha,
            Tipo       = a.Tipo,
            Concepto   = a.Concepto,
            Detalle    = a.TipoOperacionDescripcion,
            Referencia = a.Referencia ?? "",
            Debe       = a.Tipo == TipoApunte.Gasto ? a.Importe : 0,
            Haber      = a.Tipo == TipoApunte.Ingreso ? a.Importe : 0,
            Categoria  = a.TipoOperacionDescripcion,
            Origen     = "Manual",
            OrigenId   = a.Id
        });

        var lista = apuntesCuotas.Concat(apuntesSubs).Concat(apuntesFacturas).Concat(apuntesManuales)
            .OrderBy(a => a.Fecha).ThenBy(a => a.Tipo).ToList();

        decimal saldo = 0;
        foreach (var a in lista) { saldo += a.Haber - a.Debe; a.SaldoAcumulado = saldo; }

        return lista.OrderByDescending(a => a.Fecha).ToList();
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

    // ── Cursos disponibles (con datos reales en BD) ────────────────────────
    public async Task<List<string>> GetCursosDisponiblesAsync()
    {
        await using var db = await _factory.CreateDbContextAsync();
        var fechas = await db.Cuotas.Select(c => c.Fecha)
            .Union(db.Facturas.Select(f => f.Fecha))
            .Union(db.Subvenciones.Select(s => s.Fecha))
            .ToListAsync();

        var cursos = fechas.Select(f => CursoHelper.GetCursoDeFecha(f)).Distinct().OrderByDescending(c => c).ToList();

        // Asegurar que el curso actual siempre aparece
        var cursoActual = CursoHelper.GetCursoActual();
        if (!cursos.Contains(cursoActual)) cursos.Insert(0, cursoActual);

        return cursos;
    }
}

// ── DTOs ──────────────────────────────────────────────────────────────────
public class ResumenContable
{
    public string Curso               { get; set; } = "";
    public decimal TotalIngresos      { get; set; }
    public decimal TotalGastos        { get; set; }
    public decimal GastosPendientes   { get; set; }
    public decimal Saldo              { get; set; }
    public decimal SaldoAcumulado     { get; set; }
    public int NumCuotas              { get; set; }
    public int NumFacturas            { get; set; }
    public int FacturasPendientes     { get; set; }
    public List<GastoCategoria>    GastosPorCategoria   { get; set; } = new();
    public List<MovimientoMensual> MovimientosMensuales { get; set; } = new();
}

public class GastoCategoria
{
    public CategoriaGasto Categoria { get; set; }
    public decimal Total            { get; set; }
    public decimal Pagado           { get; set; }
    public int Cantidad             { get; set; }
}

public class MovimientoMensual
{
    public int Mes  { get; set; }
    public int Anio { get; set; }
    public decimal Ingresos { get; set; }
    public decimal Gastos   { get; set; }
    public string NombreMes => new DateTime(Anio, Mes, 1).ToString("MMM yyyy");
}

public enum TipoApunte { Ingreso, Gasto }

public class ApunteContable
{
    public DateTime Fecha           { get; set; }
    public TipoApunte Tipo          { get; set; }
    public string Concepto          { get; set; } = "";
    public string Detalle           { get; set; } = "";
    public string Referencia        { get; set; } = "";
    public decimal Debe             { get; set; }
    public decimal Haber            { get; set; }
    public decimal SaldoAcumulado   { get; set; }
    public bool Pendiente           { get; set; }
    public string? Categoria        { get; set; }
    public string Origen            { get; set; } = "";
    public int OrigenId             { get; set; }
}

// ── Extensión: operaciones contables ──────────────────────────────────────
public partial class ContabilidadOperacionesService
{
    private readonly IDbContextFactory<ApplicationDbContext> _factory;

    public ContabilidadOperacionesService(IDbContextFactory<ApplicationDbContext> factory)
        => _factory = factory;

    // Obtener apuntes manuales del curso
    public async Task<List<ApunteManual>> GetApuntesManualesAsync(string curso)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var desde = CursoHelper.GetFechaInicio(curso);
        var hasta = CursoHelper.GetFechaFin(curso);
        return await db.ApuntesManuales
            .Where(a => a.Fecha >= desde && a.Fecha <= hasta)
            .OrderByDescending(a => a.Fecha)
            .ToListAsync();
    }

    // Guardar apunte manual
    public async Task<ApunteManual> GuardarApunteAsync(ApunteManual apunte)
    {
        await using var db = await _factory.CreateDbContextAsync();
        if (apunte.Id == 0) db.ApuntesManuales.Add(apunte);
        else db.ApuntesManuales.Update(apunte);
        await db.SaveChangesAsync();
        return apunte;
    }

    public async Task EliminarApunteAsync(int id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var a = await db.ApuntesManuales.FindAsync(id);
        if (a != null) { db.ApuntesManuales.Remove(a); await db.SaveChangesAsync(); }
    }

    // Apertura de ejercicio: trae el saldo del curso anterior
    public async Task<ApunteManual> AperturaEjercicioAsync(string cursoNuevo)
    {
        await using var db = await _factory.CreateDbContextAsync();

        // Verificar que no existe ya una apertura para este curso
        var existe = await db.ApuntesManuales.AnyAsync(a =>
            a.CursoAcademico == cursoNuevo &&
            a.TipoOperacion == TipoApunteManual.AperturaEjercicio);
        if (existe) throw new InvalidOperationException($"Ya existe una apertura de ejercicio para el curso {cursoNuevo}.");

        // Calcular saldo del curso anterior
        var cursos = CursoHelper.GetCursosDisponibles();
        var idxActual = cursos.IndexOf(cursoNuevo);
        if (idxActual < 0 || idxActual >= cursos.Count - 1)
            throw new InvalidOperationException("No se puede determinar el curso anterior.");

        var cursoAnterior = cursos[idxActual + 1];
        var saldoAnterior = await CalcularSaldoCursoAsync(db, cursoAnterior);

        var apertura = new ApunteManual
        {
            Concepto       = $"Apertura ejercicio {cursoNuevo} — saldo de {cursoAnterior}",
            Fecha          = CursoHelper.GetFechaInicio(cursoNuevo),
            CursoAcademico = cursoNuevo,
            Tipo           = saldoAnterior >= 0 ? TipoApunte.Ingreso : TipoApunte.Gasto,
            TipoOperacion  = TipoApunteManual.AperturaEjercicio,
            Importe        = Math.Abs(saldoAnterior),
            Referencia     = $"Saldo {cursoAnterior}",
            EsAutomatico   = true
        };

        db.ApuntesManuales.Add(apertura);
        await db.SaveChangesAsync();
        return apertura;
    }

    // Cierre de ejercicio: genera el asiento de cierre
    public async Task<ResumenCierre> CierreEjercicioAsync(string curso)
    {
        await using var db = await _factory.CreateDbContextAsync();

        var saldo = await CalcularSaldoCursoAsync(db, curso);

        // Crear asiento de cierre
        var cierre = new ApunteManual
        {
            Concepto       = $"Cierre de ejercicio {curso}",
            Descripcion    = $"Saldo final del curso {curso}: {saldo:C}",
            Fecha          = CursoHelper.GetFechaFin(curso),
            CursoAcademico = curso,
            Tipo           = TipoApunte.Ingreso,
            TipoOperacion  = TipoApunteManual.CierreEjercicio,
            Importe        = Math.Abs(saldo),
            Referencia     = $"Cierre {curso}",
            EsAutomatico   = true
        };

        db.ApuntesManuales.Add(cierre);
        await db.SaveChangesAsync();

        // Calcular totales para el informe
        var desde = CursoHelper.GetFechaInicio(curso);
        var hasta = CursoHelper.GetFechaFin(curso);

        var totalCuotas   = await db.Cuotas.Where(c => c.Fecha >= desde && c.Fecha <= hasta).SumAsync(c => c.Importe);
        var totalSubs     = await db.Subvenciones.Where(s => s.Cobrado && s.Fecha >= desde && s.Fecha <= hasta).SumAsync(s => s.Importe);
        var apuntesIng    = await db.ApuntesManuales.Where(a => a.CursoAcademico == curso && a.Tipo == TipoApunte.Ingreso && a.TipoOperacion != TipoApunteManual.CierreEjercicio).SumAsync(a => a.Importe);
        var totalFacturas = await db.Facturas.Where(f => f.Pagado && f.Fecha >= desde && f.Fecha <= hasta).SumAsync(f => f.BaseImponible + f.IVA);
        var apuntesGas    = await db.ApuntesManuales.Where(a => a.CursoAcademico == curso && a.Tipo == TipoApunte.Gasto).SumAsync(a => a.Importe);

        return new ResumenCierre
        {
            Curso           = curso,
            TotalIngresos   = totalCuotas + totalSubs + apuntesIng,
            TotalGastos     = totalFacturas + apuntesGas,
            SaldoFinal      = saldo,
            FechaCierre     = DateTime.Today
        };
    }

    private async Task<decimal> CalcularSaldoCursoAsync(ApplicationDbContext db, string curso)
    {
        var desde = CursoHelper.GetFechaInicio(curso);
        var hasta = CursoHelper.GetFechaFin(curso);

        var ingresos = await db.Cuotas.Where(c => c.Fecha >= desde && c.Fecha <= hasta).SumAsync(c => c.Importe)
                     + await db.Subvenciones.Where(s => s.Cobrado && s.Fecha >= desde && s.Fecha <= hasta).SumAsync(s => s.Importe)
                     + await db.ApuntesManuales.Where(a => a.CursoAcademico == curso && a.Tipo == TipoApunte.Ingreso).SumAsync(a => a.Importe);
        var gastos   = await db.Facturas.Where(f => f.Pagado && f.Fecha >= desde && f.Fecha <= hasta).SumAsync(f => f.BaseImponible + f.IVA)
                     + await db.ApuntesManuales.Where(a => a.CursoAcademico == curso && a.Tipo == TipoApunte.Gasto).SumAsync(a => a.Importe);

        return ingresos - gastos;
    }
}

public class ResumenCierre
{
    public string Curso         { get; set; } = "";
    public decimal TotalIngresos { get; set; }
    public decimal TotalGastos   { get; set; }
    public decimal SaldoFinal    { get; set; }
    public DateTime FechaCierre  { get; set; }
}
