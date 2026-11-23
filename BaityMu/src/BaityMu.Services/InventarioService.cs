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
    /// El estado de cada lote y el paso a dañado los decide el dominio
    /// (LoteProducto.ObtenerEstado y MarcarComoDanado); el servicio elige la fecha, carga
    /// los lotes y guarda con IUnitOfWork.
    /// </remarks>
    public sealed class InventarioService : IInventarioService
    {
        private readonly ILoteRepository _lotes;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IClock _reloj;

        /// <summary>Recibe sus dependencias por inyección de dependencias.</summary>
        public InventarioService(ILoteRepository lotes, IUnitOfWork unitOfWork, IClock reloj)
        {
            _lotes = lotes;
            _unitOfWork = unitOfWork;
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

        /// <summary>Plazo que se usa cuando no se indica cuántos días (criterio 1 de la HU-12).</summary>
        public const int DiasPorDefectoProximosAVencer = 3;

        /// <inheritdoc />
        /// <remarks>
        /// Un lote entra si:
        /// - tiene existencias (CantidadDisponible &gt; 0): uno agotado no se puede vender;
        /// - no está vencido en la fecha (EstaVencido, RN-08): uno vencido ya no se puede salvar;
        /// - su FechaVencimiento es a más tardar fecha + dias.
        /// Con dias = 0 salen solo los que vencen hoy (todavía se venden ese día).
        /// </remarks>
        public async Task<IReadOnlyList<LoteConEstado>> ListarProximosAVencerAsync(
            int? dias,
            DateOnly? fecha,
            CancellationToken ct = default)
        {
            var plazo = dias ?? DiasPorDefectoProximosAVencer;
            if (plazo < 0)
                throw new ReglaDeNegocioException("El número de días no puede ser negativo.");

            var dia = fecha ?? _reloj.Today;
            var limite = dia.AddDays(plazo);
            var lotes = await _lotes.ListarAsync(null, null, null, ct);

            return lotes
                .Where(l => l.CantidadDisponible > 0
                            && !l.EstaVencido(dia)
                            && l.FechaVencimiento <= limite)
                .OrderBy(l => l.FechaVencimiento)
                .ThenBy(l => l.Id)
                .Select(l => new LoteConEstado(l, l.ObtenerEstado(dia)))
                .ToList();
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

        /// <inheritdoc />
        /// <remarks>
        /// Solo trae los lotes vencidos que todavía tienen existencias. Un lote agotado o
        /// ya procesado no aparece, así que procesar dos veces la misma fecha no duplica
        /// el producto dañado (criterio 6). Todo se guarda en una sola transacción.
        /// </remarks>
        public async Task<ResultadoVencimientos> ProcesarVencimientosAsync(DateOnly? fecha, CancellationToken ct = default)
        {
            var dia = fecha ?? _reloj.Today;
            var vencidos = await _lotes.ObtenerVencidosConExistenciasAsync(dia, ct);

            var dañados = vencidos
                .Select(lote => new LoteDañado(lote, lote.MarcarComoDanado(dia)))
                .ToList();

            await _unitOfWork.SaveChangesAsync(ct);
            return new ResultadoVencimientos(dia, dañados);
        }
    }
}
