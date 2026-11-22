namespace BaityMu.Domain.Exceptions
{

    /// <summary>
    /// Error que se lanza cuando se busca algo que no existe.
    /// Se responde con HTTP 404 (Not Found).
    /// </summary>
    /// <remarks>
    /// Ejemplos en BaityMu:
    /// - Consultar o retirar una vaca con un Id que no existe (HU-03, HU-04).
    /// - Consultar la producción de una fecha sin registros (HU-08).
    /// - Consultar un lote o una venta inexistente (HU-10, HU-14).
    ///
    /// Tiene dos constructores (sobrecarga): uno recibe el mensaje completo y el otro
    /// lo arma solo a partir del recurso y el Id.
    /// </remarks>
    public class RecursoNoEncontradoException : DomainException
    {

        /// <summary>
        /// Crea el error con un mensaje escrito a mano.
        /// </summary>
        /// <param name="message">Por ejemplo: "No hay producción registrada el 2026-12-05."</param>
        public RecursoNoEncontradoException(string message) : base(message)
        {
        }

        /// <summary>
        /// Crea el error armando el mensaje a partir del recurso y su Id.
        /// </summary>
        /// <param name="recurso">Qué se buscó, con su artículo: "una vaca", "un lote", "una venta".</param>
        /// <param name="id">El Id que no se encontró.</param>
        /// <example>
        /// new RecursoNoEncontradoException("una vaca", 99)
        /// produce el mensaje: "No existe una vaca con id 99."
        /// </example>
        public RecursoNoEncontradoException(string recurso, int id) : base($"No existe {recurso} con id {id}.")
        {
        }
    }
}
