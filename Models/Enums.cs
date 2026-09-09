namespace AMPAGestion.Models;

public enum EstadoPago    { Pendiente, Pagado, Exento, Baja }
public enum MetodoPago    { Bizum, Transferencia, Efectivo, Domiciliacion }
public enum CategoriaGasto { Actividades, Material, Comunicacion, Servicios, Mantenimiento, Seguros, Otros }
public enum TipoDocumento { Acta, Estatutos, Normativa, Contrato, Presupuesto, Otro }

public enum CursoEscolar
{
    PrimeroESO, SegundoESO, TerceroESO, CuartoESO,
    PrimeroBachiller, SegundoBachiller,
    PrimeroGradoMedio, SegundoGradoSuperior
}

public enum OrigenSubvencion
{
    Diputacion,
    JuntaAndalucia,
    Ayuntamiento,
    Ministerio,
    FondosEuropeos,
    SaldoAnterior,
    Donacion,
    Otro
}
