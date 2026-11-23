using BaityMu.Domain.Enums;
using BaityMu.Services.Dtos;
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
            CancellationToken ct) =>
            Ok(await _reportes.VentasAsync(desde, hasta, tipo, ct));

        /// <summary>Leche producida, transformada y no aprovechada de cada día (HU-16).</summary>
        /// <remarks>
        /// Solo aparecen los días con producción. Escenario: el día 1 y el día 2 muestran
        /// **150 L** producidos, 150 L transformados y 0 L no aprovechados.
        /// </remarks>
        /// <param name="desde">Fecha mínima (yyyy-MM-dd). Opcional.</param>
        /// <param name="hasta">Fecha máxima (yyyy-MM-dd). Opcional.</param>
        /// <param name="ct">Se cancela si el cliente cierra la conexión.</param>
        /// <response code="200">El reporte (con la lista vacía si no hubo producción).</response>
        /// <response code="400">'desde' es posterior a 'hasta'.</response>
        [HttpGet("leche-diaria")]
        [ProducesResponseType<ReporteLecheDiariaResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
        public async Task<ActionResult<ReporteLecheDiariaResponse>> LecheDiaria(
            [FromQuery] DateOnly? desde,
            [FromQuery] DateOnly? hasta,
            CancellationToken ct) =>
            Ok(await _reportes.LecheDiariaAsync(desde, hasta, ct));

        /// <summary>Queso y yogurt elaborados cada día, con su unidad y los litros usados (HU-17).</summary>
        /// <remarks>
        /// Escenario: el día 1 muestra **10 kg** de queso (100 L) y **50 L** de yogurt (50 L);
        /// el día 2, **15 kg** de queso (150 L). También trae el total de cada producto.
        /// </remarks>
        /// <param name="desde">Fecha mínima (yyyy-MM-dd). Opcional.</param>
        /// <param name="hasta">Fecha máxima (yyyy-MM-dd). Opcional.</param>
        /// <param name="ct">Se cancela si el cliente cierra la conexión.</param>
        /// <response code="200">El reporte (con listas vacías si no se elaboró nada).</response>
        /// <response code="400">'desde' es posterior a 'hasta'.</response>
        [HttpGet("producto-diario")]
        [ProducesResponseType<ReporteProductoDiarioResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
        public async Task<ActionResult<ReporteProductoDiarioResponse>> ProductoDiario(
            [FromQuery] DateOnly? desde,
            [FromQuery] DateOnly? hasta,
            CancellationToken ct) =>
            Ok(await _reportes.ProductoDiarioAsync(desde, hasta, ct));

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
        /// <response code="200">Un renglón por producto.</response>
        [HttpGet("producto-total")]
        [ProducesResponseType<IReadOnlyList<ProductoTotalItem>>(StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<ProductoTotalItem>>> ProductoTotal(
            [FromQuery] DateOnly? fecha,
            CancellationToken ct) =>
            Ok(await _reportes.ProductoTotalAsync(fecha, ct));
    }
}
