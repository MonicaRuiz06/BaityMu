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
    }
}
