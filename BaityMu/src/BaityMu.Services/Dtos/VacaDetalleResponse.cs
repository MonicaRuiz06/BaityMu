using System.Text.Json.Serialization;

namespace BaityMu.Services.Dtos
{
    /// <summary>
    /// Vaca con su historial de producción (GET /api/vacas/{id}). Tiene todo lo de
    /// <see cref="VacaResponse"/> y además los litros de cada día.
    /// </summary>
    /// <remarks>
    /// Hereda de VacaResponse para no repetir Id, Nombre, Raza, Activa, FechaIngreso y
    /// LitrosProducidos.
    /// </remarks>
    public sealed class VacaDetalleResponse : VacaResponse
    {
        /// <summary>Los ordeños de la vaca, del más antiguo al más reciente.</summary>
        /// <remarks>JsonPropertyOrder(1) lo deja al final del JSON, después de los datos de la vaca.</remarks>
        [JsonPropertyOrder(1)]
        public List<ProduccionVacaResponse> Historial { get; set; } = new();
    }
}
