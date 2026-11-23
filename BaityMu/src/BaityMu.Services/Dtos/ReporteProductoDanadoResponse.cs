using BaityMu.Domain.Enums;

namespace BaityMu.Services.Dtos
{
    /// <summary>Reporte del producto que se perdió por vencimiento (HU-19).</summary>
    /// <remarks>El rango (desde, hasta) se aplica a la fecha de vencimiento de los lotes.</remarks>
    public sealed class ReporteProductoDanadoResponse
    {
        /// <summary>Fecha de vencimiento mínima del filtro (null = sin límite).</summary>
        public DateOnly? Desde { get; set; }

        /// <summary>Fecha de vencimiento máxima del filtro (null = sin límite).</summary>
        public DateOnly? Hasta { get; set; }

        /// <summary>Producto del filtro (null = todos).</summary>
        public TipoProducto? TipoProducto { get; set; }

        /// <summary>Cada lote con producto dañado, ordenado por fecha de vencimiento.</summary>
        public List<LoteDanadoItem> Lotes { get; set; } = new();

        /// <summary>Lo dañado de cada producto en el período (solo los productos con daño).</summary>
        public List<TotalDanadoProducto> TotalesPorProducto { get; set; } = new();
    }

    /// <summary>Un lote dentro del reporte de producto dañado.</summary>
    public sealed class LoteDanadoItem
    {
        /// <summary>Id del lote.</summary>
        public int LoteId { get; set; }

        /// <summary>Queso o Yogurt.</summary>
        public TipoProducto TipoProducto { get; set; }

        /// <summary>Unidad del producto: "kg" o "L".</summary>
        public string Unidad { get; set; } = string.Empty;

        /// <summary>Día en que se elaboró el lote.</summary>
        public DateOnly FechaProduccion { get; set; }

        /// <summary>Último día en que el lote se podía vender.</summary>
        public DateOnly FechaVencimiento { get; set; }

        /// <summary>Cantidad que se perdió.</summary>
        public decimal CantidadDañada { get; set; }
    }

    /// <summary>Total dañado de un producto.</summary>
    public sealed class TotalDanadoProducto
    {
        /// <summary>Queso o Yogurt.</summary>
        public TipoProducto TipoProducto { get; set; }

        /// <summary>Unidad del producto: "kg" o "L".</summary>
        public string Unidad { get; set; } = string.Empty;

        /// <summary>Cuántos lotes tuvieron daño.</summary>
        public int NumeroLotes { get; set; }

        /// <summary>Cantidad total dañada.</summary>
        public decimal Cantidad { get; set; }
    }
}
