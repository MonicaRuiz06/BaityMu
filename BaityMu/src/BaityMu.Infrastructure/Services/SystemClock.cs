using BaityMu.Domain.Interfaces;

namespace BaityMu.Infrastructure.Services
{

    /// <summary>
    /// Reloj real de la aplicación: implementa IClock con la fecha del computador.
    /// </summary>
    /// <remarks>
    /// Es la implementación que usa la API cuando funciona de verdad. En las pruebas
    /// se reemplaza por un FakeClock que devuelve la fecha que se necesite (RN-10).
    ///
    /// Está en Infrastructure porque depende del sistema (la hora del computador).
    /// Domain solo conoce la interfaz IClock, no esta clase.
    ///
    /// Es sealed porque no está pensada para que otra clase herede de ella.
    /// </remarks>
    public sealed class SystemClock : IClock
    {

        /// <summary>
        /// Fecha de hoy según el computador, sin hora.
        /// </summary>
        /// <remarks>
        /// DateTime.Now trae fecha y hora; DateOnly.FromDateTime descarta la hora.
        /// </remarks>
        public DateOnly Today => DateOnly.FromDateTime(DateTime.Now);
    }
}
