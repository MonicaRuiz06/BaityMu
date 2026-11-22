using BaityMu.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace BaityMu.Web.Middleware
{
    /// <summary>
    /// Manejador global de errores de BaityMu: atrapa cualquier excepción que no se
    /// atrapó antes y la convierte en una respuesta ProblemDetails con el código HTTP
    /// que le corresponde (RNF-06).
    /// </summary>
    /// <remarks>
    /// ¿Cómo sabe qué código devolver?
    /// Por el TIPO de la excepción, usando la jerarquía de BaityMu.Domain.Exceptions:
    /// - ReglaDeNegocioException       → 400 Bad Request
    /// - RecursoNoEncontradoException  → 404 Not Found
    /// - ConflictoException            → 409 Conflict
    /// - BadHttpRequestException       → 400 (JSON mal formado, parámetros inválidos)
    /// - Cualquier otra (un bug)       → 500 Internal Server Error
    ///
    /// Como el switch pregunta por la clase base, las excepciones hijas que se creen en
    /// cada historia heredan su código sin tocar este archivo. Por ejemplo,
    /// InventarioInsuficienteException : ConflictoException responde 409 automáticamente.
    ///
    /// Gracias a este manejador:
    /// - Los controllers no necesitan try/catch.
    /// - Todos los errores responden con el mismo formato JSON (ProblemDetails, RFC 9457).
    /// - Los servicios no saben nada de HTTP: solo lanzan la excepción del negocio.
    ///
    /// Se registra en Program.cs con AddExceptionHandler y se activa con UseExceptionHandler.
    /// </remarks>
    public sealed class ManejadorGlobalDeErrores : IExceptionHandler
    {
        /// <summary>Escribe la respuesta ProblemDetails en el formato estándar de ASP.NET Core.</summary>
        private readonly IProblemDetailsService _problemDetails;

        /// <summary>Registra en el log los errores inesperados (500) para poder depurarlos.</summary>
        private readonly ILogger<ManejadorGlobalDeErrores> _logger;

        /// <summary>
        /// Crea el manejador. Ambas dependencias las entrega el contenedor de inyección
        /// de dependencias de .NET.
        /// </summary>
        /// <param name="problemDetails">Servicio de ASP.NET Core que escribe ProblemDetails.</param>
        /// <param name="logger">Logger de la aplicación.</param>
        public ManejadorGlobalDeErrores(IProblemDetailsService problemDetails, ILogger<ManejadorGlobalDeErrores> logger)
        {
            _problemDetails = problemDetails;
            _logger = logger;
        }

        /// <summary>
        /// Intenta manejar la excepción: decide el código HTTP, arma el ProblemDetails
        /// y lo escribe en la respuesta.
        /// </summary>
        /// <param name="httpContext">La petición y respuesta HTTP actuales.</param>
        /// <param name="exception">La excepción que se lanzó y nadie atrapó.</param>
        /// <param name="cancellationToken">Permite cancelar si el cliente cierra la conexión.</param>
        /// <returns>true si la respuesta se escribió; así ASP.NET Core no hace nada más.</returns>
        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            var (codigo, titulo) = exception switch
            {
                ReglaDeNegocioException => (StatusCodes.Status400BadRequest, "La solicitud no cumple una regla de negocio"),
                RecursoNoEncontradoException => (StatusCodes.Status404NotFound, "Recurso no encontrado"),
                ConflictoException => (StatusCodes.Status409Conflict, "Conflicto con el estado de la granja"),
                BadHttpRequestException => (StatusCodes.Status400BadRequest, "Solicitud mal formada"),
                _ => (StatusCodes.Status500InternalServerError, "Error interno del servidor")
            };

            // Los errores del negocio son esperados: no hace falta registrarlos como error.
            // Un 500 significa un bug, así que se guarda en el log con todo el detalle.
            if (codigo == StatusCodes.Status500InternalServerError)
            {
                _logger.LogError(exception, "Error no controlado en {Metodo} {Ruta}",
                    httpContext.Request.Method, httpContext.Request.Path);
            }

            var problema = new ProblemDetails
            {
                Status = codigo,
                Title = titulo,
                // En un 500 no se muestra el mensaje interno: podría revelar detalles del
                // servidor. En los errores del negocio el mensaje sí es útil para el cliente.
                Detail = codigo == StatusCodes.Status500InternalServerError
                    ? "Ocurrió un error inesperado en el establo de BaityMu. Muuu..."
                    : exception.Message,
                Instance = httpContext.Request.Path
            };

            httpContext.Response.StatusCode = codigo;

            return await _problemDetails.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = problema,
                Exception = exception
            });
        }
    }
}
