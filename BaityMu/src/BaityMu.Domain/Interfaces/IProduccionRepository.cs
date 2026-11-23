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
    }
}
