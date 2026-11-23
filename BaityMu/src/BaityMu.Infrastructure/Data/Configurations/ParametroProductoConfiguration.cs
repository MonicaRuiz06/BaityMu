using BaityMu.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BaityMu.Infrastructure.Data.Configurations
{
    /// <summary>
    /// Configura cómo se guarda <see cref="ParametroProducto"/> en la tabla ParametrosProducto.
    /// </summary>
    /// <remarks>
    /// HasConversion&lt;string&gt;() guarda el enum como texto ("Queso", "Yogurt") en vez de
    /// número: la tabla se entiende al leerla en SQL Server.
    /// </remarks>
    public sealed class ParametroProductoConfiguration : IEntityTypeConfiguration<ParametroProducto>
    {
        public void Configure(EntityTypeBuilder<ParametroProducto> builder)
        {
            builder.ToTable("ParametrosProducto", t =>
            {
                t.HasCheckConstraint("CK_ParametrosProducto_LitrosPorUnidad", "[LitrosPorUnidad] > 0");
                t.HasCheckConstraint("CK_ParametrosProducto_VidaUtilDias", "[VidaUtilDias] > 0");
                t.HasCheckConstraint("CK_ParametrosProducto_PrecioUnitario", "[PrecioUnitario] >= 0");
            });

            builder.HasKey(p => p.Id);

            builder.Property(p => p.TipoProducto)
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.Property(p => p.Unidad)
                .HasMaxLength(10)
                .IsRequired();

            builder.Property(p => p.LitrosPorUnidad)
                .HasPrecision(10, 3);

            builder.Property(p => p.PrecioUnitario)
                .HasPrecision(18, 2);

            // Una granja tiene un solo parámetro por producto.
            builder.HasIndex(p => new { p.GranjaId, p.TipoProducto }).IsUnique();
        }
    }
}
