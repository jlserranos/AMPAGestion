using Microsoft.EntityFrameworkCore;
using AMPAGestion.Models;

namespace AMPAGestion.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<Socio>              Socios       { get; set; }
    public DbSet<Alumno>             Alumnos      { get; set; }
    public DbSet<Cuota>              Cuotas       { get; set; }
    public DbSet<Factura>            Facturas     { get; set; }
    public DbSet<Actividad>          Actividades  { get; set; }
    public DbSet<Documento>          Documentos   { get; set; }
    public DbSet<Subvencion>         Subvenciones { get; set; }
    public DbSet<ContactoSubvencion> ContactosSubvencion { get; set; }
    public DbSet<AlertaFecha>        AlertasFecha { get; set; }
    public DbSet<Prevision>          Previsiones  { get; set; }
    public DbSet<Proveedor>          Proveedores  { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Socio>(e =>
        {
            e.HasIndex(s => s.Email).IsUnique();
            e.HasIndex(s => s.DNI).IsUnique();
        });

        builder.Entity<Alumno>(e =>
        {
            e.HasOne(a => a.Socio)
             .WithMany(s => s.Alumnos)
             .HasForeignKey(a => a.SocioId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Cuota>(e =>
        {
            e.Property(c => c.Importe).HasPrecision(10, 2);
            e.HasOne(c => c.Socio)
             .WithMany(s => s.Cuotas)
             .HasForeignKey(c => c.SocioId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Factura>(e =>
        {
            e.Property(f => f.BaseImponible).HasPrecision(10, 2);
            e.Property(f => f.IVA).HasPrecision(10, 2);
        });

        builder.Entity<Actividad>(e =>
        {
            e.Property(a => a.Precio).HasPrecision(8, 2);
        });

        builder.Entity<Subvencion>(e =>
        {
            e.Property(s => s.Importe).HasPrecision(10, 2);
        });

        builder.Entity<Prevision>(e =>
        {
            e.Property(p => p.ImportePrevisto).HasPrecision(10, 2);
            e.Property(p => p.Enero).HasPrecision(10,2);
            e.Property(p => p.Febrero).HasPrecision(10,2);
            e.Property(p => p.Marzo).HasPrecision(10,2);
            e.Property(p => p.Abril).HasPrecision(10,2);
            e.Property(p => p.Mayo).HasPrecision(10,2);
            e.Property(p => p.Junio).HasPrecision(10,2);
            e.Property(p => p.Julio).HasPrecision(10,2);
            e.Property(p => p.Agosto).HasPrecision(10,2);
            e.Property(p => p.Septiembre).HasPrecision(10,2);
            e.Property(p => p.Octubre).HasPrecision(10,2);
            e.Property(p => p.Noviembre).HasPrecision(10,2);
            e.Property(p => p.Diciembre).HasPrecision(10,2);
        });

        // AlertaFecha genérica — FKs nullable
        builder.Entity<AlertaFecha>(e =>
        {
            e.HasOne(a => a.Subvencion)
             .WithMany(s => s.Alertas)
             .HasForeignKey(a => a.SubvencionId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(a => a.Factura)
             .WithMany(f => f.Alertas)
             .HasForeignKey(a => a.FacturaId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(a => a.Documento)
             .WithMany(d => d.Alertas)
             .HasForeignKey(a => a.DocumentoId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(a => a.Actividad)
             .WithMany(a => a.Alertas)
             .HasForeignKey(a => a.ActividadId)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ContactoSubvencion>(e =>
        {
            e.HasOne(c => c.Subvencion)
             .WithMany(s => s.Contactos)
             .HasForeignKey(c => c.SubvencionId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Seed data
        builder.Entity<Socio>().HasData(
            new Socio { Id = 1, Apellidos = "García López",  Nombre = "Ana",    Email = "ana.garcia@email.com", Telefono = "600111222", Estado = EstadoPago.Pagado,    FechaAlta = new DateTime(2024, 9, 1) },
            new Socio { Id = 2, Apellidos = "Martínez Ruiz", Nombre = "Carlos", Email = "carlos.m@email.com",   Telefono = "600333444", Estado = EstadoPago.Pendiente, FechaAlta = new DateTime(2024, 9, 3) },
            new Socio { Id = 3, Apellidos = "Fernández Gil", Nombre = "Lucía",  Email = "lucia.f@email.com",    Telefono = "600555666", Estado = EstadoPago.Pagado,    FechaAlta = new DateTime(2024, 9, 5) }
        );
        builder.Entity<Alumno>().HasData(
            new Alumno { Id = 1, Nombre = "Pablo",  Apellidos = "García",    Curso = CursoEscolar.PrimeroESO,       SocioId = 1, Activo = true },
            new Alumno { Id = 2, Nombre = "Lucía",  Apellidos = "García",    Curso = CursoEscolar.CuartoESO,        SocioId = 1, Activo = true },
            new Alumno { Id = 3, Nombre = "Diego",  Apellidos = "Martínez",  Curso = CursoEscolar.SegundoESO,       SocioId = 2, Activo = true },
            new Alumno { Id = 4, Nombre = "Sara",   Apellidos = "Fernández", Curso = CursoEscolar.PrimeroBachiller, SocioId = 3, Activo = true }
        );
        builder.Entity<Cuota>().HasData(
            new Cuota { Id = 1, Fecha = new DateTime(2024, 9, 10), Concepto = "Cuota anual 2024-25", Importe = 25m, Metodo = MetodoPago.Bizum,        SocioId = 1 },
            new Cuota { Id = 2, Fecha = new DateTime(2024, 9, 12), Concepto = "Cuota anual 2024-25", Importe = 25m, Metodo = MetodoPago.Transferencia, SocioId = 3 }
        );
        builder.Entity<Factura>().HasData(
            new Factura { Id = 1, Fecha = new DateTime(2024, 10, 5),  Proveedor = "Imprenta Rápida S.L.", Concepto = "Folletos Halloween",     BaseImponible = 80m,  IVA = 16.80m, PorcentajeIVA = 21, Categoria = CategoriaGasto.Comunicacion, Pagado = true,  FechaPago = new DateTime(2024, 10, 10) },
            new Factura { Id = 2, Fecha = new DateTime(2024, 11, 15), Proveedor = "Material Escolar",     Concepto = "Taller de ciencias",     BaseImponible = 150m, IVA = 31.50m, PorcentajeIVA = 21, Categoria = CategoriaGasto.Material,     Pagado = false },
            new Factura { Id = 3, Fecha = new DateTime(2024, 12, 1),  Proveedor = "Seguridad Plus",       Concepto = "Seguro responsabilidad", BaseImponible = 200m, IVA = 0m,     PorcentajeIVA = 0,  Categoria = CategoriaGasto.Seguros,      Pagado = true,  FechaPago = new DateTime(2024, 12, 5) }
        );
        builder.Entity<Proveedor>().HasData(
            new Proveedor { Id = 1, Nombre = "Imprenta Rápida S.L.", CategoriaHabitual = CategoriaGasto.Comunicacion, IVAHabitual = 21, UltimoUso = new DateTime(2024, 10, 5) },
            new Proveedor { Id = 2, Nombre = "Material Escolar",     CategoriaHabitual = CategoriaGasto.Material,     IVAHabitual = 21, UltimoUso = new DateTime(2024, 11, 15) },
            new Proveedor { Id = 3, Nombre = "Seguridad Plus",       CategoriaHabitual = CategoriaGasto.Seguros,      IVAHabitual = 0,  UltimoUso = new DateTime(2024, 12, 1) }
        );
        builder.Entity<Actividad>().HasData(
            new Actividad { Id = 1, Nombre = "Taller de Navidad", FechaInicio = new DateTime(2024, 12, 20), Lugar = "Salón de actos", Precio = 5m, Activa = true }
        );
        builder.Entity<Documento>().HasData(
            new Documento { Id = 1, Titulo = "Estatutos del AMPA", Tipo = TipoDocumento.Estatutos, FechaSubida = new DateTime(2024, 9, 1), Publico = true }
        );
    }
}
