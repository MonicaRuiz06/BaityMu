using BaityMu.Domain.Enums;

namespace BaityMu.Services.Dtos
{
    /// <summary>Reporte de cuánto queso y yogurt se elaboró cada día (HU-17).</summary>
    public sealed class ReporteProductoDiarioResponse
    {
        /// <summary>Fecha inicial del filtro (null = sin límite).</summary>
        public DateOnly? Desde { get; set; }

        /// <summary>Fecha final del filtro (null = sin límite).</summary>
        public DateOnly? Hasta { get; set; }

        /// <summary>Un renglón por cada día y producto elaborado, ordenado por fecha y producto.</summary>
        public List<ProductoElaboradoItem> Dias { get; set; } = new();

        /// <summary>Lo elaborado de cada producto en todo el período.</summary>
        public List<ProductoElaboradoItem> TotalesPorProducto { get; set; } = new();
    }

    /// <summary>Lo que se elaboró de un producto (en un día o en todo el período).</summary>
    public sealed class ProductoElaboradoItem
    {
        /// <summary>Día de producción. Es null en los totales del período.</summary>
        public DateOnly? Fecha { get; set; }

        /// <summary>Queso o Yogurt.</summary>
        public TipoProducto TipoProducto { get; set; }

        /// <summary>Unidad del producto: "kg" o "L".</summary>
        public string Unidad { get; set; } = string.Empty;

        /// <summary>Cantidad elaborada (la inicial de los lotes, sin descontar ventas).</summary>
        public decimal Cantidad { get; set; }

        /// <summary>Litros de leche que se usaron.</summary>
        public decimal LitrosUsados { get; set; }

        /// <summary>Cuántos lotes se crearon.</summary>
        public int NumeroLotes { get; set; }
    }
}
