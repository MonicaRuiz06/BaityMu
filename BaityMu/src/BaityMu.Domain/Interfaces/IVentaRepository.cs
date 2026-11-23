using BaityMu.Domain.Entities;

namespace BaityMu.Domain.Interfaces
{
    /// <summary>
    /// Repositorio de las ventas. La consulta de ventas (HU-14) y el reporte (HU-15) le
    /// agregan sus métodos.
    /// </summary>
    public interface IVentaRepository : IRepository<Venta>
    {
    }
}
