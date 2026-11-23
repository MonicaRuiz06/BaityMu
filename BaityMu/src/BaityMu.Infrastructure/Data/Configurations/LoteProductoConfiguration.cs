using BaityMu.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BaityMu.Infrastructure.Data.Configurations
{
    /// <summary>
    /// Configura cómo se guarda <see cref="LoteProducto"/> en la tabla LotesProducto.
    /// Las restricciones CHECK repiten en la base de datos las reglas del dominio.
    /// </summary>
    public sealed class LoteProductoConfiguration : IEntityTypeConfiguration<LoteProducto>
    {
        public void Configure(EntityTypeBuilder<LoteProducto> builder)
        {
            builder.ToTable("LotesProducto", t =>
            {
                t.HasCheckConstraint("CK_LotesProducto_FechaVencimiento", "[FechaVencimiento] >= [FechaProduccion]");
                t.HasCheckConstraint("CK_LotesProducto_LitrosUsados", "[LitrosUsados] > 0");
                t.HasCheckConstraint("CK_LotesProducto_CantidadInicial", "[CantidadInicial] > 0");
                t.HasCheckConstraint("CK_LotesProducto_CantidadDisponible", "[CantidadDisponible] >= 0");
                t.HasCheckConstraint("CK_LotesProducto_CantidadDanada", "[CantidadDanada] >= 0");
                t.HasCheckConstraint("CK_LotesProducto_Cantidades",
                    "[CantidadDisponible] + [CantidadDanada] <= [CantidadInicial]");
            });

            builder.HasKey(l => l.Id);

            builder.Property(l => l.TipoProducto)
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.Property(l => l.LitrosUsados).HasPrecision(10, 2);
            builder.Property(l => l.CantidadInicial).HasPrecision(12, 3);
            builder.Property(l => l.CantidadDisponible).HasPrecision(12, 3);
            builder.Property(l => l.CantidadDanada).HasPrecision(12, 3);

            // Orden FIFO de la venta (RN-06): por producto, del lote más antiguo al más
            // nuevo. IncludeProperties agrega al índice las columnas que la venta lee, así
            // SQL Server no tiene que ir a buscarlas a la tabla.
            builder.HasIndex(l => new { l.TipoProducto, l.FechaProduccion, l.Id })
                .HasDatabaseName("IX_LotesProducto_FIFO")
                .IncludeProperties(l => new { l.CantidadDisponible, l.FechaVencimiento });

            // Cuánta leche de un día ya se transformó (RN-02).
            builder.HasIndex(l => l.FechaProduccion);

            // Procesar vencimientos y reporte de producto dañado (HU-11, HU-19).
            builder.HasIndex(l => l.FechaVencimiento);
        }
    }
}
