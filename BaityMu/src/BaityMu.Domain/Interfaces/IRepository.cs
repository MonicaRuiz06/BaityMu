namespace BaityMu.Domain.Interfaces
{

    /// <summary>
    /// Operaciones comunes a todos los repositorios de BaityMu.
    /// </summary>
    /// <typeparam name="T">La entidad que maneja el repositorio (Vaca, LoteProducto, Venta...).
    /// Debe ser una clase.</typeparam>
    /// <remarks>
    /// ¿Qué es una interfaz genérica?
    /// Un contrato escrito una sola vez para cualquier tipo T. IRepository&lt;Vaca&gt; e
    /// IRepository&lt;Venta&gt; tienen los mismos métodos sin repetir código.
    ///
    /// ¿Qué es el patrón repositorio?
    /// Una capa que esconde CÓMO se guardan los datos. Los servicios piden "dame la vaca 5"
    /// sin saber que hay SQL Server y Entity Framework detrás. Domain define el contrato y
    /// Infrastructure lo implementa; así Domain no depende de ninguna otra capa (RNF-02).
    ///
    /// Cada repositorio específico hereda de esta interfaz y agrega sus propias consultas,
    /// por ejemplo ILoteRepository.ObtenerVendiblesAsync para la venta FIFO.
    ///
    /// Los repositorios no guardan: para confirmar los cambios se usa IUnitOfWork.
    /// </remarks>
    public interface IRepository<T> where T : class
    {

        /// <summary>Busca una entidad por su Id.</summary>
        /// <returns>La entidad, o null si no existe.</returns>
        Task<T?> ObtenerPorIdAsync(int id, CancellationToken ct = default);

        /// <summary>Devuelve todas las entidades, en una lista de solo lectura.</summary>
        Task<IReadOnlyList<T>> ListarAsync(CancellationToken ct = default);

        /// <summary>
        /// Agrega una entidad nueva. No la guarda en la base de datos:
        /// se guarda al llamar a IUnitOfWork.SaveChangesAsync.
        /// </summary>
        Task AgregarAsync(T entidad, CancellationToken ct = default);
    }
}
