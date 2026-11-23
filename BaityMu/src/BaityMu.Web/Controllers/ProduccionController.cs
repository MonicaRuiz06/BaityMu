using AutoMapper;
using BaityMu.Services.Dtos;
using BaityMu.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BaityMu.Web.Controllers
{
    /// <summary>
    /// Producción de leche (Épica 2). Rol: BaityGanadero.
    /// </summary>
    [ApiController]
    [Route("api/produccion")]
    [Produces("application/json")]
    public sealed class ProduccionController : ControllerBase
    {
        private readonly IProduccionService _produccionService;
        private readonly IMapper _mapper;

        /// <summary>
        /// Nombre de la ruta GET por Id, para que CreatedAtRoute arme el encabezado Location.
        /// Los nombres de ruta son globales en toda la API: no se pueden repetir entre controllers.
        /// </summary>
        private const string RutaProduccion = "ObtenerProduccion";

        /// <summary>Recibe el servicio y AutoMapper por inyección de dependencias.</summary>
        public ProduccionController(IProduccionService produccionService, IMapper mapper)
        {
            _produccionService = produccionService;
            _mapper = mapper;
        }

        /// <summary>Registra la producción de leche de un día (HU-07).</summary>
        /// <remarks>
        /// - **Sin lista de vacas:** cada vaca activa da l litros y el total es **n × l**
        ///   (150 L en el escenario de referencia).
        /// - **Con lista de vacas:** solo se registran esas vacas con esos litros.
        /// - **Sin fecha:** se usa la de hoy.
        ///
        /// Cada vaca se registra una sola vez por día (RN-01): si alguna ya tenía producción
        /// en esa fecha, responde 409 y no se guarda nada.
        /// </remarks>
        /// <param name="request">Fecha y litros por vaca, ambos opcionales.</param>
        /// <param name="ct">Se cancela si el cliente cierra la conexión.</param>
        /// <response code="201">La producción se registró.</response>
        /// <response code="400">Litros negativos o una vaca repetida en la petición.</response>
        /// <response code="404">Una vaca no existe o está retirada.</response>
        /// <response code="409">Alguna vaca ya tenía producción ese día.</response>
        [HttpPost]
        [ProducesResponseType<ProduccionDiariaResponse>(StatusCodes.Status201Created)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
        public async Task<ActionResult<ProduccionDiariaResponse>> Registrar(
            RegistrarProduccionRequest request,
            CancellationToken ct)
        {
            var produccion = await _produccionService.RegistrarAsync(request, ct);
            var respuesta = _mapper.Map<ProduccionDiariaResponse>(produccion);

            // 201 Created: el encabezado Location apunta a GET /api/produccion/{fecha}.
            return CreatedAtRoute(RutaProduccion, new { fecha = respuesta.Fecha.ToString("yyyy-MM-dd") }, respuesta);
        }

        /// <summary>Consulta la producción de un día (HU-08).</summary>
        /// <remarks>
        /// Muestra los litros de cada vaca, el total del día, los litros ya transformados en
        /// queso o yogurt y los que todavía están **disponibles** para transformar.
        /// </remarks>
        /// <param name="fecha">Día a consultar, en formato yyyy-MM-dd. Ejemplo: 2026-12-01.</param>
        /// <param name="ct">Se cancela si el cliente cierra la conexión.</param>
        /// <response code="200">La producción del día.</response>
        /// <response code="404">Ese día no hubo producción.</response>
        [HttpGet("{fecha}", Name = RutaProduccion)]
        [ProducesResponseType<ProduccionDiariaResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
        public async Task<ActionResult<ProduccionDiariaResponse>> Obtener(DateOnly fecha, CancellationToken ct)
        {
            var produccion = await _produccionService.ObtenerAsync(fecha, ct);
            return Ok(_mapper.Map<ProduccionDiariaResponse>(produccion));
        }
    }
}
