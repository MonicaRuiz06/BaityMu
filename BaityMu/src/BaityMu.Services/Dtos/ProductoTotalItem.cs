using BaityMu.Domain.Enums;

namespace BaityMu.Services.Dtos
{
    /// <summary>Balance de un producto desde el inicio: producido, vendido, disponible y dañado (HU-18).</summary>
    /// <remarks>
    /// Siempre se cumple: Producido = Vendido + Disponible + VencidoSinProcesar + Dañado.
    /// Escenario al día 17: Queso 25 / 12 / 13 / 0 · Yogurt 50 / 20 / 0 / 30.
    /// </remarks>
    public sealed class ProductoTotalItem
    {
        /// <summary>Queso o Yogurt.</summary>
        public TipoProducto TipoProducto { get; set; }

        /// <summary>Unidad del producto: "kg" o "L".</summary>
        public string Unidad { get; set; } = string.Empty;

        /// <summary>Total elaborado (suma de la cantidad inicial de todos los lotes).</summary>
        public decimal Producido { get; set; }

        /// <summary>Total vendido (suma de las ventas).</summary>
        public decimal Vendido { get; set; }

        /// <summary>Lo que queda en lotes que no están vencidos hoy: se puede vender.</summary>
        public decimal Disponible { get; set; }

        /// <summary>
        /// Lo que queda en lotes ya vencidos a los que todavía no se les procesó el vencimiento.
        /// No se puede vender; pasa a Dañado al procesar vencimientos (HU-11).
        /// </summary>
        public decimal VencidoSinProcesar { get; set; }

        /// <summary>Total que se marcó como dañado por vencimiento.</summary>
        public decimal Dañado { get; set; }
    }
}
