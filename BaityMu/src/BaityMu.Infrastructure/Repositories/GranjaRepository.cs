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
        /// Include le pide a EF que traiga también las vacas y los productos. Sin él, esas
        /// listas llegarían vacías y n sería 0. AsSplitQuery hace una consulta por cada
        /// lista en vez de un solo JOIN gigante (vacas × productos).
        /// </remarks>
        public Task<Granja?> ObtenerCompletaAsync(CancellationToken ct = default) =>
            Db.Granjas
                .Include(g => g.Vacas)
                .Include(g => g.Productos)
                .AsSplitQuery()
                .OrderBy(g => g.Id)
                .FirstOrDefaultAsync(ct);
    }
}
