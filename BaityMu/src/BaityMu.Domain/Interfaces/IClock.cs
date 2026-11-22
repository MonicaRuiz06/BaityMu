namespace BaityMu.Domain.Interfaces
{

    /// <summary>
    /// Reloj de la aplicación: dice qué día es "hoy".
    /// </summary>
    /// <remarks>
    /// ¿Qué es una interfaz?
    /// Un contrato: dice QUÉ se puede hacer, pero no CÓMO. Cualquier clase que la
    /// implemente está obligada a tener una propiedad Today.
    ///
    /// ¿Para qué la uso en BaityMu? (RN-10)
    /// Toda operación acepta una fecha opcional; si no llega, se usa IClock.Today.
    /// - En la aplicación real, SystemClock (Infrastructure) devuelve la fecha del sistema.
    /// - En las pruebas, un FakeClock devuelve la fecha que yo quiera, para simular el
    ///   escenario del taller del día 1 al día 17 sin esperar 17 días de verdad.
    ///
    /// Si los servicios usaran DateTime.Today directamente, no se podría probar el
    /// vencimiento de un lote ni la venta FIFO en días distintos.
    /// </remarks>
    public interface IClock
    {

        /// <summary>
        /// Fecha de hoy, sin hora (DateOnly), porque el negocio trabaja por días.
        /// </summary>
        DateOnly Today { get; }
    }
}
