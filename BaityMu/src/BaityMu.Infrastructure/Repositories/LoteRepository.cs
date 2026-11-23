using BaityMu.Domain.Entities;
using BaityMu.Domain.Enums;
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

        /// <inheritdoc />
        /// <remarks>
        /// Es la misma consulta que describe la sección 7 del taller y usa el índice FIFO
        /// (TipoProducto, FechaProduccion, Id). "No vencido" es FechaVencimiento &gt;= fecha,
        /// porque el día del vencimiento todavía se vende (RN-08).
        /// </remarks>
        public async Task<IReadOnlyList<LoteProducto>> ObtenerVendiblesAsync(
            TipoProducto tipoProducto,
            DateOnly fecha,
            CancellationToken ct = default) =>
            await Db.LotesProducto
                .Where(l => l.TipoProducto == tipoProducto
                            && l.CantidadDisponible > 0
                            && l.FechaProduccion <= fecha
                            && l.FechaVencimiento >= fecha)
                .OrderBy(l => l.FechaProduccion)
                .ThenBy(l => l.Id)
                .ToListAsync(ct);

        /// <inheritdoc />
        /// <remarks>
        /// Sin AsNoTracking: EF tiene que vigilar estos lotes para guardar el cambio de
        /// disponible a dañado. Usa el índice por FechaVencimiento.
        /// </remarks>
        public async Task<IReadOnlyList<LoteProducto>> ObtenerVencidosConExistenciasAsync(
            DateOnly fecha,
            CancellationToken ct = default) =>
            await Db.LotesProducto
                .Where(l => l.FechaVencimiento < fecha && l.CantidadDisponible > 0)
                .OrderBy(l => l.TipoProducto)
                .ThenBy(l => l.FechaProduccion)
                .ThenBy(l => l.Id)
                .ToListAsync(ct);

        /// <inheritdoc />
        /// <remarks>
        /// Los filtros se van agregando a la consulta solo si vienen: EF arma un único
        /// SELECT con los WHERE que hagan falta. El orden es el mismo del índice FIFO.
        /// </remarks>
        public async Task<IReadOnlyList<LoteProducto>> ListarAsync(
            TipoProducto? tipoProducto,
            DateOnly? desde,
            DateOnly? hasta,
            CancellationToken ct = default)
        {
            IQueryable<LoteProducto> consulta = Db.LotesProducto.AsNoTracking();

            if (tipoProducto is not null)
                consulta = consulta.Where(l => l.TipoProducto == tipoProducto);
            if (desde is not null)
                consulta = consulta.Where(l => l.FechaProduccion >= desde);
            if (hasta is not null)
                consulta = consulta.Where(l => l.FechaProduccion <= hasta);

            return await consulta
                .OrderBy(l => l.TipoProducto)
                .ThenBy(l => l.FechaProduccion)
                .ThenBy(l => l.Id)
                .ToListAsync(ct);
        }
    }
}
