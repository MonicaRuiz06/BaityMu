using AutoMapper;
using BaityMu.Services.Dtos;
using BaityMu.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BaityMu.Web.Controllers
{
    /// <summary>
    /// Configuración de la granja: consultar y actualizar l y los productos (Épica 1).
    /// Rol: BaityAdministrador.
    /// </summary>
    [ApiController]
    [Route("api/granja")]
    [Produces("application/json")]
    public sealed class GranjaController : ControllerBase
    {
        private readonly IGranjaService _granjaService;
        private readonly IMapper _mapper;

        /// <summary>Recibe el servicio y AutoMapper por inyección de dependencias.</summary>
        public GranjaController(IGranjaService granjaService, IMapper mapper)
        {
            _granjaService = granjaService;
            _mapper = mapper;
        }

        /// <summary>Consulta la configuración de la granja (HU-05).</summary>
        /// <remarks>
        /// Devuelve n (vacas activas), l (litros por vaca), la producción diaria esperada
        /// (**n × l**) y, para el queso y el yogurt, su unidad, rendimiento, vida útil y precio.
        /// </remarks>
        /// <param name="ct">Se cancela si el cliente cierra la conexión.</param>
        /// <response code="200">La configuración de la granja.</response>
        /// <response code="404">La granja no está configurada.</response>
        [HttpGet]
        [ProducesResponseType<GranjaResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
        public async Task<ActionResult<GranjaResponse>> Obtener(CancellationToken ct)
        {
            var granja = await _granjaService.ObtenerAsync(ct);
            return Ok(_mapper.Map<GranjaResponse>(granja));
        }

        /// <summary>Actualiza l y los parámetros de los productos (HU-06).</summary>
        /// <remarks>
        /// Lo que no se envía se conserva: se puede cambiar solo l, solo un producto, o todo.
        /// Si un valor es inválido (l, rendimiento o vida útil ≤ 0, o precio negativo),
        /// responde 400 y **no cambia nada**.
        ///
        /// Los lotes y ventas que ya existen conservan los valores con los que se crearon;
        /// los nuevos usan los valores nuevos.
        /// </remarks>
        /// <param name="request">Los valores nuevos.</param>
        /// <param name="ct">Se cancela si el cliente cierra la conexión.</param>
        /// <response code="200">La configuración nueva.</response>
        /// <response code="400">Algún valor no es válido. No se cambió nada.</response>
        /// <response code="404">La granja no está configurada.</response>
        [HttpPut]
        [ProducesResponseType<GranjaResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
        public async Task<ActionResult<GranjaResponse>> Actualizar(ActualizarGranjaRequest request, CancellationToken ct)
        {
            var granja = await _granjaService.ActualizarAsync(request, ct);
            return Ok(_mapper.Map<GranjaResponse>(granja));
        }
    }
}
