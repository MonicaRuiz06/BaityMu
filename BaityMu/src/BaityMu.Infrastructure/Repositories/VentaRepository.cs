using BaityMu.Domain.Entities;
using BaityMu.Domain.Interfaces;
using BaityMu.Infrastructure.Data;

namespace BaityMu.Infrastructure.Repositories
{
    /// <summary>Repositorio de las ventas con EF Core.</summary>
    public sealed class VentaRepository : Repository<Venta>, IVentaRepository
    {
        /// <summary>Recibe el DbContext por inyección de dependencias.</summary>
        public VentaRepository(BaityMuDbContext db) : base(db)
        {
        }
    }
}
