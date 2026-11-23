using BaityMu.Domain.Enums;
using BaityMu.Domain.Exceptions;

namespace BaityMu.Domain.Entities
{
    /// <summary>
    /// Venta de queso o yogurt (HU-13). Descuenta el inventario en orden FIFO y guarda de
    /// qué lotes salió el producto.
    /// </summary>
    /// <remarks>
    /// El precio unitario se copia del producto en el momento de la venta (RN-09). Si después
    /// BaityAdministrador cambia el precio (HU-06), esta venta conserva el suyo.
    /// </remarks>
    public class Venta
    {
        private readonly List<VentaDetalle> _detalles = new();

        /// <summary>Id asignado por la base de datos.</summary>
        public int Id { get; private set; }

        /// <summary>Día de la venta.</summary>
        public DateOnly Fecha { get; private set; }

        /// <summary>Queso o Yogurt.</summary>
        public TipoProducto TipoProducto { get; private set; }

        /// <summary>Cantidad vendida (kg o L).</summary>
        public decimal Cantidad { get; private set; }

        /// <summary>Precio por unidad en el momento de la venta.</summary>
        public decimal PrecioUnitario { get; private set; }

        /// <summary>Cantidad × precio unitario, redondeado a 2 decimales.</summary>
        public decimal Total { get; private set; }

        /// <summary>De qué lotes salió el producto, del más antiguo al más nuevo.</summary>
        public IReadOnlyCollection<VentaDetalle> Detalles => _detalles;

        // Constructor vacío privado requerido por EF Core
        private Venta() { }

        /// <summary>
        /// Registra una venta descontando del inventario en orden FIFO (HU-13, RN-05 a RN-09).
        /// </summary>
        /// <param name="producto">Parámetros del producto: de aquí se copia el precio.</param>
        /// <param name="fecha">Día de la venta.</param>
        /// <param name="cantidad">Cantidad a vender. Mayor que 0.</param>
        /// <param name="lotes">Lotes candidatos. Pueden venir en cualquier orden: aquí se
        /// filtran (producto, existencias, vencimiento, fecha) y se ordenan FIFO.</param>
        /// <returns>La venta con sus detalles. Los lotes quedan descontados.</returns>
        /// <exception cref="ReglaDeNegocioException">Si la cantidad es 0 o negativa (400).</exception>
        /// <exception cref="InventarioInsuficienteException">
        /// Si el producto no vencido no alcanza (409). En ese caso ningún lote se toca (RN-07).
        /// </exception>
        /// <remarks>
        /// Algoritmo (sección 7 del taller):
        /// 1. Quedarse con los lotes vendibles ese día y ordenarlos por fecha de producción
        ///    (el más antiguo primero) y por Id para desempatar.
        /// 2. Si la suma de lo disponible no alcanza, rechazar todo ANTES de tocar un lote.
        /// 3. Recorrer los lotes: de cada uno se saca lo que tenga o lo que falte, lo que sea
        ///    menor, y se anota un detalle. Se para cuando ya no falta nada.
        /// </remarks>
        public static Venta Registrar(
            ParametroProducto producto,
            DateOnly fecha,
            decimal cantidad,
            IEnumerable<LoteProducto> lotes)
        {
            if (cantidad <= 0)
                throw new ReglaDeNegocioException("La cantidad a vender debe ser mayor a cero.");

            var vendibles = lotes
                .Where(l => l.TipoProducto == producto.TipoProducto && l.EsVendible(fecha))
                .OrderBy(l => l.FechaProduccion)
                .ThenBy(l => l.Id)
                .ToList();

            var existencias = vendibles.Sum(l => l.CantidadDisponible);
            if (existencias < cantidad)
                throw new InventarioInsuficienteException(
                    $"Solo hay {existencias:0.###} {producto.Unidad} de {producto.TipoProducto} sin vencer " +
                    $"el {fecha:yyyy-MM-dd} y se pidieron {cantidad:0.###} {producto.Unidad}. La venta no se registró.");

            var venta = new Venta
            {
                Fecha = fecha,
                TipoProducto = producto.TipoProducto,
                Cantidad = cantidad,
                PrecioUnitario = producto.PrecioUnitario,
                Total = Math.Round(cantidad * producto.PrecioUnitario, 2)
            };

            var falta = cantidad;
            foreach (var lote in vendibles)
            {
                if (falta == 0)
                    break;

                var tomar = Math.Min(lote.CantidadDisponible, falta);
                venta.AgregarDetalle(lote, tomar, fecha);
                falta -= tomar;
            }

            return venta;
        }

        /// <summary>Descuenta del lote y anota de dónde salió el producto.</summary>
        private void AgregarDetalle(LoteProducto lote, decimal cantidad, DateOnly fecha)
        {
            lote.Descontar(cantidad, fecha);
            _detalles.Add(new VentaDetalle(lote, cantidad));
        }
    }
}
