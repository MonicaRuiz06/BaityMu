namespace BaityMu.Domain.Exceptions
{

    /// <summary>
    /// Error que se lanza cuando la operación choca con el estado actual de la granja.
    /// Se responde con HTTP 409 (Conflict).
    /// </summary>
    /// <remarks>
    /// Se diferencia del 400 en que los datos están bien escritos, pero la granja no
    /// permite la operación en este momento.
    ///
    /// Ejemplos en BaityMu:
    /// - Registrar dos veces la producción de una vaca en la misma fecha (RN-01, HU-07).
    /// - Transformar más leche de la que se produjo ese día (RN-02, HU-09).
    /// - Vender más de lo que hay sin vencer; la venta se rechaza completa (RN-07, HU-13).
    ///
    /// No es abstract: se puede lanzar directamente. En cada historia se pueden crear
    /// hijas más específicas que hereden de ella, como InventarioInsuficienteException.
    /// </remarks>
    public class ConflictoException : DomainException
    {

        /// <summary>
        /// Crea el error explicando por qué la granja no permite la operación.
        /// </summary>
        /// <param name="message">Por ejemplo: "Solo quedan 30 L de yogurt sin vencer y se pidieron 50 L."</param>
        public ConflictoException(string message) : base(message)
        {
        }
    }
}
