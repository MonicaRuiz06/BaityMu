using BaityMu.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BaityMu.Infrastructure.Data.Configurations
{
    /// <summary>
    /// Configura cómo se guarda <see cref="ProduccionLeche"/> en la tabla ProduccionesLeche.
    /// El índice único (VacaId, Fecha) garantiza desde la base de datos que una vaca no
    /// tenga dos registros el mismo día (RN-01).
    /// </summary>
    public sealed class ProduccionLecheConfiguration : IEntityTypeConfiguration<ProduccionLeche>
    {
        public void Configure(EntityTypeBuilder<ProduccionLeche> builder)
        {
            builder.ToTable("ProduccionesLeche", t =>
                t.HasCheckConstraint("CK_ProduccionesLeche_Litros", "[Litros] >= 0"));

            builder.HasKey(p => p.Id);

            builder.Property(p => p.Litros)
                .HasPrecision(10, 2);

            // Cada producción pertenece a una vaca. Restrict: una vaca con historial no
            // se puede borrar (de todos modos nunca se borran, RNF-11).
            builder.HasOne<Vaca>()
                .WithMany()
                .HasForeignKey(p => p.VacaId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(p => new { p.VacaId, p.Fecha }).IsUnique();

            builder.HasIndex(p => p.Fecha);
        }
    }
}
