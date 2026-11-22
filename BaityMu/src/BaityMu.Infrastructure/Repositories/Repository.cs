using BaityMu.Domain.Interfaces;
using BaityMu.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BaityMu.Infrastructure.Repositories
{
    /// <summary>
    /// Implementación genérica de <see cref="IRepository{T}"/> con EF Core. Los
    /// repositorios específicos (GranjaRepository...) heredan de ella y solo agregan
    /// sus consultas propias.
    /// </summary>
    /// <typeparam name="T">La entidad que maneja el repositorio.</typeparam>
    /// <remarks>
    /// Es abstracta porque no se usa sola: siempre hay un repositorio concreto por entidad.
    /// No llama a SaveChanges; eso lo hace IUnitOfWork, para guardar todo en una sola
    /// transacción (RNF-04).
    /// </remarks>
    public abstract class Repository<T> : IRepository<T> where T : class
    {
        /// <summary>El DbContext compartido con el UnitOfWork de la misma petición.</summary>
        protected readonly BaityMuDbContext Db;

        /// <summary>Recibe el DbContext por inyección de dependencias.</summary>
        protected Repository(BaityMuDbContext db)
        {
            Db = db;
        }

        /// <inheritdoc />
        public virtual async Task<T?> ObtenerPorIdAsync(int id, CancellationToken ct = default) =>
            await Db.Set<T>().FindAsync([id], ct);

        /// <inheritdoc />
        public virtual async Task<IReadOnlyList<T>> ListarAsync(CancellationToken ct = default) =>
            await Db.Set<T>().ToListAsync(ct);

        /// <inheritdoc />
        public virtual async Task AgregarAsync(T entidad, CancellationToken ct = default) =>
            await Db.Set<T>().AddAsync(entidad, ct);
    }
}
