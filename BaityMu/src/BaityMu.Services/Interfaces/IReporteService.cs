using BaityMu.Domain.Enums;
using BaityMu.Services.Dtos;

namespace BaityMu.Services.Interfaces
{
    /// <summary>Reportes de la granja (Épica 6). Rol: BaityGerente.</summary>
    /// <remarks>
    /// Todos los reportes son de solo lectura: no modifican nada ni llaman a SaveChangesAsync.
    /// Reglas comunes: si 'desde' es posterior a 'hasta' se lanza ReglaDeNegocioException
    /// (400); si no hay datos se devuelve el reporte con listas vacías y totales en 0.
    /// </remarks>
    public interface IReporteService
    {
        /// <summary>Reporte de ventas de un período, con totales por producto y total general (HU-15).</summary>
        /// <param name="desde">Fecha mínima, o null para no limitar.</param>
        /// <param name="hasta">Fecha máxima, o null para no limitar.</param>
        /// <param name="tipoProducto">Solo un producto, o null para todos.</param>
        /// <param name="ct">Permite cancelar la operación.</param>
        /// <exception cref="BaityMu.Domain.Exceptions.ReglaDeNegocioException">desde es posterior a hasta (400).</exception>
        Task<ReporteVentasResponse> VentasAsync(
            DateOnly? desde,
            DateOnly? hasta,
            TipoProducto? tipoProducto,
            CancellationToken ct = default);

        /// <summary>Litros producidos, transformados y no aprovechados de cada día (HU-16).</summary>
        /// <param name="desde">Fecha mínima, o null para no limitar.</param>
        /// <param name="hasta">Fecha máxima, o null para no limitar.</param>
        /// <param name="ct">Permite cancelar la operación.</param>
        /// <exception cref="BaityMu.Domain.Exceptions.ReglaDeNegocioException">desde es posterior a hasta (400).</exception>
        Task<ReporteLecheDiariaResponse> LecheDiariaAsync(
            DateOnly? desde,
            DateOnly? hasta,
            CancellationToken ct = default);

        /// <summary>Cantidad de cada producto elaborada cada día, con su unidad y los litros usados (HU-17).</summary>
        /// <param name="desde">Fecha mínima, o null para no limitar.</param>
        /// <param name="hasta">Fecha máxima, o null para no limitar.</param>
        /// <param name="ct">Permite cancelar la operación.</param>
        /// <exception cref="BaityMu.Domain.Exceptions.ReglaDeNegocioException">desde es posterior a hasta (400).</exception>
        Task<ReporteProductoDiarioResponse> ProductoDiarioAsync(
            DateOnly? desde,
            DateOnly? hasta,
            CancellationToken ct = default);
    }
}
