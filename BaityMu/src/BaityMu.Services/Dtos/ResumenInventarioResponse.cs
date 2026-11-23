using BaityMu.Domain.Enums;

namespace BaityMu.Services.Dtos
{
    /// <summary>Resumen del inventario de un producto (GET /api/inventario/resumen).</summary>
    public sealed class ResumenInventarioResponse
    {
        /// <summary>Queso o Yogurt.</summary>
        public TipoProducto TipoProducto { get; set; }

        /// <summary>Existencias sin vencer: lo que se puede vender.</summary>
        public decimal CantidadVendible { get; set; }

        /// <summary>Existencias vencidas que todavía no se procesan como dañadas.</summary>
        public decimal CantidadVencidaSinProcesar { get; set; }

        /// <summary>Lo que ya se perdió por vencimiento.</summary>
        public decimal CantidadDañada { get; set; }
    }
}
