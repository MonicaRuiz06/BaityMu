using BaityMu.Domain.Entities;
using BaityMu.Domain.Interfaces;
using BaityMu.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BaityMu.Infrastructure.Repositories
{
    /// <summary>
    /// Repositorio de la producción de leche con EF Core.
    /// </summary>
    public sealed class ProduccionRepository : Repository<ProduccionLeche>, IProduccionRepository
    {
        /// <summary>Recibe el DbContext por inyección de dependencias.</summary>
        public ProduccionRepository(BaityMuDbContext db) : base(db)
        {
        }

        /// <inheritdoc />
        /// <remarks>
        /// GroupBy + Sum se traduce a un solo SELECT en SQL Server:
        /// SELECT VacaId, SUM(Litros) FROM ProduccionesLeche GROUP BY VacaId.
        /// Así no se traen todas las filas a memoria para sumarlas.
        /// </remarks>
        public async Task<IReadOnlyDictionary<int, decimal>> SumarLitrosPorVacaAsync(CancellationToken ct = default) =>
            await Db.ProduccionesLeche
                .GroupBy(p => p.VacaId)
                .Select(g => new { VacaId = g.Key, Litros = g.Sum(p => p.Litros) })
                .ToDictionaryAsync(x => x.VacaId, x => x.Litros, ct);

        /// <inheritdoc />
        /// <remarks>Usa el índice por Fecha de la tabla, creado para esta consulta.</remarks>
        public async Task<IReadOnlyList<ProduccionLeche>> ListarPorFechaAsync(DateOnly fecha, CancellationToken ct = default) =>
            await Db.ProduccionesLeche
                .AsNoTracking()
                .Where(p => p.Fecha == fecha)
                .OrderBy(p => p.VacaId)
                .ToListAsync(ct);

        /// <inheritdoc />
        /// <remarks>
        /// Los filtros solo se agregan si llegan (igual que en lotes y ventas): EF arma un solo
        /// SELECT con los WHERE que hagan falta.
        /// </remarks>
        public async Task<IReadOnlyList<ProduccionLeche>> ListarAsync(DateOnly? desde, DateOnly? hasta, CancellationToken ct = default)
        {
            IQueryable<ProduccionLeche> consulta = Db.ProduccionesLeche.AsNoTracking();

            if (desde is not null)
                consulta = consulta.Where(p => p.Fecha >= desde);
            if (hasta is not null)
                consulta = consulta.Where(p => p.Fecha <= hasta);

            return await consulta
                .OrderBy(p => p.Fecha)
                .ThenBy(p => p.VacaId)
                .ToListAsync(ct);
        }

        /// <inheritdoc />
        public async Task<IReadOnlySet<int>> VacasConProduccionAsync(DateOnly fecha, CancellationToken ct = default) =>
            (await Db.ProduccionesLeche
                .Where(p => p.Fecha == fecha)
                .Select(p => p.VacaId)
                .ToListAsync(ct))
            .ToHashSet();

        /// <inheritdoc />
        public Task AgregarVariosAsync(IEnumerable<ProduccionLeche> producciones, CancellationToken ct = default) =>
            Db.ProduccionesLeche.AddRangeAsync(producciones, ct);

        /// <inheritdoc />
        /// <remarks>
        /// AsNoTracking: estos datos solo se leen para mostrarlos, así que EF no necesita
        /// vigilar sus cambios. La consulta es más liviana.
        /// </remarks>
        public async Task<IReadOnlyList<ProduccionLeche>> ListarPorVacaAsync(int vacaId, CancellationToken ct = default) =>
            await Db.ProduccionesLeche
                .AsNoTracking()
                .Where(p => p.VacaId == vacaId)
                .OrderBy(p => p.Fecha)
                .ToListAsync(ct);
    }
}
