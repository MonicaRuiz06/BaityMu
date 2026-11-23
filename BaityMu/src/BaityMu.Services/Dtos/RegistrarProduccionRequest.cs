namespace BaityMu.Services.Dtos
{
    /// <summary>
    /// Datos para registrar la producción de un día (POST /api/produccion, HU-07).
    /// Es lo que envía BaityGanadero.
    /// </summary>
    public sealed class RegistrarProduccionRequest
    {
        /// <summary>Fecha del ordeño (yyyy-MM-dd). Si no se envía, se usa la fecha de hoy (RN-10).</summary>
        /// <example>2026-12-01</example>
        public DateOnly? Fecha { get; set; }

        /// <summary>
        /// Litros de algunas vacas. Si se deja vacío, cada vaca activa da l litros (n × l).
        /// Si trae datos, solo se registran esas vacas con esos litros.
        /// </summary>
        public List<ProduccionVacaRequest> Vacas { get; set; } = new();
    }

    /// <summary>Litros de una vaca en el día.</summary>
    public sealed class ProduccionVacaRequest
    {
        /// <summary>Id de la vaca. Debe existir y estar activa.</summary>
        /// <example>1</example>
        public int VacaId { get; set; }

        /// <summary>Litros que dio. 0 o más.</summary>
        /// <example>14.5</example>
        public decimal Litros { get; set; }
    }
}
