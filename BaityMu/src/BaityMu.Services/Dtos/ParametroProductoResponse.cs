using BaityMu.Domain.Enums;

namespace BaityMu.Services.Dtos
{
    /// <summary>Parámetros de un producto tal como los devuelve la API.</summary>
    public sealed class ParametroProductoResponse
    {
        /// <summary>"Queso" o "Yogurt".</summary>
        public TipoProducto TipoProducto { get; set; }

        /// <summary>Unidad de venta: "kg" o "L".</summary>
        public string Unidad { get; set; } = string.Empty;

        /// <summary>Rendimiento: litros de leche por unidad.</summary>
        public decimal LitrosPorUnidad { get; set; }

        /// <summary>Días que dura antes de vencerse.</summary>
        public int VidaUtilDias { get; set; }

        /// <summary>Precio de venta por unidad.</summary>
        public decimal PrecioUnitario { get; set; }
    }
}
