using BaityMu.Domain.Entities;
using BaityMu.Services.Dtos;
using BaityMu.Services.Modelos;

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

        /// <summary>Lista las vacas de la granja con los litros que ha dado cada una (HU-02).</summary>
        /// <param name="incluirRetiradas">false (por defecto): solo las vacas activas.
        /// true: también las retiradas, que salen con Activa = false.</param>
        /// <param name="ct">Permite cancelar la operación.</param>
        /// <returns>Las vacas ordenadas por Id. Lista vacía si no hay vacas.</returns>
        /// <exception cref="BaityMu.Domain.Exceptions.RecursoNoEncontradoException">La granja no existe (404).</exception>
        Task<IReadOnlyList<VacaConLitros>> ListarVacasAsync(bool incluirRetiradas, CancellationToken ct = default);
    }
}
