namespace BaityMu.Services.Dtos
{
    /// <summary>Producción de leche de un día.</summary>
    public sealed class ProduccionDiariaResponse
    {
        /// <summary>Día del ordeño.</summary>
        public DateOnly Fecha { get; set; }

        /// <summary>Litros de leche del día: la suma de todas las vacas.</summary>
        public decimal TotalLitros { get; set; }

        /// <summary>Lo que dio cada vaca.</summary>
        public List<ProduccionVacaDiaResponse> Vacas { get; set; } = new();
    }

    /// <summary>Litros que dio una vaca en el día.</summary>
    public sealed class ProduccionVacaDiaResponse
    {
        /// <summary>Id de la vaca.</summary>
        public int VacaId { get; set; }

        /// <summary>Nombre de la vaca.</summary>
        public string Nombre { get; set; } = string.Empty;

        /// <summary>Litros que dio.</summary>
        public decimal Litros { get; set; }
    }
}
