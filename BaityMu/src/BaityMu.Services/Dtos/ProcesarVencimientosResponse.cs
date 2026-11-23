using BaityMu.Domain.Enums;

namespace BaityMu.Services.Dtos
{
    /// <summary>Resultado de procesar vencimientos (POST /api/inventario/procesar-vencimientos).</summary>
    public sealed class ProcesarVencimientosResponse
    {
        /// <summary>Día del proceso.</summary>
        public DateOnly Fecha { get; set; }

        /// <summary>Lotes que pasaron a dañado. Vacío si no había nada vencido.</summary>
        public List<LoteDañadoResponse> Lotes { get; set; } = new();

        /// <summary>Total dañado en este proceso, por producto.</summary>
        public List<TotalDañadoResponse> TotalesPorProducto { get; set; } = new();
    }

    /// <summary>Un lote que pasó a dañado.</summary>
    public sealed class LoteDañadoResponse
    {
        /// <summary>Id del lote.</summary>
        public int LoteId { get; set; }

        /// <summary>Queso o Yogurt.</summary>
        public TipoProducto TipoProducto { get; set; }

        /// <summary>Fecha de vencimiento del lote.</summary>
        public DateOnly FechaVencimiento { get; set; }

        /// <summary>Cantidad que se dañó en este proceso.</summary>
        public decimal CantidadDañada { get; set; }
    }

    /// <summary>Total dañado de un producto.</summary>
    public sealed class TotalDañadoResponse
    {
        /// <summary>Queso o Yogurt.</summary>
        public TipoProducto TipoProducto { get; set; }

        /// <summary>Cantidad dañada.</summary>
        public decimal CantidadDañada { get; set; }
    }
}
