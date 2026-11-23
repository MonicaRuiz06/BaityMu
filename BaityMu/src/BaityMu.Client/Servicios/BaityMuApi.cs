using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BaityMu.Client.Modelos;

namespace BaityMu.Client.Servicios;

/// <summary>
/// Error que devolvió la API (400, 404, 409 o 500). Lleva el título y el detalle del
/// ProblemDetails para mostrárselos a Baity tal cual.
/// </summary>
public sealed class ApiException(HttpStatusCode codigo, string titulo, string detalle) : Exception(detalle)
{
    public HttpStatusCode Codigo { get; } = codigo;
    public string Titulo { get; } = titulo;
    public string Detalle { get; } = detalle;
}

/// <summary>
/// Cliente tipado de la API BaityMu: un método por endpoint. Las páginas nunca usan
/// HttpClient directamente; así, si cambia una ruta, se cambia solo aquí.
/// </summary>
public sealed class BaityMuApi(HttpClient http)
{
    /// <summary>JSON igual al de la API: camelCase y enums como texto ("Queso").</summary>
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private static string F(DateOnly fecha) => fecha.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    // ------------------------------------------------------------------ Granja

    public Task<Granja> ObtenerGranjaAsync() => GetAsync<Granja>("api/granja");

    public Task<Granja> ActualizarGranjaAsync(ActualizarGranja datos) => SendAsync<Granja>(HttpMethod.Put, "api/granja", datos);

    // ------------------------------------------------------------------ Vacas

    public Task<List<Vaca>> ListarVacasAsync(bool incluirRetiradas) =>
        GetAsync<List<Vaca>>($"api/vacas?incluirRetiradas={incluirRetiradas.ToString().ToLowerInvariant()}");

    public Task<VacaDetalle> ObtenerVacaAsync(int id) => GetAsync<VacaDetalle>($"api/vacas/{id}");

    public Task<Vaca> RegistrarVacaAsync(CrearVaca datos) => SendAsync<Vaca>(HttpMethod.Post, "api/vacas", datos);

    public Task RetirarVacaAsync(int id) => SendAsync(HttpMethod.Delete, $"api/vacas/{id}");

    // ------------------------------------------------------------------ Producción

    public Task<ProduccionDiaria> RegistrarProduccionAsync(RegistrarProduccion datos) =>
        SendAsync<ProduccionDiaria>(HttpMethod.Post, "api/produccion", datos);

