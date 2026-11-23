using BaityMu.Domain.Entities;
using BaityMu.Domain.Enums;

namespace BaityMu.Services.Modelos
{
    /// <summary>Un lote con su estado en la fecha de la consulta (HU-10).</summary>
    /// <param name="Lote">El lote.</param>
    /// <param name="Estado">Disponible, Agotado, Vencido o Dañado.</param>
    public sealed record LoteConEstado(LoteProducto Lote, EstadoLote Estado);

    /// <summary>Un lote que pasó a dañado al procesar vencimientos (HU-11).</summary>
    /// <param name="Lote">El lote, ya con su cantidad dañada.</param>
    /// <param name="CantidadDañada">Lo que se dañó en este proceso.</param>
    public sealed record LoteDañado(LoteProducto Lote, decimal CantidadDañada);

    /// <summary>Resultado de procesar vencimientos en una fecha (HU-11, criterio 5).</summary>
    /// <param name="Fecha">Día del proceso.</param>
    /// <param name="Lotes">Lotes afectados. Vacío si no había nada vencido.</param>
    public sealed record ResultadoVencimientos(DateOnly Fecha, IReadOnlyList<LoteDañado> Lotes)
    {
        /// <summary>Total dañado en este proceso, por producto. Siempre trae queso y yogurt.</summary>
        public IReadOnlyList<TotalDañado> TotalesPorProducto =>
            Enum.GetValues<TipoProducto>()
                .Select(tipo => new TotalDañado(
                    tipo,
                    Lotes.Where(l => l.Lote.TipoProducto == tipo).Sum(l => l.CantidadDañada)))
                .ToList();
    }

    /// <summary>Cantidad dañada de un producto en un proceso de vencimientos.</summary>
    public sealed record TotalDañado(TipoProducto TipoProducto, decimal CantidadDañada);

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
