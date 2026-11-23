using BaityMu.Domain.Enums;
using BaityMu.Domain.Exceptions;

namespace BaityMu.Domain.Entities
{
    /// <summary>
    /// Lote de producto: lo que sale de una transformación de leche (RN-04). Es la unidad
    /// del inventario: las ventas descuentan de los lotes en orden FIFO (HU-13).
    /// </summary>
    /// <remarks>
    /// La fecha de vencimiento se calcula al crear el lote y queda fija. Así, si después se
    /// cambia la vida útil del producto (HU-06), los lotes que ya existen no cambian.
    /// </remarks>
    public class LoteProducto
    {
        /// <summary>Id asignado por la base de datos. También desempata el orden FIFO.</summary>
        public int Id { get; private set; }

        /// <summary>Queso o Yogurt.</summary>
        public TipoProducto TipoProducto { get; private set; }

        /// <summary>Fecha de la leche que se usó.</summary>
        public DateOnly FechaProduccion { get; private set; }

        /// <summary>Último día en que se puede vender: producción + vida útil.</summary>
        public DateOnly FechaVencimiento { get; private set; }

        /// <summary>Litros de leche que se usaron.</summary>
        public decimal LitrosUsados { get; private set; }

        /// <summary>Cantidad con la que nació el lote (kg o L): litros ÷ rendimiento.</summary>
        public decimal CantidadInicial { get; private set; }

        /// <summary>Cantidad que queda para vender.</summary>
        public decimal CantidadDisponible { get; private set; }

        /// <summary>Cantidad que se perdió por vencimiento (HU-11, RN-08).</summary>
        public decimal CantidadDanada { get; private set; }

        // Constructor vacío privado requerido por EF Core
        private LoteProducto() { }

        /// <summary>
        /// Transforma leche de un día en un lote de un solo producto (HU-09).
        /// </summary>
        /// <param name="parametro">Parámetros del producto: rendimiento y vida útil.</param>
        /// <param name="fechaProduccion">Fecha de la leche que se transforma.</param>
        /// <param name="litros">Litros que se van a usar. Mayores que 0.</param>
        /// <param name="litrosProducidosEseDia">Leche total que se produjo esa fecha.</param>
        /// <param name="litrosYaTransformados">Leche de esa fecha que ya se usó en otros lotes.</param>
        /// <returns>El lote nuevo, con todo su producto disponible. Todavía no está guardado.</returns>
        /// <exception cref="ReglaDeNegocioException">Si los litros son 0 o negativos (400).</exception>
        /// <exception cref="LecheInsuficienteException">
        /// Si ese día no hubo producción, o si los litros superan lo que queda (409, RN-02).
        /// </exception>
        /// <remarks>
        /// Es la prueba mínima del taller: la suma de litros transformados de un día nunca
        /// puede superar lo producido ese día. La leche que sobra es "no aprovechada" y no
        /// se guarda como producto.
        /// </remarks>
        public static LoteProducto Crear(
            ParametroProducto parametro,
            DateOnly fechaProduccion,
            decimal litros,
            decimal litrosProducidosEseDia,
            decimal litrosYaTransformados)
        {
            if (litros <= 0)
                throw new ReglaDeNegocioException("Los litros a transformar deben ser mayores a cero.");

            if (litrosProducidosEseDia <= 0)
                throw new LecheInsuficienteException(
                    $"No hay producción registrada el {fechaProduccion:yyyy-MM-dd}: no hay leche para transformar.");

            var disponibles = litrosProducidosEseDia - litrosYaTransformados;
            if (litros > disponibles)
                throw new LecheInsuficienteException(
                    $"El {fechaProduccion:yyyy-MM-dd} solo quedan {disponibles:0.##} L sin transformar " +
                    $"y se pidieron {litros:0.##} L.");

            var cantidad = parametro.CalcularCantidad(litros);
            if (cantidad <= 0)
                throw new ReglaDeNegocioException(
                    $"Con {litros:0.##} L no alcanza para una cantidad mínima de {parametro.TipoProducto}.");

            return new LoteProducto
            {
                TipoProducto = parametro.TipoProducto,
                FechaProduccion = fechaProduccion,
                FechaVencimiento = parametro.CalcularVencimiento(fechaProduccion),
                LitrosUsados = litros,
                CantidadInicial = cantidad,
                CantidadDisponible = cantidad,
                CantidadDanada = 0
            };
        }

        /// <summary>
        /// Indica si el lote está vencido en una fecha: fecha &gt; vencimiento (RN-08).
        /// El mismo día del vencimiento todavía se puede vender.
        /// </summary>
        public bool EstaVencido(DateOnly fecha) => fecha > FechaVencimiento;

        /// <summary>
        /// Pasa a dañado lo que queda del lote porque ya venció (HU-11, RN-08).
        /// </summary>
        /// <param name="fecha">Día en que se procesan los vencimientos.</param>
        /// <returns>
        /// La cantidad que pasó a dañada. 0 si el lote ya estaba agotado o ya se había
        /// procesado, así que procesar dos veces no duplica el producto dañado.
        /// </returns>
        /// <exception cref="ReglaDeNegocioException">
        /// Si el lote todavía no está vencido en esa fecha. El mismo día del vencimiento
        /// todavía se puede vender, así que no se daña.
        /// </exception>
        public decimal MarcarComoDanado(DateOnly fecha)
        {
            if (!EstaVencido(fecha))
                throw new ReglaDeNegocioException(
                    $"El lote {Id} vence el {FechaVencimiento:yyyy-MM-dd}: el {fecha:yyyy-MM-dd} todavía no está vencido.");

            var cantidad = CantidadDisponible;
            CantidadDanada += cantidad;
            CantidadDisponible = 0;
            return cantidad;
        }

        /// <summary>Estado del lote en una fecha (HU-10).</summary>
        /// <param name="fecha">Día de la consulta.</param>
        /// <returns>
        /// - Con existencias: <see cref="EstadoLote.Vencido"/> si pasó su vencimiento, o
        ///   <see cref="EstadoLote.Disponible"/> si no.
        /// - Sin existencias: <see cref="EstadoLote.Dañado"/> si algo se perdió por
        ///   vencimiento, o <see cref="EstadoLote.Agotado"/> si se vendió todo.
        /// </returns>
        public EstadoLote ObtenerEstado(DateOnly fecha)
        {
            if (CantidadDisponible > 0)
                return EstaVencido(fecha) ? EstadoLote.Vencido : EstadoLote.Disponible;

            return CantidadDanada > 0 ? EstadoLote.Dañado : EstadoLote.Agotado;
        }
    }
}
