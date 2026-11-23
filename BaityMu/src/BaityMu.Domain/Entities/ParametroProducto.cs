using BaityMu.Domain.Enums;
using BaityMu.Domain.Exceptions;

namespace BaityMu.Domain.Entities
{
    /// <summary>
    /// Parámetros de un producto de la granja: cuántos litros de leche se necesitan por
    /// unidad (rendimiento), cuántos días dura (vida útil) y a cuánto se vende.
    /// </summary>
    /// <remarks>
    /// Ejemplo del taller: el queso usa 10 L por kg, dura 30 días y vale 28 000 por kg;
    /// el yogurt usa 1 L por litro y dura 15 días. Los valores iniciales salen de
    /// appsettings.json (RNF-09) y se cambian con la HU-06.
    /// </remarks>
    public class ParametroProducto
    {
        /// <summary>Id asignado por la base de datos.</summary>
        public int Id { get; private set; }

        /// <summary>Granja a la que pertenece.</summary>
        public int GranjaId { get; private set; }

        /// <summary>Queso o Yogurt. Cada granja tiene un solo parámetro por producto.</summary>
        public TipoProducto TipoProducto { get; private set; }

        /// <summary>Unidad de venta: "kg" para el queso, "L" para el yogurt.</summary>
        public string Unidad { get; private set; } = string.Empty;

        /// <summary>Rendimiento: litros de leche que se necesitan para una unidad. Mayor que 0.</summary>
        public decimal LitrosPorUnidad { get; private set; }

        /// <summary>Días que dura el producto antes de vencerse. Mayor que 0.</summary>
        public int VidaUtilDias { get; private set; }

        /// <summary>Precio de venta de una unidad. 0 o más.</summary>
        public decimal PrecioUnitario { get; private set; }

        // Constructor vacío privado requerido por EF Core
        private ParametroProducto() { }

        /// <summary>Crea los parámetros de un producto. Lo usa solo <see cref="Granja"/>.</summary>
        /// <exception cref="ReglaDeNegocioException">Si algún valor no es válido.</exception>
        internal ParametroProducto(
            TipoProducto tipoProducto,
            string unidad,
            decimal litrosPorUnidad,
            int vidaUtilDias,
            decimal precioUnitario)
        {
            if (!Enum.IsDefined(tipoProducto))
                throw new ReglaDeNegocioException("El tipo de producto debe ser Queso o Yogurt.");

            if (string.IsNullOrWhiteSpace(unidad))
                throw new ReglaDeNegocioException($"La unidad del {tipoProducto} es obligatoria.");

            if (litrosPorUnidad <= 0)
                throw new ReglaDeNegocioException($"Los litros por unidad del {tipoProducto} deben ser mayores a cero.");

            if (vidaUtilDias <= 0)
                throw new ReglaDeNegocioException($"La vida útil del {tipoProducto} debe ser mayor a cero días.");

            if (precioUnitario < 0)
                throw new ReglaDeNegocioException($"El precio del {tipoProducto} no puede ser negativo.");

            TipoProducto = tipoProducto;
            Unidad = unidad.Trim();
            LitrosPorUnidad = litrosPorUnidad;
            VidaUtilDias = vidaUtilDias;
            PrecioUnitario = precioUnitario;
        }
    }
}
