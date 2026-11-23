using BaityMu.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BaityMu.Infrastructure.Data
{
    /// <summary>
    /// Contexto de Entity Framework Core de BaityMu: la conexión entre el código
    /// y la base de datos SQL Server.
    /// </summary>
    /// <remarks>
    /// ¿Qué es un DbContext?
    /// La clase central de Entity Framework. Representa una sesión con la base de datos:
    /// - Declara las tablas con propiedades DbSet&lt;T&gt; (DbSet&lt;Vaca&gt;, DbSet&lt;Venta&gt;...).
    /// - Lleva la cuenta de lo que se agrega o modifica (change tracking).
    /// - Guarda todos esos cambios juntos, en una sola transacción, con SaveChangesAsync.
    ///
    /// Las tablas se agregan con cada historia de usuario; la HU-01 agregó Granjas y Vacas.
    ///
    /// Solo la capa Infrastructure lo conoce. Los controllers nunca lo usan (RNF-02);
    /// los servicios trabajan con repositorios e IUnitOfWork.
    /// </remarks>
    public class BaityMuDbContext : DbContext
    {
        /// <summary>Tabla Granjas. Solo tiene una fila: la granja de Baity.</summary>
        public DbSet<Granja> Granjas => Set<Granja>();

        /// <summary>Tabla Vacas: el hato. Las vacas se desactivan, nunca se borran (RNF-11).</summary>
        public DbSet<Vaca> Vacas => Set<Vaca>();

        /// <summary>Tabla ProduccionesLeche: los litros que dio cada vaca cada día.</summary>
        public DbSet<ProduccionLeche> ProduccionesLeche => Set<ProduccionLeche>();

        /// <summary>
        /// Crea el contexto con las opciones que recibe desde afuera.
        /// </summary>
        /// <param name="options">Qué motor usar y cómo conectarse. En la aplicación es
        /// SQL Server con la cadena de conexión de LocalDB; en las pruebas es una base
        /// InMemory. El DbContext no decide: se lo inyectan (inyección de dependencias).</param>
        public BaityMuDbContext(DbContextOptions<BaityMuDbContext> options) : base(options)
        {
        }

        /// <summary>
        /// Configura el modelo: tablas, columnas, llaves, índices y restricciones.
        /// </summary>
        /// <param name="modelBuilder">Herramienta de EF Core para describir el modelo.</param>
        /// <remarks>
        /// ApplyConfigurationsFromAssembly busca en este proyecto todas las clases que
        /// implementan IEntityTypeConfiguration&lt;T&gt; y las aplica. Así, cada entidad
        /// tiene su configuración en un archivo propio (por ejemplo VacaConfiguration)
        /// y no hay que registrarla aquí a mano.
        /// </remarks>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(BaityMuDbContext).Assembly);
        }
    }
}
