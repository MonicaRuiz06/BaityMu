namespace BaityMu.Domain.Entities
{
    /// <summary>
    /// Línea de una venta: cuánto salió de un lote (RN-09). Es la trazabilidad de la venta:
    /// se sabe exactamente de qué lotes salió cada kilo o litro.
    /// </summary>
    public class VentaDetalle
    {
        /// <summary>Id asignado por la base de datos.</summary>
        public int Id { get; private set; }

        /// <summary>Venta a la que pertenece.</summary>
        public int VentaId { get; private set; }

        /// <summary>Lote del que salió el producto.</summary>
        public int LoteId { get; private set; }

        /// <summary>
        /// El lote. Sirve para mostrar su fecha de producción sin copiarla aquí: se obtiene
        /// con un JOIN (ver docs/BaseDeDatos.md).
        /// </summary>
        public LoteProducto Lote { get; private set; } = null!;

        /// <summary>Cantidad que salió de ese lote. Mayor que 0.</summary>
        public decimal Cantidad { get; private set; }

        // Constructor vacío privado requerido por EF Core
        private VentaDetalle() { }

        /// <summary>Crea la línea. Lo usa solo <see cref="Venta"/>.</summary>
        internal VentaDetalle(LoteProducto lote, decimal cantidad)
        {
            Lote = lote;
            LoteId = lote.Id;
            Cantidad = cantidad;
        }
    }
}
