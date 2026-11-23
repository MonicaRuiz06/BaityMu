namespace BaityMu.Domain.Exceptions
{
    /// <summary>
    /// Error que se lanza cuando las existencias no vencidas no alcanzan para una venta
    /// (RN-07). La venta se rechaza completa y el inventario no cambia. Responde HTTP 409
    /// porque hereda de <see cref="ConflictoException"/>.
    /// </summary>
    public sealed class InventarioInsuficienteException : ConflictoException
    {
        /// <summary>Crea el error con lo que había y lo que se pidió.</summary>
        /// <param name="message">Por ejemplo: "Solo hay 0 L de Yogurt sin vencer y se pidieron 5 L."</param>
        public InventarioInsuficienteException(string message) : base(message)
        {
        }
    }
}
