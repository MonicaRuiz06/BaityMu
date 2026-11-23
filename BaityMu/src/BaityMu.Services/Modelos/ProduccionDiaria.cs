namespace BaityMu.Services.Modelos
{
    /// <summary>
    /// La producción de un día: lo que dio cada vaca y el total. El controller lo convierte
    /// en ProduccionDiariaResponse.
    /// </summary>
    /// <param name="Fecha">Día del ordeño.</param>
    /// <param name="Vacas">Lo que dio cada vaca, ordenado por Id.</param>
    public sealed record ProduccionDiaria(DateOnly Fecha, IReadOnlyList<ProduccionDeVaca> Vacas)
    {
        /// <summary>Total del día. En el escenario de referencia: 10 × 15 = 150 L.</summary>
        public decimal TotalLitros => Vacas.Sum(v => v.Litros);
    }

    /// <summary>Litros que dio una vaca en el día.</summary>
    /// <param name="VacaId">Id de la vaca.</param>
    /// <param name="Nombre">Nombre de la vaca.</param>
    /// <param name="Litros">Litros que dio.</param>
    public sealed record ProduccionDeVaca(int VacaId, string Nombre, decimal Litros);
}
