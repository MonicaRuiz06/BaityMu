using BaityMu.Domain.Entities;
using BaityMu.Services.Dtos;
using BaityMu.Services.Modelos;

namespace BaityMu.Services.Interfaces
{
    /// <summary>
    /// Casos de uso de la producción de leche (Épica 2). Rol: BaityGanadero.
    /// </summary>
    public interface IProduccionService
    {
        /// <summary>Registra la producción de un día (HU-07, RN-01).</summary>
        /// <param name="request">Fecha opcional y, si se quiere, los litros de algunas vacas.</param>
        /// <param name="ct">Permite cancelar la operación.</param>
        /// <returns>Lo que se registró: cada vaca y el total del día.</returns>
        /// <exception cref="BaityMu.Domain.Exceptions.ReglaDeNegocioException">Litros negativos o vaca repetida en la petición (400).</exception>
        /// <exception cref="BaityMu.Domain.Exceptions.RecursoNoEncontradoException">Una vaca no existe o está retirada (404).</exception>
        /// <exception cref="BaityMu.Domain.Exceptions.ProduccionDuplicadaException">Alguna vaca ya tenía producción ese día (409).</exception>
        Task<ProduccionDiaria> RegistrarAsync(RegistrarProduccionRequest request, CancellationToken ct = default);

        /// <summary>
        /// Consulta la producción de un día: lo que dio cada vaca, el total, lo ya
        /// transformado y lo disponible (HU-08).
        /// </summary>
        /// <param name="fecha">Día a consultar.</param>
        /// <param name="ct">Permite cancelar la operación.</param>
        /// <exception cref="BaityMu.Domain.Exceptions.RecursoNoEncontradoException">Ese día no hubo producción (404).</exception>
        Task<ProduccionDiaria> ObtenerAsync(DateOnly fecha, CancellationToken ct = default);

        /// <summary>
        /// Transforma leche de un día en un lote de queso o yogurt (HU-09, RN-02 a RN-04).
        /// Rol: BaityQuesero.
        /// </summary>
        /// <param name="request">Fecha, producto y litros.</param>
        /// <param name="ct">Permite cancelar la operación.</param>
        /// <returns>El lote creado y guardado.</returns>
        /// <exception cref="BaityMu.Domain.Exceptions.ReglaDeNegocioException">Litros ≤ 0 o producto no configurado (400).</exception>
        /// <exception cref="BaityMu.Domain.Exceptions.LecheInsuficienteException">Sin producción ese día o leche insuficiente (409).</exception>
        Task<LoteProducto> TransformarAsync(TransformarLecheRequest request, CancellationToken ct = default);
    }
}
