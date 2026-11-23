using AutoMapper;
using BaityMu.Domain.Enums;
using BaityMu.Services.Dtos;
using BaityMu.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BaityMu.Web.Controllers
{
    /// <summary>
    /// Inventario de queso y yogurt (Épica 4). Rol: BaityBodeguero.
    /// </summary>
    /// <remarks>
    /// Todas las consultas aceptan <c>fecha</c> para ver el inventario "como estaba" ese
    /// día: un lote que vence el 16 aparece Disponible si se consulta el 16 y Vencido si se
    /// consulta el 17. Sin fecha, se usa la de hoy.
    /// </remarks>
    [ApiController]
    [Route("api/inventario")]
    [Produces("application/json")]
    public sealed class InventarioController : ControllerBase
    {
        /// <summary>Nombre de la ruta GET de un lote, para el encabezado Location.</summary>
        public const string RutaLote = "ObtenerLote";

        private readonly IInventarioService _inventarioService;
        private readonly IMapper _mapper;

        /// <summary>Recibe el servicio y AutoMapper por inyección de dependencias.</summary>
        public InventarioController(IInventarioService inventarioService, IMapper mapper)
        {
            _inventarioService = inventarioService;
            _mapper = mapper;
        }

        /// <summary>Lista los lotes del inventario con su estado (HU-10).</summary>
        /// <remarks>Ordenados como los usa la venta: por producto y del más antiguo al más nuevo (FIFO).</remarks>
        /// <param name="tipoProducto">Solo "Queso" o solo "Yogurt".</param>
        /// <param name="desde">Fecha de producción mínima (yyyy-MM-dd).</param>
        /// <param name="hasta">Fecha de producción máxima (yyyy-MM-dd).</param>
        /// <param name="fecha">Fecha para calcular el estado. Por defecto, hoy.</param>
        /// <param name="ct">Se cancela si el cliente cierra la conexión.</param>
        /// <response code="200">Los lotes (puede estar vacío).</response>
        /// <response code="400">'desde' es posterior a 'hasta', o un filtro no es válido.</response>
        [HttpGet]
        [ProducesResponseType<IReadOnlyList<LoteResponse>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
        public async Task<ActionResult<IReadOnlyList<LoteResponse>>> Listar(
            [FromQuery] TipoProducto? tipoProducto,
            [FromQuery] DateOnly? desde,
            [FromQuery] DateOnly? hasta,
            [FromQuery] DateOnly? fecha,
            CancellationToken ct)
        {
            var lotes = await _inventarioService.ListarLotesAsync(tipoProducto, desde, hasta, fecha, ct);
            return Ok(_mapper.Map<IReadOnlyList<LoteResponse>>(lotes));
        }

        /// <summary>Consulta un lote por Id (HU-10).</summary>
        /// <param name="id">Id del lote.</param>
        /// <param name="fecha">Fecha para calcular el estado. Por defecto, hoy.</param>
        /// <param name="ct">Se cancela si el cliente cierra la conexión.</param>
        /// <response code="200">El lote.</response>
        /// <response code="404">No existe un lote con ese Id.</response>
        [HttpGet("lotes/{id:int}", Name = RutaLote)]
        [ProducesResponseType<LoteResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
        public async Task<ActionResult<LoteResponse>> ObtenerLote(int id, [FromQuery] DateOnly? fecha, CancellationToken ct)
        {
            var lote = await _inventarioService.ObtenerLoteAsync(id, fecha, ct);
            return Ok(_mapper.Map<LoteResponse>(lote));
        }

        /// <summary>Resumen del inventario por producto (HU-10).</summary>
        /// <remarks>
        /// Para queso y yogurt: cuánto se puede vender, cuánto está vencido sin procesar y
        /// cuánto ya está dañado.
        /// </remarks>
        /// <param name="fecha">Fecha del resumen. Por defecto, hoy.</param>
        /// <param name="ct">Se cancela si el cliente cierra la conexión.</param>
        /// <response code="200">El resumen de los dos productos.</response>
        [HttpGet("resumen")]
        [ProducesResponseType<IReadOnlyList<ResumenInventarioResponse>>(StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<ResumenInventarioResponse>>> Resumen(
            [FromQuery] DateOnly? fecha,
            CancellationToken ct)
        {
            var resumen = await _inventarioService.ObtenerResumenAsync(fecha, ct);
            return Ok(_mapper.Map<IReadOnlyList<ResumenInventarioResponse>>(resumen));
        }
    }
}
