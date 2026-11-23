using BaityMu.Domain.Entities;

namespace BaityMu.Domain.Interfaces
{
    /// <summary>
    /// Repositorio de la producción de leche. La HU-07 le agrega el registro diario.
    /// </summary>
    public interface IProduccionRepository : IRepository<ProduccionLeche>
    {
        /// <summary>
        /// Suma los litros que ha producido cada vaca en toda su historia (HU-02).
        /// </summary>
        /// <returns>
        /// Un diccionario VacaId → litros totales. Las vacas que nunca se han ordeñado
        /// no aparecen: quien lo use debe tomarlas como 0 L.
        /// </returns>
        Task<IReadOnlyDictionary<int, decimal>> SumarLitrosPorVacaAsync(CancellationToken ct = default);

        /// <summary>
        /// Devuelve el historial de producción de una vaca, del día más antiguo al más reciente (HU-03).
        /// </summary>
        /// <param name="vacaId">Id de la vaca.</param>
        /// <param name="ct">Permite cancelar la operación.</param>
        /// <returns>Los ordeños de la vaca. Lista vacía si nunca se ha ordeñado.</returns>
        Task<IReadOnlyList<ProduccionLeche>> ListarPorVacaAsync(int vacaId, CancellationToken ct = default);
    }
}
