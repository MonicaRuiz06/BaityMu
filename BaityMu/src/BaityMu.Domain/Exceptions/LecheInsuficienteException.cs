namespace BaityMu.Domain.Exceptions
{
    /// <summary>
    /// Error que se lanza al intentar transformar más leche de la que queda disponible
    /// ese día, o leche de un día sin producción (RN-02). Responde HTTP 409 porque hereda
    /// de <see cref="ConflictoException"/>.
    /// </summary>
    public sealed class LecheInsuficienteException : ConflictoException
    {
        /// <summary>Crea el error explicando cuánta leche había y cuánta se pidió.</summary>
        /// <param name="message">Por ejemplo: "El 2026-12-01 solo quedan 50 L sin transformar y se pidieron 60 L."</param>
        public LecheInsuficienteException(string message) : base(message)
        {
        }
    }
}
