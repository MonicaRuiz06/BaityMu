namespace BaityMu.Domain.Exceptions
{
    /// <summary>
    /// Error que se lanza al registrar la producción de una vaca en una fecha que ya
    /// tiene registro (RN-01). Se responde con HTTP 409, porque hereda de
    /// <see cref="ConflictoException"/>.
    /// </summary>
    /// <remarks>
    /// Al heredar, el manejador global de errores no necesita ningún cambio: lo trata
    /// como cualquier ConflictoException.
    /// </remarks>
    public sealed class ProduccionDuplicadaException : ConflictoException
    {
        /// <summary>Crea el error con la fecha y las vacas que ya tenían registro.</summary>
        /// <param name="fecha">Fecha repetida.</param>
        /// <param name="nombresVacas">Vacas que ya tenían producción ese día.</param>
        public ProduccionDuplicadaException(DateOnly fecha, IEnumerable<string> nombresVacas)
            : base($"Ya hay producción registrada el {fecha:yyyy-MM-dd} para: {string.Join(", ", nombresVacas)}. " +
                   "Cada vaca se registra una sola vez por día (RN-01).")
        {
        }
    }
}
