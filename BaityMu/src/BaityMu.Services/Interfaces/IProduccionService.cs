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
    }
}
