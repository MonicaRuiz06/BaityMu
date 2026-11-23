using BaityMu.Domain.Entities;
using BaityMu.Services.Dtos;

namespace BaityMu.Services.Interfaces
{
    /// <summary>Casos de uso de las ventas (Épica 5). Rol: BaityVendedor.</summary>
    public interface IVentaService
    {
        /// <summary>Registra una venta descontando el inventario en orden FIFO (HU-13).</summary>
        /// <param name="request">Fecha opcional, producto y cantidad.</param>
        /// <param name="ct">Permite cancelar la operación.</param>
        /// <returns>La venta guardada, con sus detalles.</returns>
        /// <exception cref="BaityMu.Domain.Exceptions.ReglaDeNegocioException">Cantidad ≤ 0 o producto no configurado (400).</exception>
        /// <exception cref="BaityMu.Domain.Exceptions.InventarioInsuficienteException">No alcanza el producto no vencido (409).</exception>
        Task<Venta> RegistrarVentaAsync(VentaRequest request, CancellationToken ct = default);
    }
}
