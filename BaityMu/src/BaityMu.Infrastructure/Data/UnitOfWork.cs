using BaityMu.Domain.Interfaces;


namespace BaityMu.Infrastructure.Data
{

    /// <summary>
    /// Implementación de IUnitOfWork con Entity Framework Core.
    /// </summary>
    /// <remarks>
    /// Recibe el BaityMuDbContext por el constructor (inyección de dependencias) y,
    /// al guardar, delega en el SaveChangesAsync del DbContext. EF Core envía todos los
    /// cambios pendientes a SQL Server en una sola transacción: o se guarda todo, o nada.
    ///
    /// Los servicios dependen de IUnitOfWork (Domain), no de esta clase ni de EF Core.
    /// Así la lógica de negocio no sabe qué tecnología guarda los datos.
    ///
    /// El repositorio y el UnitOfWork de una misma petición comparten el mismo DbContext,
    /// por eso lo que agregan los repositorios es lo que este SaveChangesAsync guarda.
    /// </remarks>
    public sealed class UnitOfWork : IUnitOfWork
    {
        /// <summary>El contexto de EF Core que lleva la cuenta de los cambios pendientes.</summary>
        private readonly BaityMuDbContext _db;

        /// <summary>Crea la unidad de trabajo con el DbContext de la petición actual.</summary>
        /// <param name="db">Lo entrega el contenedor de inyección de dependencias de .NET.</param>
        public UnitOfWork(BaityMuDbContext db)
        {
            _db = db;
        }

        /// <inheritdoc />
        public Task<int> SaveChangesAsync(CancellationToken ct = default)
            => _db.SaveChangesAsync(ct);
    }
}
