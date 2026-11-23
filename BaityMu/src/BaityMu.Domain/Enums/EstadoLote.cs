namespace BaityMu.Domain.Enums
{
    /// <summary>
    /// Estado de un lote del inventario en una fecha (HU-10).
    /// </summary>
    /// <remarks>
    /// No se guarda en la base de datos: se calcula con <c>LoteProducto.ObtenerEstado(fecha)</c>,
    /// porque depende del día en que se consulta. El mismo lote está Disponible el día 16
    /// y Vencido el día 17.
    /// </remarks>
    public enum EstadoLote
    {
        /// <summary>Tiene existencias y no está vencido: se puede vender.</summary>
        Disponible = 1,

        /// <summary>Se vendió todo. No queda nada ni se perdió nada.</summary>
        Agotado = 2,

        /// <summary>Tiene existencias pero ya pasó su vencimiento: falta procesarlo (HU-11).</summary>
        Vencido = 3,

        /// <summary>Se procesó el vencimiento: lo que quedaba pasó a dañado (RN-08).</summary>
        Dañado = 4
    }
}
