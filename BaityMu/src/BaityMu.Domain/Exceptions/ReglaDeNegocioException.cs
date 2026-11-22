namespace BaityMu.Domain.Exceptions
{

    /// <summary>
    /// Error que se lanza cuando los datos no cumplen una regla de negocio.
    /// Se responde con HTTP 400 (Bad Request).
    /// </summary>
    /// <remarks>
    /// Significa: "lo que me pediste no es válido". El cliente debe corregir los datos
    /// y volver a intentarlo.
    ///
    /// Ejemplos en BaityMu:
    /// - Registrar una vaca sin raza (HU-01).
    /// - Configurar los litros por vaca (l) en cero o negativos (HU-06).
    /// - Transformar o vender una cantidad menor o igual a cero (HU-09, HU-13).
    /// - Pedir un reporte con "desde" posterior a "hasta" (Épica 6).
    /// </remarks>
    public class ReglaDeNegocioException : DomainException
    {

        /// <summary>
        /// Crea el error con el motivo por el que los datos no son válidos.
        /// </summary>
        /// <param name="message">Por ejemplo: "La raza de la vaca es obligatoria."</param>
        public ReglaDeNegocioException(string message) : base(message)
        {
        }
    }
}
