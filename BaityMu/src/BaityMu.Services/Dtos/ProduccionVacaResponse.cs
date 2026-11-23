namespace BaityMu.Services.Dtos
{
    /// <summary>Un día del historial de producción de una vaca.</summary>
    public sealed class ProduccionVacaResponse
    {
        /// <summary>Día del ordeño (yyyy-MM-dd).</summary>
        public DateOnly Fecha { get; set; }

        /// <summary>Litros que dio la vaca ese día.</summary>
        public decimal Litros { get; set; }
    }
}
