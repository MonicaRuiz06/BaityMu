using BaityMu.Domain.Entities;
using BaityMu.Domain.Interfaces;
using BaityMu.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BaityMu.Infrastructure.Repositories
{
    /// <summary>
    /// Repositorio de los lotes de producto con EF Core.
    /// </summary>
    public sealed class LoteRepository : Repository<LoteProducto>, ILoteRepository
    {
        /// <summary>Recibe el DbContext por inyección de dependencias.</summary>
        public LoteRepository(BaityMuDbContext db) : base(db)
        {
        }

        /// <inheritdoc />
        /// <remarks>
        /// SumAsync sobre una consulta vacía da 0. Se convierte a decimal? para que EF
        /// no falle cuando no hay filas.
        /// </remarks>
        public async Task<decimal> LitrosTransformadosAsync(DateOnly fechaProduccion, CancellationToken ct = default) =>
            await Db.LotesProducto
                .Where(l => l.FechaProduccion == fechaProduccion)
                .SumAsync(l => (decimal?)l.LitrosUsados, ct) ?? 0m;
    }
}
