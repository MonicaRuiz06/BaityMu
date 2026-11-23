using BaityMu.Domain.Entities;

namespace BaityMu.Services.Modelos
{
    /// <summary>
    /// Una vaca junto con los litros que ha producido en total. Es lo que devuelve el
    /// servicio en el listado (HU-02); el controller lo convierte en VacaResponse.
    /// </summary>
    /// <param name="Vaca">La vaca.</param>
    /// <param name="LitrosProducidos">Suma de toda su producción. 0 si nunca se ha ordeñado.</param>
    /// <remarks>
    /// ¿Qué es un record?
    /// Una clase corta para datos que no cambian. Esta línea crea las dos propiedades,
    /// el constructor, Equals y ToString, sin escribirlos a mano.
    /// </remarks>
    public sealed record VacaConLitros(Vaca Vaca, decimal LitrosProducidos);
}
