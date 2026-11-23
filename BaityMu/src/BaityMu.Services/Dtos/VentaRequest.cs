using BaityMu.Domain.Enums;

namespace BaityMu.Services.Dtos
{
    /// <summary>Datos para registrar una venta (POST /api/ventas, HU-13). Lo envía BaityVendedor.</summary>
    public sealed class VentaRequest
    {
        /// <summary>Día de la venta (yyyy-MM-dd). Si no se envía, la de hoy.</summary>
        /// <example>2026-12-03</example>
        public DateOnly? Fecha { get; set; }

        /// <summary>"Queso" o "Yogurt".</summary>
        /// <example>Queso</example>
        public TipoProducto TipoProducto { get; set; }

        /// <summary>Cantidad a vender (kg o L). Mayor que 0.</summary>
        /// <example>12</example>
        public decimal Cantidad { get; set; }
    }
}
