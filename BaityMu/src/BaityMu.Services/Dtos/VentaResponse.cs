using BaityMu.Domain.Enums;

namespace BaityMu.Services.Dtos
{
    /// <summary>Venta registrada, con el detalle de los lotes de donde salió (RN-09).</summary>
    public sealed class VentaResponse
    {
        /// <summary>Id de la venta.</summary>
        public int Id { get; set; }

        /// <summary>Día de la venta.</summary>
        public DateOnly Fecha { get; set; }

        /// <summary>Queso o Yogurt.</summary>
        public TipoProducto TipoProducto { get; set; }

        /// <summary>Cantidad vendida.</summary>
        public decimal Cantidad { get; set; }

        /// <summary>Precio por unidad en el momento de la venta.</summary>
        public decimal PrecioUnitario { get; set; }

        /// <summary>Cantidad × precio unitario.</summary>
        public decimal Total { get; set; }

        /// <summary>Lotes de donde salió el producto, del más antiguo al más nuevo (FIFO).</summary>
        public List<VentaDetalleResponse> Detalles { get; set; } = new();
    }

    /// <summary>Parte de una venta que salió de un lote.</summary>
    public sealed class VentaDetalleResponse
    {
        /// <summary>Id del lote.</summary>
        public int LoteId { get; set; }

        /// <summary>Fecha de producción del lote.</summary>
        public DateOnly FechaProduccion { get; set; }

        /// <summary>Cantidad que salió de ese lote.</summary>
        public decimal Cantidad { get; set; }
    }
}
