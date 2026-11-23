using BaityMu.Domain.Enums;

namespace BaityMu.Infrastructure.Seed
{
    /// <summary>
    /// Valores iniciales de la granja. Se leen de la sección "GranjaInicial" de
    /// appsettings.json, para que n y l no queden fijos en el código (RNF-09).
    /// </summary>
    public sealed class GranjaInicialOptions
    {
        /// <summary>Nombre de la sección en appsettings.json.</summary>
        public const string Seccion = "GranjaInicial";

        /// <summary>Nombre de la granja. Por defecto "Granja BaityMu".</summary>
        public string Nombre { get; set; } = "Granja BaityMu";

        /// <summary>n inicial: cuántas vacas se registran al crear la granja.</summary>
        public int NumeroVacas { get; set; }

        /// <summary>l: litros de leche que da cada vaca por día.</summary>
        public decimal LitrosPorVaca { get; set; }

        /// <summary>Raza con la que se registran las vacas iniciales.</summary>
        public string RazaVacasIniciales { get; set; } = "Holstein";

        /// <summary>Parámetros iniciales del queso y el yogurt (HU-05).</summary>
        public List<ProductoInicialOptions> Productos { get; set; } = new();
    }

    /// <summary>
    /// Parámetros iniciales de un producto, tal como vienen en appsettings.json.
    /// </summary>
    public sealed class ProductoInicialOptions
    {
        /// <summary>"Queso" o "Yogurt". El texto se convierte solo en el enum.</summary>
        public TipoProducto TipoProducto { get; set; }

        /// <summary>Unidad de venta: "kg" o "L".</summary>
        public string Unidad { get; set; } = string.Empty;

        /// <summary>Litros de leche por unidad (rendimiento).</summary>
        public decimal LitrosPorUnidad { get; set; }

        /// <summary>Días que dura el producto.</summary>
        public int VidaUtilDias { get; set; }

        /// <summary>Precio de venta por unidad.</summary>
        public decimal PrecioUnitario { get; set; }
    }
}
