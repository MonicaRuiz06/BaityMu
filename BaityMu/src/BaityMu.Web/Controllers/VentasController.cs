using AutoMapper;
using BaityMu.Services.Dtos;
using BaityMu.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BaityMu.Web.Controllers
{
    /// <summary>Ventas de queso y yogurt (Épica 5). Rol: BaityVendedor.</summary>
    [ApiController]
    [Route("api/ventas")]
    [Produces("application/json")]
    public sealed class VentasController : ControllerBase
    {
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

            // 201 sin encabezado Location: GET /api/ventas/{id} llega con la HU-14.
            return StatusCode(StatusCodes.Status201Created, _mapper.Map<VentaResponse>(venta));
        }
    }
}
