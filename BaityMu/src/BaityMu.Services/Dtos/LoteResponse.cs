using BaityMu.Domain.Enums;

namespace BaityMu.Services.Dtos
{
    /// <summary>Lote de producto tal como lo devuelve la API.</summary>
    public sealed class LoteResponse
    {
        /// <summary>Id del lote.</summary>
        public int Id { get; set; }

        /// <summary>Queso o Yogurt.</summary>
        public TipoProducto TipoProducto { get; set; }

        /// <summary>Fecha de la leche que se usó.</summary>
        public DateOnly FechaProduccion { get; set; }

        /// <summary>Último día en que se puede vender.</summary>
        public DateOnly FechaVencimiento { get; set; }

        /// <summary>Litros de leche que se usaron.</summary>
        public decimal LitrosUsados { get; set; }

        /// <summary>Cantidad con la que nació el lote (kg o L).</summary>
        public decimal CantidadInicial { get; set; }

        /// <summary>Cantidad que queda para vender.</summary>
        public decimal CantidadDisponible { get; set; }

        /// <summary>Cantidad que se perdió por vencimiento.</summary>
        public decimal CantidadDanada { get; set; }

        /// <summary>Disponible, Agotado, Vencido o Dañado, según la fecha de la consulta.</summary>
        public EstadoLote Estado { get; set; }
    }
}
