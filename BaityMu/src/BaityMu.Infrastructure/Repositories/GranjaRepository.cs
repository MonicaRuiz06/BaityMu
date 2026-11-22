using BaityMu.Domain.Entities;
using BaityMu.Domain.Interfaces;
using BaityMu.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BaityMu.Infrastructure.Repositories
{
    /// <summary>
    /// Repositorio de la granja con EF Core.
    /// </summary>
    public sealed class GranjaRepository : Repository<Granja>, IGranjaRepository
    {
        /// <summary>Recibe el DbContext por inyección de dependencias.</summary>
        public GranjaRepository(BaityMuDbContext db) : base(db)
        {
        }

        /// <inheritdoc />
        /// <remarks>
        /// Include(g => g.Vacas) le pide a EF que traiga también las vacas en la misma
        /// consulta (un JOIN). Sin él, la lista de vacas llegaría vacía y n sería 0.
        /// </remarks>
        public Task<Granja?> ObtenerConVacasAsync(CancellationToken ct = default) =>
            Db.Granjas
                .Include(g => g.Vacas)
                .OrderBy(g => g.Id)
                .FirstOrDefaultAsync(ct);
    }
}
