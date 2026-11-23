using BaityMu.Domain.Enums;

namespace BaityMu.Services.Dtos
{
    /// <summary>Reporte de ventas de un período (HU-15).</summary>
    /// <remarks>
    /// Si no hay ventas en el rango se responde igual (200) con listas vacías y totales en 0
    /// (regla común de la Épica 6).
    /// </remarks>
    public sealed class ReporteVentasResponse
    {
        /// <summary>Fecha inicial del filtro (null = sin límite).</summary>
        public DateOnly? Desde { get; set; }

        /// <summary>Fecha final del filtro (null = sin límite).</summary>
        public DateOnly? Hasta { get; set; }

        /// <summary>Producto del filtro (null = todos).</summary>
        public TipoProducto? TipoProducto { get; set; }

        /// <summary>Cada venta del período, de la más antigua a la más reciente.</summary>
        public List<ReporteVentaItem> Ventas { get; set; } = new();

        /// <summary>Subtotales por producto (solo los productos que tuvieron ventas).</summary>
        public List<TotalVentasProducto> TotalesPorProducto { get; set; } = new();

        /// <summary>Suma del dinero de todas las ventas del reporte.</summary>
        public decimal TotalGeneral { get; set; }
    }

    /// <summary>Una venta dentro del reporte.</summary>
    public sealed class ReporteVentaItem
    {
        /// <summary>Id de la venta (para abrirla con GET /api/ventas/{id}).</summary>
        public int Id { get; set; }

        /// <summary>Día de la venta.</summary>
        public DateOnly Fecha { get; set; }

        /// <summary>Queso o Yogurt.</summary>
        public TipoProducto TipoProducto { get; set; }

        /// <summary>Cantidad vendida.</summary>
        public decimal Cantidad { get; set; }

        /// <summary>Precio por unidad que se cobró en esa venta.</summary>
        public decimal PrecioUnitario { get; set; }

        /// <summary>Cantidad × precio unitario.</summary>
        public decimal Total { get; set; }
    }

    /// <summary>Subtotal de un producto en el reporte de ventas.</summary>
    public sealed class TotalVentasProducto
    {
        /// <summary>Queso o Yogurt.</summary>
        public TipoProducto TipoProducto { get; set; }

        /// <summary>Cuántas ventas hubo de ese producto.</summary>
        public int NumeroVentas { get; set; }

        /// <summary>Cantidad total vendida (kg o L, según el producto).</summary>
        public decimal Cantidad { get; set; }

        /// <summary>Dinero total de ese producto.</summary>
        public decimal Total { get; set; }
    }
}
