using BaityMu.Domain.Entities;
using BaityMu.Domain.Enums;

namespace BaityMu.Services.Modelos
{
    /// <summary>Un lote con su estado en la fecha de la consulta (HU-10).</summary>
    /// <param name="Lote">El lote.</param>
    /// <param name="Estado">Disponible, Agotado, Vencido o Dañado.</param>
    public sealed record LoteConEstado(LoteProducto Lote, EstadoLote Estado);

    /// <summary>Resumen del inventario de un producto en una fecha (HU-10, criterio 5).</summary>
    /// <param name="TipoProducto">Queso o Yogurt.</param>
    /// <param name="CantidadVendible">Existencias que no han vencido: lo que se puede vender hoy.</param>
    /// <param name="CantidadVencidaSinProcesar">Existencias vencidas que todavía no pasan a dañado (HU-11).</param>
    /// <param name="CantidadDañada">Lo que ya se perdió por vencimiento.</param>
    public sealed record ResumenInventario(
        TipoProducto TipoProducto,
        decimal CantidadVendible,
        decimal CantidadVencidaSinProcesar,
        decimal CantidadDañada);
}
