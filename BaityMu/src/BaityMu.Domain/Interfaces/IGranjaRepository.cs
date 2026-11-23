using BaityMu.Domain.Entities;

namespace BaityMu.Domain.Interfaces
{
    /// <summary>
    /// Repositorio de la granja. Además de las operaciones comunes, trae la granja
    /// completa (vacas y productos), que es lo que necesitan sus reglas (n, nombres
    /// únicos, rendimientos...).
    /// </summary>
    public interface IGranjaRepository : IRepository<Granja>
    {
        /// <summary>
        /// Devuelve la granja de Baity con todas sus vacas y los parámetros de sus productos.
        /// </summary>
        /// <returns>La granja, o null si todavía no se ha sembrado.</returns>
        /// <remarks>Solo existe una granja (ver docs/BaseDeDatos.md).</remarks>
        Task<Granja?> ObtenerCompletaAsync(CancellationToken ct = default);
    }
}
