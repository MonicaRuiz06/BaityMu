using BaityMu.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BaityMu.Infrastructure.Data.Configurations
{
    /// <summary>Configura las tablas Ventas y VentaDetalles (HU-13, RN-09).</summary>
    public sealed class VentaConfiguration : IEntityTypeConfiguration<Venta>
    {
        public void Configure(EntityTypeBuilder<Venta> builder)
        {
            builder.ToTable("Ventas", t =>
            {
                t.HasCheckConstraint("CK_Ventas_Cantidad", "[Cantidad] > 0");
                t.HasCheckConstraint("CK_Ventas_PrecioUnitario", "[PrecioUnitario] >= 0");
                t.HasCheckConstraint("CK_Ventas_Total", "[Total] >= 0");
            });

            builder.HasKey(v => v.Id);

            builder.Property(v => v.TipoProducto).HasConversion<string>().HasMaxLength(20);
            builder.Property(v => v.Cantidad).HasPrecision(12, 3);
            builder.Property(v => v.PrecioUnitario).HasPrecision(18, 2);
            builder.Property(v => v.Total).HasPrecision(18, 2);

            // Los detalles son parte de la venta: se borran con ella (Cascade).
            builder.HasMany(v => v.Detalles)
                .WithOne()
                .HasForeignKey(d => d.VentaId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Navigation(v => v.Detalles).UsePropertyAccessMode(PropertyAccessMode.Field);

            // Reporte de ventas (HU-15).
            builder.HasIndex(v => new { v.Fecha, v.TipoProducto });
        }
    }

    /// <summary>Configura VentaDetalles: una línea por lote de cada venta.</summary>
    public sealed class VentaDetalleConfiguration : IEntityTypeConfiguration<VentaDetalle>
    {
        public void Configure(EntityTypeBuilder<VentaDetalle> builder)
        {
            builder.ToTable("VentaDetalles", t =>
                t.HasCheckConstraint("CK_VentaDetalles_Cantidad", "[Cantidad] > 0"));

            builder.HasKey(d => d.Id);
            builder.Property(d => d.Cantidad).HasPrecision(12, 3);

            // NO ACTION: no se puede borrar un lote que ya se vendió.
            builder.HasOne(d => d.Lote)
                .WithMany()
                .HasForeignKey(d => d.LoteId)
                .OnDelete(DeleteBehavior.Restrict);

            // Una venta toma de cada lote una sola vez.
            builder.HasIndex(d => new { d.VentaId, d.LoteId }).IsUnique();
        }
    }
}
