namespace BaityMu.Domain.Interfaces
{

    /// <summary>
    /// Unidad de trabajo: guarda en la base de datos todos los cambios pendientes
    /// en una sola transacción.
    /// </summary>
    /// <remarks>
    /// Los repositorios NO guardan: solo agregan o modifican entidades en memoria.
    /// Nada llega a SQL Server hasta que el servicio llama a SaveChangesAsync.
    ///
    /// ¿Para qué la uso en BaityMu? (RN-07, RNF-04)
    /// En una venta FIFO se crea la venta, sus detalles y se descuenta de uno o varios
    /// lotes. Con un solo SaveChangesAsync todo se guarda junto; si algo falla, no se
    /// guarda nada y el inventario queda como estaba. La venta es atómica.
    ///
    /// La implementación (UnitOfWork en Infrastructure) llama al SaveChangesAsync
    /// del DbContext de Entity Framework Core.
    /// </remarks>
    public interface IUnitOfWork
    {

        /// <summary>
        /// Confirma todos los cambios pendientes en una sola transacción.
        /// </summary>
        /// <param name="ct">Permite cancelar la operación, por ejemplo si el cliente
        /// cierra la conexión. Es opcional.</param>
        /// <returns>Cuántos registros se insertaron, modificaron o eliminaron.</returns>
        Task<int> SaveChangesAsync(CancellationToken ct = default);
    }
}
