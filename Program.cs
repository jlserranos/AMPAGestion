using Microsoft.EntityFrameworkCore;
using AMPAGestion.Data;
using AMPAGestion.Services;
using Blazored.Toast;

// Necesario para que Npgsql acepte DateTime sin zona horaria explícita
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Puerto dinámico para Railway
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// PostgreSQL — DATABASE_URL en Railway, cadena local en desarrollo
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
string connStr;

if (!string.IsNullOrEmpty(databaseUrl))
{
    try
    {
        var uri  = new Uri(databaseUrl);
        var user = uri.UserInfo.Split(':');
        connStr = $"Host={uri.Host};Port={uri.Port};" +
                  $"Database={uri.AbsolutePath.TrimStart('/')};" +
                  $"Username={user[0]};Password={user[1]};" +
                  $"SSL Mode=Require;Trust Server Certificate=true";
        Console.WriteLine($"[DB] Conectando a {uri.Host}:{uri.Port}/{uri.AbsolutePath.TrimStart('/')}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[DB] Error parseando DATABASE_URL: {ex.Message}");
        Console.WriteLine($"[DB] DATABASE_URL recibida: {databaseUrl[..Math.Min(50, databaseUrl.Length)]}...");
        throw;
    }
}
else
{
    connStr = builder.Configuration.GetConnectionString("Default")!;
    Console.WriteLine("[DB] Usando cadena local (appsettings.json)");
}

builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseNpgsql(connStr));

// Servicios
builder.Services.AddScoped<SocioService>();
builder.Services.AddScoped<AlumnoService>();
builder.Services.AddScoped<ProveedorService>();
builder.Services.AddScoped<FacturaService>();
builder.Services.AddScoped<ContabilidadService>();
builder.Services.AddScoped<ActividadService>();
builder.Services.AddScoped<DocumentoService>();
builder.Services.AddScoped<SubvencionService>();
builder.Services.AddScoped<PrevisionService>();
builder.Services.AddScoped<ImagenService>();
builder.Services.AddScoped<ExportService>();

builder.Services.AddBlazoredToast();

var app = builder.Build();

// Crear tablas al arrancar
using (var scope = app.Services.CreateScope())
{
    var factory = scope.ServiceProvider
        .GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
    using var db = factory.CreateDbContext();
    db.Database.EnsureCreated();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
