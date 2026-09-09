# AMPA Gestión — ASP.NET Core Blazor Server

Aplicación web para la gestión integral de una AMPA: socios, alumnos, facturas y contabilidad.

## Requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 Community (o VS Code con extensión C#)

## Puesta en marcha

### 1. Restaurar paquetes

```bash
cd AMPAGestion
dotnet restore
```

### 2. Ejecutar la aplicación

```bash
dotnet run
```

La base de datos SQLite (`ampa.db`) se crea **automáticamente** con todas las tablas y datos de prueba la primera vez que arranca la app. No hace falta ningún comando adicional.

Luego abre: `https://localhost:5001` o `http://localhost:5000`

### 3. Con Visual Studio

Abre `AMPAGestion.csproj` → pulsa **F5**. La base de datos se crea sola al arrancar.

---

## Estructura del proyecto

```
AMPAGestion/
├── Models/          → Entidades de base de datos
│   ├── Socio.cs
│   ├── Alumno.cs
│   ├── Cuota.cs
│   ├── Factura.cs
│   └── Enums.cs
├── Data/
│   └── ApplicationDbContext.cs   → EF Core + datos de prueba (seed)
├── Services/        → Lógica de negocio
│   ├── SocioService.cs
│   ├── AlumnoService.cs
│   ├── FacturaService.cs
│   └── ContabilidadService.cs
├── Pages/           → Componentes Blazor (.razor)
│   ├── Index.razor              → Dashboard
│   ├── Socios/
│   │   ├── Index.razor          → Listado con filtros
│   │   ├── Detalle.razor        → Ficha + alumnos + cuotas
│   │   └── Formulario.razor     → Alta / edición
│   ├── Alumnos/
│   │   ├── Index.razor
│   │   └── Formulario.razor
│   ├── Facturas/
│   │   ├── Index.razor
│   │   └── Formulario.razor
│   └── Contabilidad/
│       └── Index.razor          → Resumen anual + tabla mensual
├── Shared/
│   ├── MainLayout.razor
│   └── NavMenu.razor
├── wwwroot/css/app.css
├── Program.cs
└── appsettings.json
```

## Base de datos

- Motor: **SQLite** (archivo `ampa.db` en la carpeta del proyecto)
- ORM: **Entity Framework Core 8**
- Los datos de prueba se cargan automáticamente al iniciar la app (4 socios, 5 alumnos, 2 cuotas, 3 facturas)

## Personalización

- Cambia el nombre del AMPA en `appsettings.json` → `"AMPA:Nombre"`
- Cambia la cuota anual por defecto en `appsettings.json` → `"AMPA:CuotaAnual"`
- Para usar **SQL Server** en lugar de SQLite: cambia `UseSqlite` por `UseSqlServer` en `Program.cs` y actualiza la cadena de conexión

## Funcionalidades incluidas

- ✅ Dashboard con KPIs y resumen económico
- ✅ Gestión de socios (CRUD completo + filtros)
- ✅ Gestión de alumnos vinculados a socios
- ✅ Registro de pagos/cuotas por socio
- ✅ Gestión de facturas de gasto con cálculo de IVA
- ✅ Marcar facturas como pagadas
- ✅ Contabilidad anual con desglose mensual y por categoría
- ✅ Notificaciones toast en todas las acciones
- ✅ Modales de confirmación para borrados
- ✅ Diseño responsive (Bootstrap 5)
- ✅ Datos de prueba precargados

## Posibles mejoras futuras

- Exportación a Excel / PDF
- Envío de emails de recordatorio de pago
- Importación de socios desde CSV
- Módulo de actividades y eventos
- Autenticación con roles (admin, tesorero, consulta)
- Subida de archivos PDF de facturas
