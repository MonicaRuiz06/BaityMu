namespace BaityMu.Services.Dtos
{
    /// <summary>
    /// Datos para registrar una vaca (POST /api/vacas). Es lo que envía BaityGanadero.
    /// </summary>
    /// <remarks>
    /// ¿Qué es un DTO?
    /// Un "objeto de transferencia de datos": una clase simple, solo con propiedades, que
    /// viaja como JSON entre el cliente y la API. Así la API nunca expone las entidades del
    /// dominio directamente (RNF-08).
    /// </remarks>
    public sealed class CrearVacaRequest
    {
        /// <summary>Raza de la vaca, por ejemplo "Holstein" o "Jersey". Es obligatoria.</summary>
        /// <example>Holstein</example>
        public string Raza { get; set; } = string.Empty;

        /// <summary>
        /// Nombre opcional. Si no se envía, la primera vaca se llama "BaityCow", la
        /// segunda "BaityCow 2", y así sucesivamente.
        /// </summary>
        /// <example>Lola</example>
        public string? Nombre { get; set; }

        /// <summary>Fecha de ingreso (yyyy-MM-dd). Si no se envía, se usa la fecha de hoy.</summary>
        /// <example>2026-12-01</example>
        public DateOnly? FechaIngreso { get; set; }
    }
}
