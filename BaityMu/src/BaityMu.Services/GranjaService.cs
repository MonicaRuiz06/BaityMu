using BaityMu.Domain.Entities;
using BaityMu.Domain.Exceptions;
using BaityMu.Domain.Interfaces;
using BaityMu.Services.Dtos;
using BaityMu.Services.Interfaces;
using BaityMu.Services.Modelos;

namespace BaityMu.Services
{
    /// <summary>
    /// Servicio de la granja: coordina el repositorio, el dominio y la unidad de trabajo.
    /// </summary>
    /// <remarks>
    /// El servicio no tiene reglas de negocio propias: esas viven en las entidades
    /// (Granja.AgregarVaca). Su trabajo es el "guion" de cada caso de uso:
    /// 1. cargar lo necesario del repositorio,
    /// 2. pedirle al dominio que haga el cambio,
    /// 3. guardar todo junto con IUnitOfWork.
    /// </remarks>
    public sealed class GranjaService : IGranjaService
    {
        private readonly IGranjaRepository _granjas;
        private readonly IProduccionRepository _producciones;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IClock _reloj;

        /// <summary>Recibe sus dependencias por inyección de dependencias.</summary>
        public GranjaService(
            IGranjaRepository granjas,
            IProduccionRepository producciones,
            IUnitOfWork unitOfWork,
            IClock reloj)
        {
            _granjas = granjas;
            _producciones = producciones;
            _unitOfWork = unitOfWork;
            _reloj = reloj;
        }

        /// <inheritdoc />
        public Task<Granja> ObtenerAsync(CancellationToken ct = default) => ObtenerGranjaAsync(ct);

        /// <inheritdoc />
        /// <remarks>
        /// Todos los cambios se guardan juntos con un solo SaveChangesAsync. Si cualquier
        /// valor es inválido, el dominio lanza la excepción antes de llegar a guardar, así
        /// que la base de datos queda como estaba (criterio 2: "nada cambia").
        /// </remarks>
        public async Task<Granja> ActualizarAsync(ActualizarGranjaRequest request, CancellationToken ct = default)
        {
            var repetido = request.Productos
                .GroupBy(p => p.TipoProducto)
                .FirstOrDefault(g => g.Count() > 1);
            if (repetido is not null)
                throw new ReglaDeNegocioException($"El producto {repetido.Key} viene más de una vez.");

            var granja = await ObtenerGranjaAsync(ct);

            if (request.LitrosPorVaca is decimal litrosPorVaca)
                granja.ActualizarLitrosPorVaca(litrosPorVaca);

            foreach (var producto in request.Productos)
            {
                granja.ConfigurarProducto(
                    producto.TipoProducto,
                    producto.Unidad,
                    producto.LitrosPorUnidad,
                    producto.VidaUtilDias,
                    producto.PrecioUnitario);
            }

            await _unitOfWork.SaveChangesAsync(ct);
            return granja;
        }

        /// <inheritdoc />
        public async Task<Vaca> AgregarVacaAsync(CrearVacaRequest request, CancellationToken ct = default)
        {
            var granja = await ObtenerGranjaAsync(ct);

            var fechaIngreso = request.FechaIngreso ?? _reloj.Today;
            var vaca = granja.AgregarVaca(request.Raza, request.Nombre, fechaIngreso);

            // EF ve la vaca nueva en la lista de la granja y la inserta al guardar.
            await _unitOfWork.SaveChangesAsync(ct);
            return vaca;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<VacaConLitros>> ListarVacasAsync(bool incluirRetiradas, CancellationToken ct = default)
        {
            var granja = await ObtenerGranjaAsync(ct);
            var litrosPorVaca = await _producciones.SumarLitrosPorVacaAsync(ct);

            return granja.Vacas
                .Where(v => incluirRetiradas || v.Activa)
                .OrderBy(v => v.Id)
                // GetValueOrDefault: si la vaca no está en el diccionario, nunca se ordeñó → 0 L.
                .Select(v => new VacaConLitros(v, litrosPorVaca.GetValueOrDefault(v.Id)))
                .ToList();
        }

        /// <inheritdoc />
        public async Task<VacaDetalle> ObtenerVacaAsync(int id, CancellationToken ct = default)
        {
            var granja = await ObtenerGranjaAsync(ct);
            var vaca = granja.Vacas.FirstOrDefault(v => v.Id == id)
                ?? throw new RecursoNoEncontradoException("una vaca", id);

            var historial = await _producciones.ListarPorVacaAsync(id, ct);
            return new VacaDetalle(vaca, historial);
        }

        /// <inheritdoc />
        public async Task RetirarVacaAsync(int id, CancellationToken ct = default)
        {
            var granja = await ObtenerGranjaAsync(ct);
            granja.RetirarVaca(id);

            // EF detecta que Activa cambió y hace un UPDATE (no un DELETE).
            await _unitOfWork.SaveChangesAsync(ct);
        }

        /// <summary>Trae la granja con sus vacas, o lanza 404 si no se ha sembrado.</summary>
        private async Task<Granja> ObtenerGranjaAsync(CancellationToken ct) =>
            await _granjas.ObtenerCompletaAsync(ct)
                ?? throw new RecursoNoEncontradoException(
                    "La granja no está configurada. Revisa la sección GranjaInicial de appsettings.json.");
    }
}
