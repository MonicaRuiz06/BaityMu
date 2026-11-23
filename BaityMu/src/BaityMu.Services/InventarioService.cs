using BaityMu.Domain.Enums;
using BaityMu.Domain.Exceptions;
using BaityMu.Domain.Interfaces;
using BaityMu.Services.Interfaces;
using BaityMu.Services.Modelos;

namespace BaityMu.Services
{
    /// <summary>
    /// Servicio del inventario: consulta los lotes y calcula su estado según la fecha.
    /// </summary>
    /// <remarks>
    /// Solo lee: no usa IUnitOfWork. El estado de cada lote lo decide el dominio
    /// (LoteProducto.ObtenerEstado); el servicio solo elige la fecha y arma el resultado.
    /// </remarks>
    public sealed class InventarioService : IInventarioService
    {
        private readonly ILoteRepository _lotes;
        private readonly IClock _reloj;

        /// <summary>Recibe sus dependencias por inyección de dependencias.</summary>
        public InventarioService(ILoteRepository lotes, IClock reloj)
        {
            _lotes = lotes;
            _reloj = reloj;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<LoteConEstado>> ListarLotesAsync(
            TipoProducto? tipoProducto,
            DateOnly? desde,
            DateOnly? hasta,
            DateOnly? fecha,
            CancellationToken ct = default)
        {
            if (desde > hasta)
                throw new ReglaDeNegocioException("La fecha 'desde' no puede ser posterior a 'hasta'.");

            var dia = fecha ?? _reloj.Today;
            var lotes = await _lotes.ListarAsync(tipoProducto, desde, hasta, ct);
            return lotes.Select(l => new LoteConEstado(l, l.ObtenerEstado(dia))).ToList();
        }

        /// <inheritdoc />
        public async Task<LoteConEstado> ObtenerLoteAsync(int id, DateOnly? fecha, CancellationToken ct = default)
        {
            var lote = await _lotes.ObtenerPorIdAsync(id, ct)
                ?? throw new RecursoNoEncontradoException("un lote", id);

            return new LoteConEstado(lote, lote.ObtenerEstado(fecha ?? _reloj.Today));
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<ResumenInventario>> ObtenerResumenAsync(DateOnly? fecha, CancellationToken ct = default)
        {
            var dia = fecha ?? _reloj.Today;
            var lotes = await _lotes.ListarAsync(null, null, null, ct);

            // Enum.GetValues recorre Queso y Yogurt, así ambos aparecen aunque no tengan lotes.
            return Enum.GetValues<TipoProducto>()
                .Select(tipo =>
                {
                    var delTipo = lotes.Where(l => l.TipoProducto == tipo).ToList();
                    return new ResumenInventario(
                        tipo,
                        CantidadVendible: delTipo.Where(l => !l.EstaVencido(dia)).Sum(l => l.CantidadDisponible),
                        CantidadVencidaSinProcesar: delTipo.Where(l => l.EstaVencido(dia)).Sum(l => l.CantidadDisponible),
                        CantidadDañada: delTipo.Sum(l => l.CantidadDanada));
                })
                .ToList();
        }
    }
}
