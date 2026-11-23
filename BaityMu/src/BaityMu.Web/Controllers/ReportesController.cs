using BaityMu.Domain.Enums;
using BaityMu.Domain.Exceptions;
using BaityMu.Services.Dtos;
using BaityMu.Services.Exportacion;
using BaityMu.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BaityMu.Web.Controllers
{
    /// <summary>Reportes de la granja (Épica 6). Rol: BaityGerente.</summary>
    /// <remarks>
    /// Todos son GET de solo lectura. Si 'desde' es posterior a 'hasta' responden 400; si no
    /// hay datos responden 200 con listas vacías y totales en 0.
    ///
    /// No usa AutoMapper: ReporteService ya devuelve los DTOs armados.
    ///
    /// Todos aceptan <c>?formato=csv</c> (HU-21): en vez de JSON devuelven un archivo .csv
    /// con la lista principal del reporte (las ventas, los días, los lotes o las vacas),
    /// listo para abrir en Excel. Cualquier otro formato responde 400.
    /// </remarks>
    [ApiController]
    [Route("api/reportes")]
    [Produces("application/json")]
    public sealed class ReportesController : ControllerBase
    {
        private readonly IReporteService _reportes;

        /// <summary>Recibe el servicio por inyección de dependencias.</summary>
        public ReportesController(IReporteService reportes)
        {
            _reportes = reportes;
        }

        /// <summary>Reporte de ventas de un período (HU-15).</summary>
        /// <remarks>
        /// Muestra cada venta (fecha, producto, cantidad, precio y total), el total por producto
        /// y el total general. Escenario día 3: 12 kg de queso ($336.000) + 20 L de yogurt
        /// ($120.000) = **$456.000**.
        /// </remarks>
        /// <param name="desde">Fecha mínima (yyyy-MM-dd). Opcional.</param>
        /// <param name="hasta">Fecha máxima (yyyy-MM-dd). Opcional.</param>
        /// <param name="tipo">Solo "Queso" o solo "Yogurt". Opcional.</param>
        /// <param name="formato">"csv" para descargar un archivo .csv (HU-21). Opcional: por defecto, JSON.</param>
        /// <param name="ct">Se cancela si el cliente cierra la conexión.</param>
        /// <response code="200">El reporte (con listas vacías si no hubo ventas).</response>
        /// <response code="400">'desde' es posterior a 'hasta', o un filtro no es válido.</response>
        [HttpGet("ventas")]
        [ProducesResponseType<ReporteVentasResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
        public async Task<ActionResult<ReporteVentasResponse>> Ventas(
            [FromQuery] DateOnly? desde,
            [FromQuery] DateOnly? hasta,
            [FromQuery] TipoProducto? tipo,
            [FromQuery] string? formato,
            CancellationToken ct)
        {
            ValidarFormato(formato);
            var reporte = await _reportes.VentasAsync(desde, hasta, tipo, ct);
            return Responder(reporte, reporte.Ventas, "reporte-ventas", formato);
        }

        /// <summary>Leche producida, transformada y no aprovechada de cada día (HU-16).</summary>
        /// <remarks>
        /// Solo aparecen los días con producción. Escenario: el día 1 y el día 2 muestran
        /// **150 L** producidos, 150 L transformados y 0 L no aprovechados.
        /// </remarks>
        /// <param name="desde">Fecha mínima (yyyy-MM-dd). Opcional.</param>
        /// <param name="hasta">Fecha máxima (yyyy-MM-dd). Opcional.</param>
        /// <param name="ct">Se cancela si el cliente cierra la conexión.</param>
        /// <param name="formato">"csv" para descargar un archivo .csv (HU-21). Opcional: por defecto, JSON.</param>
        /// <response code="200">El reporte (con la lista vacía si no hubo producción).</response>
        /// <response code="400">'desde' es posterior a 'hasta'.</response>
        [HttpGet("leche-diaria")]
        [ProducesResponseType<ReporteLecheDiariaResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
        public async Task<ActionResult<ReporteLecheDiariaResponse>> LecheDiaria(
            [FromQuery] DateOnly? desde,
            [FromQuery] DateOnly? hasta,
            [FromQuery] string? formato,
            CancellationToken ct)
        {
            ValidarFormato(formato);
            var reporte = await _reportes.LecheDiariaAsync(desde, hasta, ct);
            return Responder(reporte, reporte.Dias, "reporte-leche-diaria", formato);
        }

        /// <summary>Queso y yogurt elaborados cada día, con su unidad y los litros usados (HU-17).</summary>
        /// <remarks>
        /// Escenario: el día 1 muestra **10 kg** de queso (100 L) y **50 L** de yogurt (50 L);
        /// el día 2, **15 kg** de queso (150 L). También trae el total de cada producto.
        /// </remarks>
        /// <param name="desde">Fecha mínima (yyyy-MM-dd). Opcional.</param>
        /// <param name="hasta">Fecha máxima (yyyy-MM-dd). Opcional.</param>
        /// <param name="ct">Se cancela si el cliente cierra la conexión.</param>
        /// <param name="formato">"csv" para descargar un archivo .csv (HU-21). Opcional: por defecto, JSON.</param>
        /// <response code="200">El reporte (con listas vacías si no se elaboró nada).</response>
        /// <response code="400">'desde' es posterior a 'hasta'.</response>
        [HttpGet("producto-diario")]
        [ProducesResponseType<ReporteProductoDiarioResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
        public async Task<ActionResult<ReporteProductoDiarioResponse>> ProductoDiario(
            [FromQuery] DateOnly? desde,
            [FromQuery] DateOnly? hasta,
            [FromQuery] string? formato,
            CancellationToken ct)
        {
            ValidarFormato(formato);
            var reporte = await _reportes.ProductoDiarioAsync(desde, hasta, ct);
            return Responder(reporte, reporte.Dias, "reporte-producto-diario", formato);
        }

        /// <summary>Balance de cada producto: producido, vendido, disponible y dañado (HU-18).</summary>
        /// <remarks>
        /// Escenario completo (día 17, después de procesar vencimientos):
        ///
        /// | Producto | Producido | Vendido | Disponible | Dañado |
        /// |---|---|---|---|---|
        /// | Queso (kg) | 25 | 12 | 13 | 0 |
        /// | Yogurt (L) | 50 | 20 | 0 | 30 |
        ///
        /// Si hay lotes vencidos sin procesar, su cantidad sale en **vencidoSinProcesar**
        /// (no se puede vender, pero todavía no está marcada como dañada).
        /// </remarks>
        /// <param name="fecha">Fecha de consulta (yyyy-MM-dd). Opcional: si no llega, se usa la de hoy.</param>
        /// <param name="ct">Se cancela si el cliente cierra la conexión.</param>
        /// <param name="formato">"csv" para descargar un archivo .csv (HU-21). Opcional: por defecto, JSON.</param>
        /// <response code="200">Un renglón por producto.</response>
        /// <response code="400">El formato no es válido.</response>
        [HttpGet("producto-total")]
        [ProducesResponseType<IReadOnlyList<ProductoTotalItem>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
        public async Task<ActionResult<IReadOnlyList<ProductoTotalItem>>> ProductoTotal(
            [FromQuery] DateOnly? fecha,
            [FromQuery] string? formato,
            CancellationToken ct)
        {
            ValidarFormato(formato);
            var reporte = await _reportes.ProductoTotalAsync(fecha, ct);
            return Responder(reporte, reporte, "reporte-producto-total", formato);
        }

        /// <summary>Producto que se perdió por vencimiento, lote por lote (HU-19).</summary>
        /// <remarks>
        /// El rango se aplica a la **fecha de vencimiento**. Solo salen los lotes a los que ya se
        /// les procesó el vencimiento (POST /api/inventario/procesar-vencimientos).
        /// Escenario: después de procesar el día 17 aparece Y1 con **30 L** de yogurt (venció el 16).
        /// </remarks>
        /// <param name="desde">Fecha de vencimiento mínima (yyyy-MM-dd). Opcional.</param>
        /// <param name="hasta">Fecha de vencimiento máxima (yyyy-MM-dd). Opcional.</param>
        /// <param name="tipo">Solo "Queso" o solo "Yogurt". Opcional.</param>
        /// <param name="ct">Se cancela si el cliente cierra la conexión.</param>
        /// <param name="formato">"csv" para descargar un archivo .csv (HU-21). Opcional: por defecto, JSON.</param>
        /// <response code="200">El reporte (con listas vacías si no hubo daño).</response>
        /// <response code="400">'desde' es posterior a 'hasta', o un filtro no es válido.</response>
        [HttpGet("producto-danado")]
        [ProducesResponseType<ReporteProductoDanadoResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
        public async Task<ActionResult<ReporteProductoDanadoResponse>> ProductoDanado(
            [FromQuery] DateOnly? desde,
            [FromQuery] DateOnly? hasta,
            [FromQuery] TipoProducto? tipo,
            [FromQuery] string? formato,
            CancellationToken ct)
        {
            ValidarFormato(formato);
            var reporte = await _reportes.ProductoDanadoAsync(desde, hasta, tipo, ct);
            return Responder(reporte, reporte.Lotes, "reporte-producto-danado", formato);
        }

        /// <summary>Producción de cada vaca: promedio, la mejor, la peor y las que están por debajo (HU-20).</summary>
        /// <remarks>
        /// - **promedioDiarioHato**: litros de todo el hato en un día con ordeño.
        /// - **promedioPorVaca**: litros de una vaca en un día; contra este se compara cada vaca.
        /// - **porDebajoDelPromedio**: las vacas con menos que promedioPorVaca y su **faltante**.
        ///
        /// Si todas producen lo mismo, ninguna queda por debajo. Sin producción en el rango
        /// responde 200 con vacasEvaluadas = 0.
        /// </remarks>
        /// <param name="desde">Fecha mínima (yyyy-MM-dd). Opcional.</param>
        /// <param name="hasta">Fecha máxima (yyyy-MM-dd). Opcional.</param>
        /// <param name="ct">Se cancela si el cliente cierra la conexión.</param>
        /// <param name="formato">"csv" para descargar un archivo .csv (HU-21). Opcional: por defecto, JSON.</param>
        /// <response code="200">El reporte (sin vacas evaluadas si no hubo producción).</response>
        /// <response code="400">'desde' es posterior a 'hasta'.</response>
        [HttpGet("vacas")]
        [ProducesResponseType<ReporteVacasResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
        public async Task<ActionResult<ReporteVacasResponse>> Vacas(
            [FromQuery] DateOnly? desde,
            [FromQuery] DateOnly? hasta,
            [FromQuery] string? formato,
            CancellationToken ct)
        {
            ValidarFormato(formato);
            var reporte = await _reportes.VacasAsync(desde, hasta, ct);
            return Responder(reporte, reporte.Vacas, "reporte-vacas", formato);
        }

        // ------------------------------------------------------------------ HU-21 Formato CSV

        /// <summary>true si se pidió el reporte en CSV.</summary>
        private static bool EsCsv(string? formato) =>
            string.Equals(formato, "csv", StringComparison.OrdinalIgnoreCase);

        /// <summary>Solo se aceptan "json" (o nada) y "csv".</summary>
        /// <remarks>Se valida antes de consultar, para no hacer el trabajo si la petición es inválida.</remarks>
        /// <exception cref="ReglaDeNegocioException">Otro formato (400).</exception>
        private static void ValidarFormato(string? formato)
        {
            if (formato is not null && !EsCsv(formato)
                && !string.Equals(formato, "json", StringComparison.OrdinalIgnoreCase))
                throw new ReglaDeNegocioException($"El formato '{formato}' no es válido. Usa 'json' o 'csv'.");
        }

        /// <summary>
        /// Devuelve el reporte como JSON o, si se pidió CSV, su lista principal como archivo.
        /// </summary>
        /// <typeparam name="TReporte">Tipo del reporte completo (lo que va en JSON).</typeparam>
        /// <typeparam name="TFila">Tipo de cada fila del CSV.</typeparam>
        /// <param name="reporte">El reporte que armó ReporteService.</param>
        /// <param name="filas">La lista del reporte que se exporta en el CSV.</param>
        /// <param name="nombre">Nombre del archivo, sin extensión.</param>
        /// <param name="formato">El formato pedido.</param>
        /// <remarks>
        /// File(...) arma la respuesta con el encabezado Content-Disposition: attachment, así
        /// el navegador lo descarga como "reporte-ventas.csv" en vez de mostrarlo.
        /// </remarks>
        private ActionResult<TReporte> Responder<TReporte, TFila>(
            TReporte reporte,
            IEnumerable<TFila> filas,
            string nombre,
            string? formato) =>
            EsCsv(formato)
                ? File(CsvExporter.ExportarBytes(filas), CsvExporter.TipoContenido, $"{nombre}.csv")
                : Ok(reporte);
    }
}
