using BaityMu.Domain.Entities;
using BaityMu.Domain.Enums;

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

        /// <summary>
        /// Lotes que se pueden vender en una fecha (HU-13): de ese producto, con existencias,
        /// producidos hasta esa fecha y sin vencer, en orden FIFO. Vienen con seguimiento de
        /// cambios, porque la venta los descuenta.
        /// </summary>
        Task<IReadOnlyList<LoteProducto>> ObtenerVendiblesAsync(TipoProducto tipoProducto, DateOnly fecha, CancellationToken ct = default);

        /// <summary>
        /// Lotes que ya vencieron en una fecha y todavía tienen existencias (HU-11):
        /// FechaVencimiento &lt; fecha y CantidadDisponible &gt; 0. Vienen con seguimiento
        /// de cambios, porque se van a modificar.
        /// </summary>
        Task<IReadOnlyList<LoteProducto>> ObtenerVencidosConExistenciasAsync(DateOnly fecha, CancellationToken ct = default);

        /// <summary>
        /// Lista los lotes en orden FIFO (del más antiguo al más nuevo), con filtros
        /// opcionales (HU-10). Cada filtro en null significa "sin filtrar".
        /// </summary>
        /// <param name="tipoProducto">Solo queso o solo yogurt.</param>
        /// <param name="desde">Fecha de producción mínima (incluida).</param>
        /// <param name="hasta">Fecha de producción máxima (incluida).</param>
        /// <param name="ct">Permite cancelar la operación.</param>
        Task<IReadOnlyList<LoteProducto>> ListarAsync(
            TipoProducto? tipoProducto,
            DateOnly? desde,
            DateOnly? hasta,
            CancellationToken ct = default);
    }
}
