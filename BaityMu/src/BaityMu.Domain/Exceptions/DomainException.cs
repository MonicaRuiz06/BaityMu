namespace BaityMu.Domain.Exceptions
{

    /// <summary>
    /// Clase base de todos los errores del negocio de BaityMu.
    /// </summary>
    /// <remarks>
    /// ¿Qué es una excepción?
    /// Un objeto que representa un error. Cuando el código hace "throw", la ejecución
    /// se detiene y el error sube hasta que alguien lo atrapa (catch).
    ///
    /// ¿Por qué heredar de Exception?
    /// Así C# la trata como cualquier excepción: se puede lanzar con throw, trae el
    /// mensaje (Message) y la traza de dónde ocurrió (StackTrace).
    ///
    /// ¿Por qué es abstract?
    /// Nadie lanza un "error de dominio" genérico. Siempre se lanza una de sus hijas:
    /// - ReglaDeNegocioException      → HTTP 400 (datos inválidos)
    /// - RecursoNoEncontradoException → HTTP 404 (no existe)
    /// - ConflictoException           → HTTP 409 (choca con el estado de la granja)
    ///
    /// ¿Para qué la uso en BaityMu?
    /// El middleware de errores de la capa Web atrapa cualquier DomainException y la
    /// convierte en una respuesta ProblemDetails con el código HTTP de su tipo. Así los
    /// controllers no necesitan try/catch y todos los errores responden igual (RNF-06).
    /// </remarks>
    public abstract class DomainException : Exception
    {

        /// <summary>
        /// Crea el error con un mensaje que explica qué pasó.
        /// </summary>
        /// <param name="message">Texto que verá quien consume la API, por ejemplo
        /// "No existe una vaca con id 99".</param>
        /// <remarks>
        /// Es protected porque solo las clases hijas pueden llamarlo.
        /// ": base(message)" le pasa el mensaje al constructor de Exception, que es el
        /// único que puede guardarlo en la propiedad Message (de solo lectura).
        /// </remarks>
        protected DomainException(string message) : base(message)
        {
        }
    }
}
