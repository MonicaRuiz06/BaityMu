using BaityMu.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BaityMu.Infrastructure.Data.Configurations
{
    /// <summary>
    /// Configura cómo se guarda <see cref="Granja"/> en la tabla Granjas.
    /// n (NumeroVacas) y n × l (ProduccionDiariaEsperada) no se guardan: se calculan.
    /// </summary>
    public sealed class GranjaConfiguration : IEntityTypeConfiguration<Granja>
    {
        public void Configure(EntityTypeBuilder<Granja> builder)
        {
            builder.ToTable("Granjas", t =>
                t.HasCheckConstraint("CK_Granjas_LitrosPorVaca", "[LitrosPorVaca] > 0"));

            builder.HasKey(g => g.Id);

            builder.Property(g => g.Nombre)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(g => g.LitrosPorVaca)
                .HasPrecision(10, 2);

            builder.Ignore(g => g.NumeroVacas);
            builder.Ignore(g => g.ProduccionDiariaEsperada);

            builder.HasMany(g => g.Vacas)
                .WithOne()
                .HasForeignKey(v => v.GranjaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Navigation(g => g.Vacas)
                .UsePropertyAccessMode(PropertyAccessMode.Field);

            // Los parámetros de producto son parte de la granja: si se borrara la granja,
            // se borran con ella (Cascade). Las vacas no, porque tienen historial.
            builder.HasMany(g => g.Productos)
                .WithOne()
                .HasForeignKey(p => p.GranjaId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Navigation(g => g.Productos)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        }
    }
}