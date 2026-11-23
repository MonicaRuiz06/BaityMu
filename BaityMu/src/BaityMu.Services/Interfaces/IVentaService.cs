using BaityMu.Domain.Entities;
using BaityMu.Domain.Enums;
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

        /// <summary>Consulta una venta con los lotes de donde salió (HU-14).</summary>
        /// <exception cref="BaityMu.Domain.Exceptions.RecursoNoEncontradoException">La venta no existe (404).</exception>
        Task<Venta> ObtenerAsync(int id, CancellationToken ct = default);

        /// <summary>Lista las ventas, con filtros opcionales por fechas y producto (HU-14).</summary>
        /// <exception cref="BaityMu.Domain.Exceptions.ReglaDeNegocioException">desde es posterior a hasta (400).</exception>
        Task<IReadOnlyList<Venta>> ListarAsync(
            DateOnly? desde,
            DateOnly? hasta,
            TipoProducto? tipoProducto,
            CancellationToken ct = default);
    }
}
