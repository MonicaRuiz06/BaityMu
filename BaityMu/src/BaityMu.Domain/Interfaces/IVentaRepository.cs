using BaityMu.Domain.Entities;
using BaityMu.Domain.Enums;

namespace BaityMu.Domain.Interfaces
{
    /// <summary>
    /// Repositorio de las ventas. El reporte de ventas (HU-15) le agrega su consulta.
    /// </summary>
    public interface IVentaRepository : IRepository<Venta>
    {
        /// <summary>Una venta con sus detalles y el lote de cada detalle (HU-14).</summary>
        /// <returns>La venta, o null si no existe.</returns>
        Task<Venta?> ObtenerConDetallesAsync(int id, CancellationToken ct = default);

        /// <summary>
        /// Lista las ventas con sus detalles, de la más antigua a la más reciente (HU-14).
        /// Cada filtro en null significa "sin filtrar".
        /// </summary>
        /// <param name="desde">Fecha mínima de la venta (incluida).</param>
        /// <param name="hasta">Fecha máxima de la venta (incluida).</param>
        /// <param name="tipoProducto">Solo queso o solo yogurt.</param>
        /// <param name="ct">Permite cancelar la operación.</param>
        Task<IReadOnlyList<Venta>> ListarAsync(
            DateOnly? desde,
            DateOnly? hasta,
            TipoProducto? tipoProducto,
            CancellationToken ct = default);
    }
}
