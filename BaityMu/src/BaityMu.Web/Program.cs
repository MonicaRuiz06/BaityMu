// =====================================================================================
// BaityMu.Web — Program.cs
// Punto de entrada de la API y RAÍZ DE COMPOSICIÓN: aquí se conectan todas las capas.
//
// Tiene dos partes:
//   1. builder.Services...  → registrar servicios en el contenedor de inyección de
//                             dependencias (qué clase entregar cuando alguien pida algo).
//   2. app.Use... / Map...  → armar el "pipeline": el camino por el que pasa cada
//                             petición HTTP, en orden, hasta llegar a un controller.
//
// Es el único lugar de Web que conoce Infrastructure (para registrarla). Los controllers
// nunca usan el DbContext: hablan solo con los servicios (RNF-02).
// =====================================================================================

using System.Reflection;
using System.Text.Json.Serialization;
using BaityMu.Domain.Exceptions;
using BaityMu.Infrastructure;
using BaityMu.Domain.Interfaces;
using BaityMu.Infrastructure.Data;
using BaityMu.Infrastructure.Seed;
using BaityMu.Services;
using BaityMu.Web.Mapping;
using BaityMu.Web.Middleware;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// -------------------------------------------------------------------------------------
// 1. SERVICIOS (inyección de dependencias)
// -------------------------------------------------------------------------------------

// Capas propias:
//   - Infrastructure: DbContext con SQL Server, UnitOfWork y SystemClock.
//   - Services: los servicios de negocio de cada historia.
builder.Services.AddRepositories(builder.Configuration);
builder.Services.AddServices();

// Controllers. Los enums viajan como texto en el JSON ("Queso") y no como número (1),
// así la API es más fácil de leer y de usar (RNF-05).
builder.Services
    .AddControllers()
    .AddJsonOptions(opciones =>
        opciones.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Lo mismo para los endpoints mínimos definidos aquí mismo (MapGet).
builder.Services.ConfigureHttpJsonOptions(opciones =>
    opciones.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// AutoMapper: convierte entidades del dominio en DTOs. Los mapeos están en BaityMuProfile.
builder.Services.AddAutoMapper(configuracion => configuracion.AddProfile<BaityMuProfile>());

// Manejo centralizado de errores: las excepciones del dominio se convierten en
// ProblemDetails con 400, 404 o 409 (ver Middleware/ManejadorGlobalDeErrores.cs).
builder.Services.AddProblemDetails(opciones =>
    opciones.CustomizeProblemDetails = contexto =>
        contexto.ProblemDetails.Extensions["traceId"] = contexto.HttpContext.TraceIdentifier);
builder.Services.AddExceptionHandler<ManejadorGlobalDeErrores>();

// CORS: permite que una página web de otro dominio (por ejemplo un frontend) consuma la
// API desde el navegador, igual que en el ejemplo del profesor (RNF-13).
const string PoliticaCors = "BaityMuCors";
builder.Services.AddCors(opciones =>
    opciones.AddPolicy(PoliticaCors, politica =>
        politica.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

// Swagger / OpenAPI: documentación interactiva de la API en /swagger (RNF-07).
// Toma los comentarios /// de los proyectos que generan su archivo XML de documentación.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opciones =>
{
    opciones.SwaggerDoc("v1", new()
    {
        Title = "BaityMu API",
        Version = "v1",
        Description =
            "<img src=\"/img/logo-baitymu.png\" alt=\"BaityMu\" width=\"150\" />\n\n" +
            "API de la granja lechera de **Baity**: vacas, producción de leche, transformación " +
            "en queso o yogurt, inventario por lotes con salida **FIFO**, ventas y reportes.\n\n" +
            "Taller final de Lenguajes de Programación — Universidad Santiago de Cali."
    });

    foreach (var archivoXml in Directory.GetFiles(AppContext.BaseDirectory, "BaityMu.*.xml"))
    {
        opciones.IncludeXmlComments(archivoXml);
    }
});

var app = builder.Build();

// -------------------------------------------------------------------------------------
// 2. BASE DE DATOS: aplicar las migraciones y sembrar la granja al arrancar
// -------------------------------------------------------------------------------------
// Crea la base de datos BaityMu en LocalDB (si no existe) y aplica las migraciones que
// falten, para no tener que correr "dotnet ef database update" a mano.
// Después, si no hay granja, la crea con sus n vacas desde la sección GranjaInicial de
// appsettings.json (RNF-09).
using (var alcance = app.Services.CreateScope())
{
    var db = alcance.ServiceProvider.GetRequiredService<BaityMuDbContext>();
    if (db.Database.GetMigrations().Any())
    {
        await db.Database.MigrateAsync();
    }

    var granjaInicial = app.Configuration
        .GetSection(GranjaInicialOptions.Seccion)
        .Get<GranjaInicialOptions>() ?? new GranjaInicialOptions();
    var reloj = alcance.ServiceProvider.GetRequiredService<IClock>();
    await SembradorDeDatos.SembrarAsync(db, granjaInicial, reloj);
}

// -------------------------------------------------------------------------------------
// 3. PIPELINE HTTP (el orden importa)
// -------------------------------------------------------------------------------------

// Primero el manejador de errores, para que atrape lo que falle en todo lo que sigue.
app.UseExceptionHandler();

// Respuestas de error sin cuerpo (por ejemplo, un 404 de una ruta que no existe) también
// salen en formato ProblemDetails.
app.UseStatusCodePages();

// Swagger siempre disponible: es parte de la entrega del taller.
app.UseSwagger();
app.UseSwaggerUI(opciones =>
{
    opciones.DocumentTitle = "BaityMu API";
    opciones.SwaggerEndpoint("/swagger/v1/swagger.json", "BaityMu API v1");
});

// Archivos de wwwroot (el logo que muestra Swagger).
app.UseStaticFiles();

app.UseHttpsRedirection();
app.UseCors(PoliticaCors);
app.UseAuthorization();

app.MapControllers();

// Página de inicio: confirma que la API está viva y dónde está la documentación.
app.MapGet("/", () => Results.Ok(new
    {
        api = "BaityMu",
        mensaje = "¡Muuu! La granja de Baity está abierta. BaityCow te da la bienvenida.",
        documentacion = "/swagger"
    }))
    .WithTags("BaityMu")
    .WithSummary("Bienvenida de la API");

// Solo en desarrollo: endpoints para ver cómo responde el manejador global de errores.
// Sirven para probar en Swagger los códigos 400, 404, 409 y 500 sin tener datos.
if (app.Environment.IsDevelopment())
{
    var diagnostico = app.MapGroup("/api/diagnostico").WithTags("Diagnóstico (solo desarrollo)");

    diagnostico.MapGet("/errores/{codigo:int}", (int codigo) => codigo switch
        {
            400 => throw new ReglaDeNegocioException("La raza de la vaca es obligatoria."),
            404 => throw new RecursoNoEncontradoException("una vaca", 99),
            409 => throw new ConflictoException("Solo quedan 30 L de yogurt sin vencer y se pidieron 50 L."),
            500 => throw new InvalidOperationException("Error simulado para probar el 500."),
            _ => Results.Ok(new { mensaje = "Usa 400, 404, 409 o 500 para ver cada tipo de error." })
        })
        .WithSummary("Lanza una excepción de ejemplo para ver el ProblemDetails de cada código");
}

app.Run();

/// <summary>
/// Clase del programa. Se declara parcial y pública para que, si más adelante se hacen
/// pruebas de integración (WebApplicationFactory), puedan arrancar la API completa.
/// </summary>
public partial class Program;
