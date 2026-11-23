using BaityMu.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace BaityMu.Services
{
    /// <summary>
    /// Registro de los servicios de negocio (capa Services) en el contenedor de
    /// inyección de dependencias de .NET.
    /// </summary>
    /// <remarks>
    /// Se llama igual que la clase de Infrastructure, pero no chocan porque están en
    /// namespaces distintos: BaityMu.Services.DependencyInjection y
    /// BaityMu.Infrastructure.DependencyInjection.
    /// </remarks>
    public static class DependencyInjection
    {
        /// <summary>
        /// Registra los servicios de negocio: GranjaService, ProduccionService,
        /// InventarioService, VentaService y ReporteService.
        /// </summary>
        /// <param name="services">Colección de servicios de la aplicación. Se llama desde
        /// Program.cs con builder.Services.AddServices().</param>
        /// <returns>La misma colección, para poder encadenar más registros.</returns>
        /// <remarks>
        /// Cada historia de usuario agrega aquí su servicio con AddScoped (uno por
        /// petición HTTP, igual que el DbContext que usa por debajo).
        ///
        /// No recibe IConfiguration porque los servicios de negocio no leen appsettings.json.
        /// </remarks>
        public static IServiceCollection AddServices(this IServiceCollection services)
        {
            services.AddScoped<IGranjaService, GranjaService>();
            services.AddScoped<IProduccionService, ProduccionService>();

            return services;
        }
    }
}
