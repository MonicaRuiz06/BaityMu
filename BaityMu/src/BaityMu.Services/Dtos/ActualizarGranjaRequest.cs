using BaityMu.Domain.Enums;

namespace BaityMu.Services.Dtos
{
    /// <summary>
    /// Datos para cambiar la configuración de la granja (PUT /api/granja, HU-06).
    /// Lo que no se envía se conserva.
    /// </summary>
    public sealed class ActualizarGranjaRequest
    {
        /// <summary>Nuevo l: litros por vaca por día. Mayor que 0. Si no se envía, no cambia.</summary>
        /// <example>16</example>
        public decimal? LitrosPorVaca { get; set; }

        /// <summary>Productos a configurar. Los que no aparecen en la lista no cambian.</summary>
        public List<ActualizarProductoRequest> Productos { get; set; } = new();
    }

    /// <summary>Nuevos parámetros de un producto.</summary>
    public sealed class ActualizarProductoRequest
    {
        /// <summary>"Queso" o "Yogurt".</summary>
        /// <example>Yogurt</example>
        public TipoProducto TipoProducto { get; set; }

        /// <summary>Unidad de venta ("kg" o "L"). Si no se envía, se conserva la actual.</summary>
        /// <example>L</example>
        public string? Unidad { get; set; }

        /// <summary>Rendimiento: litros de leche por unidad. Mayor que 0.</summary>
        /// <example>1</example>
        public decimal LitrosPorUnidad { get; set; }

        /// <summary>Vida útil en días. Mayor que 0.</summary>
        /// <example>15</example>
        public int VidaUtilDias { get; set; }

        /// <summary>Precio por unidad. 0 o más.</summary>
        /// <example>6500</example>
        public decimal PrecioUnitario { get; set; }
    }
}
