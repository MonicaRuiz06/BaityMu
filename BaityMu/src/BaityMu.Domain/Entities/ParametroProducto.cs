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
    /// appsettings.json (RNF-09) y se cambian con PUT /api/granja (HU-06).
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

            TipoProducto = tipoProducto;
            Actualizar(unidad, litrosPorUnidad, vidaUtilDias, precioUnitario);
        }

        /// <summary>
        /// Cantidad de producto que sale de unos litros de leche (RN-03): litros ÷ rendimiento.
        /// Por ejemplo, 100 L de leche con 10 L/kg dan 10 kg de queso.
        /// </summary>
        /// <remarks>
        /// Se redondea hacia abajo a 3 decimales (la precisión de la tabla), para no
        /// registrar nunca más producto del que realmente salió.
        /// </remarks>
        public decimal CalcularCantidad(decimal litros) =>
            Math.Round(litros / LitrosPorUnidad, 3, MidpointRounding.ToZero);

        /// <summary>
        /// Fecha en que vence un lote producido en una fecha: producción + vida útil (RN-04).
        /// Por ejemplo, yogurt del 2026-12-01 con 15 días de vida útil vence el 2026-12-16.
        /// </summary>
        public DateOnly CalcularVencimiento(DateOnly fechaProduccion) =>
            fechaProduccion.AddDays(VidaUtilDias);

        /// <summary>
        /// Cambia los parámetros del producto (HU-06). Primero valida todo y solo después
        /// asigna: si un valor es inválido, no cambia nada.
        /// </summary>
        /// <remarks>
        /// Los lotes y ventas que ya existen no se ven afectados: cada lote guarda su propia
        /// fecha de vencimiento y cada venta su propio precio cuando se crean (criterio 3).
        /// </remarks>
        /// <exception cref="ReglaDeNegocioException">Si algún valor no es válido.</exception>
        internal void Actualizar(string unidad, decimal litrosPorUnidad, int vidaUtilDias, decimal precioUnitario)
        {
            if (string.IsNullOrWhiteSpace(unidad))
                throw new ReglaDeNegocioException($"La unidad del {TipoProducto} es obligatoria.");

            if (litrosPorUnidad <= 0)
                throw new ReglaDeNegocioException($"Los litros por unidad del {TipoProducto} deben ser mayores a cero.");

            if (vidaUtilDias <= 0)
                throw new ReglaDeNegocioException($"La vida útil del {TipoProducto} debe ser mayor a cero días.");

            if (precioUnitario < 0)
                throw new ReglaDeNegocioException($"El precio del {TipoProducto} no puede ser negativo.");

            Unidad = unidad.Trim();
            LitrosPorUnidad = litrosPorUnidad;
            VidaUtilDias = vidaUtilDias;
            PrecioUnitario = precioUnitario;
        }
    }
}
