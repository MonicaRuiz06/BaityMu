using BaityMu.Domain.Entities;

namespace BaityMu.Domain.Interfaces
{
    /// <summary>
    /// Repositorio de los lotes de producto: el inventario. Las historias del inventario
    /// y las ventas (HU-10 a HU-13) le agregan sus consultas.
    /// </summary>
    public interface ILoteRepository : IRepository<LoteProducto>
    {
        /// <summary>
        /// Suma los litros de leche de una fecha que ya se usaron en lotes (RN-02, HU-08).
        /// </summary>
        /// <returns>0 si esa leche no se ha transformado.</returns>
        Task<decimal> LitrosTransformadosAsync(DateOnly fechaProduccion, CancellationToken ct = default);
    }
}
