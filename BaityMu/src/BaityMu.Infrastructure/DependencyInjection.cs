using BaityMu.Domain.Interfaces;
using BaityMu.Infrastructure.Data;
using BaityMu.Infrastructure.Repositories;
using BaityMu.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BaityMu.Infrastructure
{
    /// <summary>
    /// Registro de los servicios de la capa Infrastructure en el contenedor de
    /// inyección de dependencias de .NET.
    /// </summary>
    /// <remarks>
    /// ¿Qué es la inyección de dependencias?
    /// Las clases no crean lo que necesitan con "new": lo piden en su constructor y el
    /// contenedor de .NET se lo entrega. Esta clase le dice al contenedor qué entregar
    /// cuando alguien pide cada interfaz.
    ///
    /// Es static porque no se crean objetos de ella: solo tiene un método de extensión.
    /// </remarks>
    public static class DependencyInjection
    {
        /// <summary>
        /// Registra el DbContext con SQL Server, la unidad de trabajo, el reloj y los repositorios.
        /// </summary>
        /// <param name="services">Colección de servicios de la aplicación. El "this" lo
        /// convierte en método de extensión: en Program.cs se llama
        /// builder.Services.AddRepositories(builder.Configuration).</param>
        /// <param name="configuration">Configuración de la aplicación, de donde se lee la
        /// cadena de conexión "DefaultConnection" de appsettings.json.</param>
        /// <returns>La misma colección, para poder encadenar más registros.</returns>
        /// <remarks>
        /// Tiempos de vida:
        /// - Scoped (DbContext y UnitOfWork): un objeto por cada petición HTTP. Todos los
        ///   repositorios de una petición comparten el mismo DbContext, así un solo
        ///   SaveChangesAsync guarda todo junto.
        /// - Singleton (SystemClock): un único objeto para toda la aplicación, porque no
        ///   guarda estado.
        ///
        /// Los repositorios de cada entidad se registran aquí con cada historia de usuario
        /// (HU-01: GranjaRepository; HU-02: ProduccionRepository; HU-09: LoteRepository).
        /// </remarks>
        public static IServiceCollection AddRepositories(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddDbContext<BaityMuDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddSingleton<IClock, SystemClock>();

            // Repositorios (Scoped: comparten el DbContext de la petición)
            services.AddScoped<IGranjaRepository, GranjaRepository>();
            services.AddScoped<IProduccionRepository, ProduccionRepository>();
            services.AddScoped<ILoteRepository, LoteRepository>();

            return services;
        }
    }
}
