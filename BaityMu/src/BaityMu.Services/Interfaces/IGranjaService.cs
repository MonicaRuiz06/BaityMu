using BaityMu.Domain.Entities;
using BaityMu.Services.Dtos;
using BaityMu.Services.Modelos;

namespace BaityMu.Services.Interfaces
{
    /// <summary>
    /// Casos de uso de la granja y su hato (Épica 1).
    /// </summary>
    /// <remarks>
    /// El controller depende de esta interfaz y no de la clase GranjaService. Así se puede
    /// cambiar o simular el servicio sin tocar el controller.
    /// </remarks>
    public interface IGranjaService
    {
        /// <summary>Consulta la configuración de la granja: n, l, n × l y los productos (HU-05).</summary>
        /// <param name="ct">Permite cancelar la operación.</param>
        /// <returns>La granja con sus vacas y productos.</returns>
        /// <exception cref="BaityMu.Domain.Exceptions.RecursoNoEncontradoException">La granja no existe (404).</exception>
        Task<Granja> ObtenerAsync(CancellationToken ct = default);

        /// <summary>Cambia l y los parámetros de los productos (HU-06).</summary>
        /// <param name="request">Los valores nuevos. Lo que no se envía se conserva.</param>
        /// <param name="ct">Permite cancelar la operación.</param>
        /// <returns>La granja con la configuración nueva.</returns>
        /// <exception cref="BaityMu.Domain.Exceptions.ReglaDeNegocioException">Algún valor no es
        /// válido, o un producto viene repetido (400). En ese caso no se guarda nada.</exception>
        /// <exception cref="BaityMu.Domain.Exceptions.RecursoNoEncontradoException">La granja no existe (404).</exception>
        Task<Granja> ActualizarAsync(ActualizarGranjaRequest request, CancellationToken ct = default);

        /// <summary>Registra una vaca nueva en la granja (HU-01).</summary>
        /// <param name="request">Raza, nombre opcional y fecha de ingreso opcional.</param>
        /// <param name="ct">Permite cancelar la operación.</param>
        /// <returns>La vaca creada, ya guardada y con su Id.</returns>
        /// <exception cref="BaityMu.Domain.Exceptions.ReglaDeNegocioException">Raza vacía (400).</exception>
        /// <exception cref="BaityMu.Domain.Exceptions.ConflictoException">Nombre repetido (409).</exception>
        /// <exception cref="BaityMu.Domain.Exceptions.RecursoNoEncontradoException">La granja no existe (404).</exception>
        Task<Vaca> AgregarVacaAsync(CrearVacaRequest request, CancellationToken ct = default);

        /// <summary>Lista las vacas de la granja con los litros que ha dado cada una (HU-02).</summary>
        /// <param name="incluirRetiradas">false (por defecto): solo las vacas activas.
        /// true: también las retiradas, que salen con Activa = false.</param>
        /// <param name="ct">Permite cancelar la operación.</param>
        /// <returns>Las vacas ordenadas por Id. Lista vacía si no hay vacas.</returns>
        /// <exception cref="BaityMu.Domain.Exceptions.RecursoNoEncontradoException">La granja no existe (404).</exception>
        Task<IReadOnlyList<VacaConLitros>> ListarVacasAsync(bool incluirRetiradas, CancellationToken ct = default);

        /// <summary>Consulta una vaca con su producción total y la de cada día (HU-03).</summary>
        /// <param name="id">Id de la vaca. También encuentra las vacas retiradas.</param>
        /// <param name="ct">Permite cancelar la operación.</param>
        /// <returns>La vaca y su historial.</returns>
        /// <exception cref="BaityMu.Domain.Exceptions.RecursoNoEncontradoException">La vaca o la granja no existen (404).</exception>
        Task<VacaDetalle> ObtenerVacaAsync(int id, CancellationToken ct = default);

        /// <summary>Retira una vaca del hato (HU-04). Se desactiva, nunca se borra.</summary>
        /// <param name="id">Id de la vaca.</param>
        /// <param name="ct">Permite cancelar la operación.</param>
        /// <exception cref="BaityMu.Domain.Exceptions.RecursoNoEncontradoException">La vaca no existe (404).</exception>
        /// <exception cref="BaityMu.Domain.Exceptions.ReglaDeNegocioException">Es la última vaca activa (400).</exception>
        /// <exception cref="BaityMu.Domain.Exceptions.ConflictoException">Ya estaba retirada (409).</exception>
        Task RetirarVacaAsync(int id, CancellationToken ct = default);
    }
}
