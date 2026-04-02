using Microsoft.EntityFrameworkCore;
using AMPAGestion.Data;
using AMPAGestion.Models;

namespace AMPAGestion.Services;

public class PrevisionService
{
    private readonly IDbContextFactory<ApplicationDbContext> _factory;

    public PrevisionService(IDbContextFactory<ApplicationDbContext> factory)
        => _factory = factory;

    public async Task<List<Prevision>> GetTodasAsync(int anio)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Previsiones
            .Where(p => p.Anio == anio)
            .OrderBy(p => p.Tipo)
            .ThenBy(p => p.Concepto)
            .ToListAsync();
    }

    public async Task<Prevision?> GetByIdAsync(int id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        return await db.Previsiones.FindAsync(id);
    }

    public async Task<Prevision> CrearAsync(Prevision p)
    {
        await using var db = await _factory.CreateDbContextAsync();
        db.Previsiones.Add(p);
        await db.SaveChangesAsync();
        return p;
    }

    public async Task ActualizarAsync(Prevision p)
    {
        await using var db = await _factory.CreateDbContextAsync();
        db.Previsiones.Update(p);
        await db.SaveChangesAsync();
    }

    public async Task EliminarAsync(int id)
    {
        await using var db = await _factory.CreateDbContextAsync();
        var p = await db.Previsiones.FindAsync(id);
        if (p != null) { db.Previsiones.Remove(p); await db.SaveChangesAsync(); }
    }

    public async Task<ComparativaPrevision> GetComparativaAsync(int anio)
    {
        await using var db = await _factory.CreateDbContextAsync();

        var previsiones = await db.Previsiones.Where(p => p.Anio == anio).ToListAsync();
        var cuotas      = await db.Cuotas.Where(c => c.Fecha.Year == anio).ToListAsync();
        var facturas    = await db.Facturas.Where(f => f.Fecha.Year == anio).ToListAsync();
        var subvenciones= await db.Subvenciones.Where(s => s.Fecha.Year == anio && s.Cobrado).ToListAsync();

        var realIngresos = cuotas.Sum(c => c.Importe) + subvenciones.Sum(s => s.Importe);
        var realGastos   = facturas.Where(f => f.Pagado).Sum(f => f.Total);

        var prevIngresos = previsiones.Where(p => p.Tipo == TipoPrevision.Ingreso).Sum(p => p.ImportePrevisto);
        var prevGastos   = previsiones.Where(p => p.Tipo == TipoPrevision.Gasto).Sum(p => p.ImportePrevisto);

        var filas = Enumerable.Range(1, 12).Select(mes =>
        {
            var prevIngMes  = previsiones.Where(p => p.Tipo == TipoPrevision.Ingreso).Sum(p => p.GetMes(mes));
            var prevGasMes  = previsiones.Where(p => p.Tipo == TipoPrevision.Gasto).Sum(p => p.GetMes(mes));
            var realIngMes  = cuotas.Where(c => c.Fecha.Month == mes).Sum(c => c.Importe)
                            + subvenciones.Where(s => (s.FechaCobro ?? s.Fecha).Month == mes).Sum(s => s.Importe);
            var realGasMes  = facturas.Where(f => f.Pagado && f.Fecha.Month == mes).Sum(f => f.Total);
            return new FilaComparativa
            {
                Mes            = mes,
                PrevIngresos   = prevIngMes,
                PrevGastos     = prevGasMes,
                RealIngresos   = realIngMes,
                RealGastos     = realGasMes
            };
        }).ToList();

        // Detalle por línea de previsión
        var detalle = previsiones.Select(p =>
        {
            decimal real = 0;
            if (p.Tipo == TipoPrevision.Ingreso)
                real = p.Categoria == null
                    ? cuotas.Sum(c => c.Importe) + subvenciones.Sum(s => s.Importe)
                    : 0;
            else
                real = p.Categoria.HasValue
                    ? facturas.Where(f => f.Pagado && f.Categoria == p.Categoria.Value).Sum(f => f.Total)
                    : facturas.Where(f => f.Pagado).Sum(f => f.Total);

            return new DetallePrevision
            {
                Id             = p.Id,
                Concepto       = p.Concepto,
                Tipo           = p.Tipo,
                Categoria      = p.Categoria?.ToString(),
                ImportePrevisto= p.ImportePrevisto,
                ImporteReal    = real,
                Desviacion     = real - p.ImportePrevisto,
                PctEjecucion   = p.ImportePrevisto > 0
                    ? Math.Round(real / p.ImportePrevisto * 100, 1)
                    : 0
            };
        }).ToList();

        return new ComparativaPrevision
        {
            Anio            = anio,
            PrevIngresos    = prevIngresos,
            PrevGastos      = prevGastos,
            RealIngresos    = realIngresos,
            RealGastos      = realGastos,
            FilasMensuales  = filas,
            Detalle         = detalle
        };
    }
}

public class ComparativaPrevision
{
    public int Anio { get; set; }
    public decimal PrevIngresos { get; set; }
    public decimal PrevGastos   { get; set; }
    public decimal RealIngresos { get; set; }
    public decimal RealGastos   { get; set; }
    public decimal DesvIngresos => RealIngresos - PrevIngresos;
    public decimal DesvGastos   => RealGastos   - PrevGastos;
    public decimal PrevSaldo    => PrevIngresos - PrevGastos;
    public decimal RealSaldo    => RealIngresos - RealGastos;
    public List<FilaComparativa>  FilasMensuales { get; set; } = new();
    public List<DetallePrevision> Detalle        { get; set; } = new();
}

public class FilaComparativa
{
    public int Mes { get; set; }
    public decimal PrevIngresos { get; set; }
    public decimal PrevGastos   { get; set; }
    public decimal RealIngresos { get; set; }
    public decimal RealGastos   { get; set; }
    public decimal PrevSaldo    => PrevIngresos - PrevGastos;
    public decimal RealSaldo    => RealIngresos - RealGastos;
    public string NombreMes     => new DateTime(2000, Mes, 1).ToString("MMMM");
    public bool TieneDatos      => PrevIngresos > 0 || PrevGastos > 0 || RealIngresos > 0 || RealGastos > 0;
}

public class DetallePrevision
{
    public int Id { get; set; }
    public string Concepto        { get; set; } = string.Empty;
    public TipoPrevision Tipo     { get; set; }
    public string? Categoria      { get; set; }
    public decimal ImportePrevisto { get; set; }
    public decimal ImporteReal    { get; set; }
    public decimal Desviacion     { get; set; }
    public decimal PctEjecucion   { get; set; }
}
