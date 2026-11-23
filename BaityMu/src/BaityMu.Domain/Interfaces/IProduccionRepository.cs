using BaityMu.Domain.Entities;

namespace BaityMu.Domain.Interfaces
{
    /// <summary>
    /// Repositorio de la producción de leche: lo que dio cada vaca cada día.
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

        /// <summary>Todos los ordeños de una fecha, ordenados por vaca (HU-08).</summary>
        /// <returns>Lista vacía si ese día no hubo producción.</returns>
        Task<IReadOnlyList<ProduccionLeche>> ListarPorFechaAsync(DateOnly fecha, CancellationToken ct = default);

        /// <summary>Ids de las vacas que ya tienen producción en una fecha (HU-07, RN-01).</summary>
        Task<IReadOnlySet<int>> VacasConProduccionAsync(DateOnly fecha, CancellationToken ct = default);

        /// <summary>
        /// Agrega varios registros a la vez. Se guardan al llamar a IUnitOfWork.SaveChangesAsync.
        /// </summary>
        Task AgregarVariosAsync(IEnumerable<ProduccionLeche> producciones, CancellationToken ct = default);
    }
}
