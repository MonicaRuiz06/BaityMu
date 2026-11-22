using BaityMu.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BaityMu.Infrastructure.Data.Configurations
{
    /// <summary>
    /// Configura cómo se guarda <see cref="Vaca"/> en la tabla Vacas. El nombre es único
    /// dentro de la granja, y el índice (GranjaId, Activa) acelera el cálculo de n.
    /// </summary>
    public sealed class VacaConfiguration : IEntityTypeConfiguration<Vaca>
    {
        public void Configure(EntityTypeBuilder<Vaca> builder)
        {
            builder.ToTable("Vacas");

            builder.HasKey(v => v.Id);

            builder.Property(v => v.Nombre)
                .HasMaxLength(60)
                .IsRequired();

            builder.Property(v => v.Raza)
                .HasMaxLength(60)
                .IsRequired();

            builder.HasIndex(v => new { v.GranjaId, v.Nombre }).IsUnique();

            builder.HasIndex(v => new { v.GranjaId, v.Activa });
        }
    }
}
