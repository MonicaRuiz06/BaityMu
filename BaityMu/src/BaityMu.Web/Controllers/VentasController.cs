using AutoMapper;
using BaityMu.Domain.Enums;
using BaityMu.Services.Dtos;
using BaityMu.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BaityMu.Web.Controllers
{
    /// <summary>Ventas de queso y yogurt: registrar y consultar (Épica 5). Rol: BaityVendedor.</summary>
    [ApiController]
    [Route("api/ventas")]
    [Produces("application/json")]
    public sealed class VentasController : ControllerBase
    {
        /// <summary>Nombre de la ruta GET de una venta, para el encabezado Location.</summary>
        private const string RutaVenta = "ObtenerVenta";

        private readonly IVentaService _ventaService;
        private readonly IMapper _mapper;

        /// <summary>Recibe el servicio y AutoMapper por inyección de dependencias.</summary>
        public VentasController(IVentaService ventaService, IMapper mapper)
        {
            _ventaService = ventaService;
            _mapper = mapper;
        }

        /// <summary>Registra una venta descontando el inventario en orden FIFO (HU-13).</summary>
        /// <remarks>
        /// - Se vende primero del lote **más antiguo** que no esté vencido (RN-06).
        /// - Si un lote no alcanza, se sigue con el siguiente y la venta tiene varios detalles.
        ///   Escenario día 3: **12 kg** de queso = 10 kg de Q1 + 2 kg de Q2.
        /// - Un lote vencido nunca se vende, aunque sea el más antiguo.
        /// - Si el producto no vencido no alcanza, responde **409** y no se vende nada (RN-07).
        ///   Escenario día 17: 5 L de yogurt → 409.
        /// - El precio se copia del producto en ese momento.
        /// </remarks>
        /// <param name="request">Fecha opcional, producto y cantidad.</param>
        /// <param name="ct">Se cancela si el cliente cierra la conexión.</param>
        /// <response code="201">La venta se registró.</response>
        /// <response code="400">Cantidad ≤ 0 o producto no válido.</response>
        /// <response code="409">No hay suficiente producto sin vencer.</response>
        [HttpPost]
        [ProducesResponseType<VentaResponse>(StatusCodes.Status201Created)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
        public async Task<ActionResult<VentaResponse>> Registrar(VentaRequest request, CancellationToken ct)
        {
            var venta = await _ventaService.RegistrarVentaAsync(request, ct);
            var respuesta = _mapper.Map<VentaResponse>(venta);

            // 201 Created: Location apunta a GET /api/ventas/{id} (HU-14).
            return CreatedAtRoute(RutaVenta, new { id = respuesta.Id }, respuesta);
        }

        /// <summary>Consulta una venta con los lotes de donde salió (HU-14).</summary>
        /// <param name="id">Id de la venta.</param>
        /// <param name="ct">Se cancela si el cliente cierra la conexión.</param>
        /// <response code="200">La venta y sus detalles.</response>
        /// <response code="404">No existe una venta con ese Id.</response>
        [HttpGet("{id:int}", Name = RutaVenta)]
        [ProducesResponseType<VentaResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
        public async Task<ActionResult<VentaResponse>> Obtener(int id, CancellationToken ct)
        {
            var venta = await _ventaService.ObtenerAsync(id, ct);
            return Ok(_mapper.Map<VentaResponse>(venta));
        }

        /// <summary>Lista las ventas con sus detalles (HU-14).</summary>
        /// <remarks>Ordenadas de la más antigua a la más reciente. Todos los filtros son opcionales.</remarks>
        /// <param name="desde">Fecha mínima (yyyy-MM-dd).</param>
        /// <param name="hasta">Fecha máxima (yyyy-MM-dd).</param>
        /// <param name="tipo">Solo "Queso" o solo "Yogurt".</param>
        /// <param name="ct">Se cancela si el cliente cierra la conexión.</param>
        /// <response code="200">Las ventas (puede estar vacío).</response>
        /// <response code="400">'desde' es posterior a 'hasta', o un filtro no es válido.</response>
        [HttpGet]
        [ProducesResponseType<IReadOnlyList<VentaResponse>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
        public async Task<ActionResult<IReadOnlyList<VentaResponse>>> Listar(
            [FromQuery] DateOnly? desde,
            [FromQuery] DateOnly? hasta,
            [FromQuery] TipoProducto? tipo,
            CancellationToken ct)
        {
            var ventas = await _ventaService.ListarAsync(desde, hasta, tipo, ct);
            return Ok(_mapper.Map<IReadOnlyList<VentaResponse>>(ventas));
        }
    }
}
