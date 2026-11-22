using BaityMu.Domain.Entities;

namespace BaityMu.Domain.Interfaces
{
    /// <summary>
    /// Repositorio de la granja. Además de las operaciones comunes, trae la granja con
    /// su hato completo, que es lo que necesitan sus reglas (n, nombres únicos...).
    /// </summary>
    public interface IGranjaRepository : IRepository<Granja>
    {
        /// <summary>
        /// Devuelve la granja de Baity con todas sus vacas cargadas.
        /// </summary>
        /// <returns>La granja, o null si todavía no se ha sembrado.</returns>
        /// <remarks>Solo existe una granja (ver docs/BaseDeDatos.md).</remarks>
        Task<Granja?> ObtenerConVacasAsync(CancellationToken ct = default);
    }
}