    /// <summary>La producción de un día, o null si ese día no hubo (404).</summary>
    public async Task<ProduccionDiaria?> ObtenerProduccionAsync(DateOnly fecha)
    {
        try
        {
            return await GetAsync<ProduccionDiaria>($"api/produccion/{F(fecha)}");
        }
        catch (ApiException e) when (e.Codigo == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    // ------------------------------------------------------------------ Transformación e inventario

    public Task<Lote> TransformarAsync(TransformarLeche datos) =>
        SendAsync<Lote>(HttpMethod.Post, "api/transformaciones", datos);

    /// <param name="tipo">Solo queso o solo yogurt; null para ambos.</param>
    /// <param name="fecha">Fecha con la que se calcula el estado de cada lote.</param>
    /// <param name="producidosEl">Si viene, solo los lotes hechos con la leche de ese día.</param>
    public Task<List<Lote>> ListarLotesAsync(TipoProducto? tipo, DateOnly fecha, DateOnly? producidosEl = null)
    {
        var url = $"api/inventario?fecha={F(fecha)}";
        if (tipo is not null)
            url += $"&tipoProducto={tipo}";
        if (producidosEl is DateOnly dia)
            url += $"&desde={F(dia)}&hasta={F(dia)}";
        return GetAsync<List<Lote>>(url);
    }

    public Task<ResultadoVencimientos> ProcesarVencimientosAsync(DateOnly fecha) =>
        SendAsync<ResultadoVencimientos>(HttpMethod.Post, $"api/inventario/procesar-vencimientos?fecha={F(fecha)}");

    public Task<List<Lote>> ProximosAVencerAsync(int dias, DateOnly fecha) =>
        GetAsync<List<Lote>>($"api/inventario/proximos-a-vencer?dias={dias}&fecha={F(fecha)}");

    public Task<List<ResumenInventario>> ResumenInventarioAsync(DateOnly fecha) =>
        GetAsync<List<ResumenInventario>>($"api/inventario/resumen?fecha={F(fecha)}");

    // ------------------------------------------------------------------ Ventas

    public Task<Venta> RegistrarVentaAsync(NuevaVenta datos) => SendAsync<Venta>(HttpMethod.Post, "api/ventas", datos);

    public Task<List<Venta>> ListarVentasAsync(DateOnly desde, DateOnly hasta, TipoProducto? tipo = null)
    {
        var url = $"api/ventas?desde={F(desde)}&hasta={F(hasta)}";
        if (tipo is not null)
            url += $"&tipo={tipo}";
        return GetAsync<List<Venta>>(url);
    }

    // ------------------------------------------------------------------ Reportes

    public Task<ReporteVentas> ReporteVentasAsync(DateOnly desde, DateOnly hasta, TipoProducto? tipo = null)
    {
        var url = $"api/reportes/ventas?desde={F(desde)}&hasta={F(hasta)}";
        if (tipo is not null)
            url += $"&tipo={tipo}";
        return GetAsync<ReporteVentas>(url);
    }

    public Task<ReporteLecheDiaria> ReporteLecheDiariaAsync(DateOnly desde, DateOnly hasta) =>
        GetAsync<ReporteLecheDiaria>($"api/reportes/leche-diaria?desde={F(desde)}&hasta={F(hasta)}");

    public Task<ReporteProductoDiario> ReporteProductoDiarioAsync(DateOnly desde, DateOnly hasta) =>
        GetAsync<ReporteProductoDiario>($"api/reportes/producto-diario?desde={F(desde)}&hasta={F(hasta)}");

    public Task<List<ProductoTotal>> ReporteProductoTotalAsync(DateOnly fecha) =>
        GetAsync<List<ProductoTotal>>($"api/reportes/producto-total?fecha={F(fecha)}");

    public Task<ReporteProductoDanado> ReporteProductoDanadoAsync(DateOnly desde, DateOnly hasta, TipoProducto? tipo = null)
    {
        var url = $"api/reportes/producto-danado?desde={F(desde)}&hasta={F(hasta)}";
        if (tipo is not null)
            url += $"&tipo={tipo}";
        return GetAsync<ReporteProductoDanado>(url);
    }

    public Task<ReporteVacas> ReporteVacasAsync(DateOnly desde, DateOnly hasta) =>
        GetAsync<ReporteVacas>($"api/reportes/vacas?desde={F(desde)}&hasta={F(hasta)}");

    // ------------------------------------------------------------------ Plomería

    private async Task<T> GetAsync<T>(string url)
    {
        using var respuesta = await EnviarAsync(() => http.GetAsync(url));
        return await LeerAsync<T>(respuesta);
    }

    private async Task<T> SendAsync<T>(HttpMethod metodo, string url, object? cuerpo = null)
    {
        using var respuesta = await EnviarAsync(() => http.SendAsync(Crear(metodo, url, cuerpo)));
        return await LeerAsync<T>(respuesta);
    }

    private async Task SendAsync(HttpMethod metodo, string url, object? cuerpo = null)
    {
        using var respuesta = await EnviarAsync(() => http.SendAsync(Crear(metodo, url, cuerpo)));
        await AsegurarExitoAsync(respuesta);
    }

    private static HttpRequestMessage Crear(HttpMethod metodo, string url, object? cuerpo) =>
        new(metodo, url) { Content = cuerpo is null ? null : JsonContent.Create(cuerpo, options: Json) };

    /// <summary>Si la API no está corriendo, el navegador lanza HttpRequestException sin detalles.</summary>
    private static async Task<HttpResponseMessage> EnviarAsync(Func<Task<HttpResponseMessage>> enviar)
    {
        try
        {
            return await enviar();
        }
        catch (HttpRequestException)
        {
            throw new ApiException(0, "Sin conexión con la API",
                "No se pudo hablar con la API de BaityMu. ¿Está ejecutándose en http://localhost:5251?");
        }
    }

    private static async Task<T> LeerAsync<T>(HttpResponseMessage respuesta)
    {
        await AsegurarExitoAsync(respuesta);
        return (await respuesta.Content.ReadFromJsonAsync<T>(Json))!;
    }

    /// <summary>Convierte un error de la API (ProblemDetails) en ApiException.</summary>
    private static async Task AsegurarExitoAsync(HttpResponseMessage respuesta)
    {
        if (respuesta.IsSuccessStatusCode)
            return;

        var titulo = $"Error {(int)respuesta.StatusCode}";
        var detalle = "La API respondió con un error.";
        try
        {
            var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>(Json);
            if (problema.TryGetProperty("title", out var t)) titulo = t.GetString() ?? titulo;
            if (problema.TryGetProperty("detail", out var d)) detalle = d.GetString() ?? detalle;
            else if (problema.TryGetProperty("errors", out var errores))
                detalle = string.Join(" ", errores.EnumerateObject()
                    .SelectMany(e => e.Value.EnumerateArray().Select(m => m.GetString())));
        }
        catch (JsonException)
        {
            // La respuesta no era JSON: se deja el mensaje genérico.
        }

        throw new ApiException(respuesta.StatusCode, titulo, detalle);
    }
}
