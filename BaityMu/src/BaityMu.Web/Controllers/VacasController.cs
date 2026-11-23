using AutoMapper;
using BaityMu.Services.Dtos;
using BaityMu.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BaityMu.Web.Controllers
{
    /// <summary>
    /// Hato de Baity: registro y consulta de vacas (Épica 1). Rol: BaityGanadero.
    /// </summary>
    /// <remarks>
    /// El controller no tiene lógica de negocio (RNF-02): recibe la petición, llama al
    /// servicio y convierte el resultado en DTO con AutoMapper. Los errores (400, 404, 409)
    /// los traduce el manejador global a partir de las excepciones del dominio.
    /// </remarks>
    [ApiController]
    [Route("api/vacas")]
    [Produces("application/json")]
    public sealed class VacasController : ControllerBase
    {
        private readonly IGranjaService _granjaService;
        private readonly IMapper _mapper;

        /// <summary>Recibe el servicio y AutoMapper por inyección de dependencias.</summary>
        public VacasController(IGranjaService granjaService, IMapper mapper)
        {
            _granjaService = granjaService;
            _mapper = mapper;
        }

        /// <summary>Lista las vacas de la granja con los litros que ha producido cada una (HU-02).</summary>
        /// <remarks>
        /// Por defecto solo trae las vacas activas. Con **incluirRetiradas=true** también
        /// trae las retiradas, marcadas con `activa: false`. Una vaca que nunca se ha
        /// ordeñado sale con 0 L.
        /// </remarks>
        /// <param name="incluirRetiradas">true para incluir las vacas retiradas.</param>
        /// <param name="ct">Se cancela si el cliente cierra la conexión.</param>
        /// <response code="200">El listado (puede estar vacío).</response>
        /// <response code="404">La granja no está configurada.</response>
        [HttpGet]
        [ProducesResponseType<IReadOnlyList<VacaResponse>>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
        public async Task<ActionResult<IReadOnlyList<VacaResponse>>> Listar(
            [FromQuery] bool incluirRetiradas = false,
            CancellationToken ct = default)
        {
            var vacas = await _granjaService.ListarVacasAsync(incluirRetiradas, ct);
            return Ok(_mapper.Map<IReadOnlyList<VacaResponse>>(vacas));
        }

        /// <summary>Registra una vaca en la granja (HU-01).</summary>
        /// <remarks>
        /// Si no se envía nombre, la primera vaca se llama **BaityCow**, la segunda
        /// **BaityCow 2**, etc. Si no se envía fecha de ingreso, se usa la de hoy.
        /// La vaca nace activa y con 0 L producidos, y n aumenta en 1.
        /// </remarks>
        /// <param name="request">Raza (obligatoria), nombre y fecha de ingreso opcionales.</param>
        /// <param name="ct">Se cancela si el cliente cierra la conexión.</param>
        /// <response code="201">La vaca se registró.</response>
        /// <response code="400">La raza viene vacía.</response>
        /// <response code="404">La granja no está configurada.</response>
        /// <response code="409">Ya existe una vaca con ese nombre.</response>
        [HttpPost]
        [ProducesResponseType<VacaResponse>(StatusCodes.Status201Created)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
        public async Task<ActionResult<VacaResponse>> Registrar(CrearVacaRequest request, CancellationToken ct)
        {
            var vaca = await _granjaService.AgregarVacaAsync(request, ct);
            var respuesta = _mapper.Map<VacaResponse>(vaca);

            // 201 Created con la ruta de la vaca nueva en el encabezado Location.
            // GET /api/vacas/{id} llega con la HU-02.
            return Created($"/api/vacas/{respuesta.Id}", respuesta);
        }
    }
}
