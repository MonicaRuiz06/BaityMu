using BaityMu.Domain.Enums;
using BaityMu.Services.Modelos;

namespace BaityMu.Services.Interfaces
{
    /// <summary>
    /// Casos de uso del inventario (Épica 4). Rol: BaityBodeguero.
    /// </summary>
    public interface IInventarioService
    {
        /// <summary>Lista los lotes con su estado (HU-10).</summary>
        /// <param name="tipoProducto">Filtro opcional: solo queso o solo yogurt.</param>
        /// <param name="desde">Filtro opcional por fecha de producción (incluida).</param>
        /// <param name="hasta">Filtro opcional por fecha de producción (incluida).</param>
        /// <param name="fecha">Fecha para calcular el estado. Si es null, la de hoy.</param>
        /// <param name="ct">Permite cancelar la operación.</param>
        /// <exception cref="BaityMu.Domain.Exceptions.ReglaDeNegocioException">desde es posterior a hasta (400).</exception>
        Task<IReadOnlyList<LoteConEstado>> ListarLotesAsync(
            TipoProducto? tipoProducto,
            DateOnly? desde,
            DateOnly? hasta,
            DateOnly? fecha,
            CancellationToken ct = default);

        /// <summary>Consulta un lote por Id con su estado (HU-10).</summary>
        /// <exception cref="BaityMu.Domain.Exceptions.RecursoNoEncontradoException">El lote no existe (404).</exception>
        Task<LoteConEstado> ObtenerLoteAsync(int id, DateOnly? fecha, CancellationToken ct = default);

        /// <summary>
        /// Resumen por producto: cantidad vendible, vencida sin procesar y dañada (HU-10).
        /// Siempre trae queso y yogurt, aunque alguno no tenga lotes.
        /// </summary>
        Task<IReadOnlyList<ResumenInventario>> ObtenerResumenAsync(DateOnly? fecha, CancellationToken ct = default);
    }
}
