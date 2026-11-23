using BaityMu.Domain.Enums;

namespace BaityMu.Services.Dtos
{
    /// <summary>
    /// Datos para transformar leche de un día en un producto (POST /api/transformaciones,
    /// HU-09). Es lo que envía BaityQuesero.
    /// </summary>
    public sealed class TransformarLecheRequest
    {
        /// <summary>Fecha de la leche que se usa (yyyy-MM-dd). Si no se envía, la de hoy.</summary>
        /// <example>2026-12-01</example>
        public DateOnly? Fecha { get; set; }

        /// <summary>Un solo producto: "Queso" o "Yogurt" (RN-02).</summary>
        /// <example>Queso</example>
        public TipoProducto TipoProducto { get; set; }

        /// <summary>Litros de leche a transformar. Mayores que 0.</summary>
        /// <example>100</example>
        public decimal Litros { get; set; }
    }
}
