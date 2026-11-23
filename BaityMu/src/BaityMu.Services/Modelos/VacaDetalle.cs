using BaityMu.Domain.Entities;

namespace BaityMu.Services.Modelos
{
    /// <summary>
    /// Una vaca con todo su historial de producción (HU-03). El controller lo convierte
    /// en VacaDetalleResponse.
    /// </summary>
    /// <param name="Vaca">La vaca.</param>
    /// <param name="Historial">Sus ordeños, del más antiguo al más reciente.</param>
    public sealed record VacaDetalle(Vaca Vaca, IReadOnlyList<ProduccionLeche> Historial)
    {
        /// <summary>Litros totales: la suma de todo el historial. 0 si nunca se ordeñó.</summary>
        public decimal LitrosProducidos => Historial.Sum(p => p.Litros);
    }
}
