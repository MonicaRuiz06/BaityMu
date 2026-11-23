using BaityMu.Domain.Entities;
using BaityMu.Domain.Enums;
using BaityMu.Domain.Interfaces;
using BaityMu.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BaityMu.Infrastructure.Repositories
{
    /// <summary>Repositorio de las ventas con EF Core.</summary>
    public sealed class VentaRepository : Repository<Venta>, IVentaRepository
    {
        /// <summary>Recibe el DbContext por inyección de dependencias.</summary>
        public VentaRepository(BaityMuDbContext db) : base(db)
        {
        }

        /// <summary>
        /// Ventas con sus detalles y el lote de cada detalle. ThenInclude trae el lote para
        /// mostrar su fecha de producción (el JOIN de docs/BaseDeDatos.md). Solo lectura.
        /// </summary>
        private IQueryable<Venta> ConDetalles() =>
            Db.Ventas
                .AsNoTracking()
                .Include(v => v.Detalles)
                    .ThenInclude(d => d.Lote);

        /// <inheritdoc />
        public Task<Venta?> ObtenerConDetallesAsync(int id, CancellationToken ct = default) =>
            ConDetalles().FirstOrDefaultAsync(v => v.Id == id, ct);

        /// <inheritdoc />
        /// <remarks>Usa el índice (Fecha, TipoProducto) de la tabla Ventas.</remarks>
        public async Task<IReadOnlyList<Venta>> ListarAsync(
            DateOnly? desde,
            DateOnly? hasta,
            TipoProducto? tipoProducto,
            CancellationToken ct = default)
        {
            var consulta = ConDetalles();

            if (desde is not null)
                consulta = consulta.Where(v => v.Fecha >= desde);
            if (hasta is not null)
                consulta = consulta.Where(v => v.Fecha <= hasta);
            if (tipoProducto is not null)
                consulta = consulta.Where(v => v.TipoProducto == tipoProducto);

            return await consulta
                .OrderBy(v => v.Fecha)
                .ThenBy(v => v.Id)
                .ToListAsync(ct);
        }
    }
}
