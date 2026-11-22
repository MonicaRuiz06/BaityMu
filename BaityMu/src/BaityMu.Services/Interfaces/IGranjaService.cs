using BaityMu.Domain.Entities;
using BaityMu.Services.Dtos;

namespace BaityMu.Services.Interfaces
{
    /// <summary>
    /// Casos de uso de la granja y su hato (Épica 1).
    /// </summary>
    /// <remarks>
    /// El controller depende de esta interfaz y no de la clase GranjaService. Así se puede
    /// cambiar o simular el servicio sin tocar el controller.
    /// </remarks>
    public interface IGranjaService
    {
        /// <summary>Registra una vaca nueva en la granja (HU-01).</summary>
        /// <param name="request">Raza, nombre opcional y fecha de ingreso opcional.</param>
        /// <param name="ct">Permite cancelar la operación.</param>
        /// <returns>La vaca creada, ya guardada y con su Id.</returns>
        /// <exception cref="BaityMu.Domain.Exceptions.ReglaDeNegocioException">Raza vacía (400).</exception>
        /// <exception cref="BaityMu.Domain.Exceptions.ConflictoException">Nombre repetido (409).</exception>
        /// <exception cref="BaityMu.Domain.Exceptions.RecursoNoEncontradoException">La granja no existe (404).</exception>
        Task<Vaca> AgregarVacaAsync(CrearVacaRequest request, CancellationToken ct = default);
    }
}
