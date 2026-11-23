// =====================================================================================
// BaityMu.Client — Program.cs
// Front de BaityMu en Blazor WebAssembly: corre completo en el navegador (C# compilado
// a WebAssembly) y habla con la API por HTTP. Por eso la API tiene CORS habilitado.
// =====================================================================================

using System.Globalization;
using BaityMu.Client;
using BaityMu.Client.Servicios;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Números y fechas en español (14,5 L · 1 dic 2026).
var espanol = new CultureInfo("es-CO");
CultureInfo.DefaultThreadCurrentCulture = espanol;
CultureInfo.DefaultThreadCurrentUICulture = espanol;

// Dirección de la API: wwwroot/appsettings.json → "ApiBaseUrl".
var urlApi = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5251";
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(urlApi) });
builder.Services.AddScoped<BaityMuApi>();

// Singleton: en WebAssembly hay un solo usuario por pestaña, así que el día de trabajo
// y los avisos se comparten entre todas las pantallas.
builder.Services.AddSingleton<EstadoApp>();

await builder.Build().RunAsync();
