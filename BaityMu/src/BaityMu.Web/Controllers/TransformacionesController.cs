using AutoMapper;
using BaityMu.Domain.Enums;
using BaityMu.Services.Dtos;
using BaityMu.Services.Interfaces;
using BaityMu.Services.Modelos;
using Microsoft.AspNetCore.Mvc;

namespace BaityMu.Web.Controllers
{
    /// <summary>
    /// Transformación de la leche en queso o yogurt (Épica 3). Rol: BaityQuesero.
    /// </summary>
    [ApiController]
    [Route("api/transformaciones")]
    [Produces("application/json")]
    public sealed class TransformacionesController : ControllerBase
    {
        private readonly IProduccionService _produccionService;
        private readonly IMapper _mapper;

        /// <summary>Recibe el servicio y AutoMapper por inyección de dependencias.</summary>
        public TransformacionesController(IProduccionService produccionService, IMapper mapper)
        {
            _produccionService = produccionService;
            _mapper = mapper;
        }

        /// <summary>Transforma leche de un día en un lote de queso o yogurt (HU-09).</summary>
        /// <remarks>
        /// - Cantidad del lote = litros ÷ rendimiento (RN-03). Ejemplo: 100 L → **10 kg** de queso.
        /// - Vencimiento = fecha de la leche + vida útil (RN-04). Ejemplo: yogurt del
        ///   2026-12-01 → vence el **2026-12-16**.
        /// - Se puede transformar varias veces el mismo día, pero la suma nunca supera lo
        ///   producido (RN-02). La leche que sobra es no aprovechada.
        /// </remarks>
        /// <param name="request">Fecha, producto y litros.</param>
        /// <param name="ct">Se cancela si el cliente cierra la conexión.</param>
        /// <response code="201">El lote se creó.</response>
        /// <response code="400">Litros ≤ 0 o producto no válido.</response>
        /// <response code="409">No hay producción ese día o no alcanza la leche.</response>
        [HttpPost]
        [ProducesResponseType<LoteResponse>(StatusCodes.Status201Created)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
        public async Task<ActionResult<LoteResponse>> Transformar(TransformarLecheRequest request, CancellationToken ct)
        {
            var lote = await _produccionService.TransformarAsync(request, ct);

            // Un lote recién creado siempre está Disponible: tiene todo su producto y vence
            // en el futuro. Location apunta a GET /api/inventario/lotes/{id} (HU-10).
            var respuesta = _mapper.Map<LoteResponse>(new LoteConEstado(lote, EstadoLote.Disponible));
            return CreatedAtRoute(InventarioController.RutaLote, new { id = respuesta.Id }, respuesta);
        }
    }
}
