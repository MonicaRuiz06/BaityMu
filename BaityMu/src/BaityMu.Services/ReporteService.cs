using BaityMu.Domain.Enums;
using BaityMu.Domain.Exceptions;
using BaityMu.Domain.Interfaces;
using BaityMu.Services.Dtos;
using BaityMu.Services.Interfaces;

namespace BaityMu.Services
{
    /// <summary>
    /// Servicio de reportes (Épica 6). Lee los datos con los repositorios que ya existen y
    /// arma cada reporte en memoria.
    /// </summary>
    /// <remarks>
    /// ¿Por qué devuelve DTOs directamente y no entidades?
    /// Un reporte no es una entidad del dominio: es un resumen calculado (sumas, agrupaciones).
    /// No hay nada que mapear con AutoMapper, así que el servicio arma el DTO y el controlador
    /// solo lo devuelve.
    ///
    /// Es de solo lectura: no usa IUnitOfWork porque nunca guarda cambios.
    /// </remarks>
    public sealed class ReporteService : IReporteService
    {
        private readonly IVentaRepository _ventas;

        /// <summary>Recibe sus dependencias por inyección de dependencias.</summary>
        public ReporteService(IVentaRepository ventas)
        {
            _ventas = ventas;
        }

        /// <inheritdoc />
        public async Task<ReporteVentasResponse> VentasAsync(
            DateOnly? desde,
            DateOnly? hasta,
            TipoProducto? tipoProducto,
            CancellationToken ct = default)
        {
            ValidarRango(desde, hasta);

            // Reutiliza la consulta de la HU-14 (ya viene ordenada por fecha e Id).
            var ventas = await _ventas.ListarAsync(desde, hasta, tipoProducto, ct);

            return new ReporteVentasResponse
            {
                Desde = desde,
                Hasta = hasta,
                TipoProducto = tipoProducto,
                Ventas = ventas.Select(v => new ReporteVentaItem
                {
                    Id = v.Id,
                    Fecha = v.Fecha,
                    TipoProducto = v.TipoProducto,
                    Cantidad = v.Cantidad,
                    PrecioUnitario = v.PrecioUnitario,
                    Total = v.Total,
                }).ToList(),

                // GroupBy junta las ventas de cada producto; OrderBy deja Queso antes que Yogurt.
                TotalesPorProducto = ventas
                    .GroupBy(v => v.TipoProducto)
                    .OrderBy(g => g.Key)
                    .Select(g => new TotalVentasProducto
                    {
                        TipoProducto = g.Key,
                        NumeroVentas = g.Count(),
                        Cantidad = g.Sum(v => v.Cantidad),
                        Total = g.Sum(v => v.Total),
                    }).ToList(),

                // Sum de una lista vacía es 0: así se cumple "sin datos → totales en 0".
                TotalGeneral = ventas.Sum(v => v.Total),
            };
        }

        /// <summary>
        /// Regla común de la Épica 6, en un solo lugar: el rango no puede estar al revés.
        /// </summary>
        /// <remarks>
        /// Si alguna de las dos fechas es null, la comparación da false y no se lanza nada
        /// (un rango abierto siempre es válido).
        /// </remarks>
        /// <exception cref="ReglaDeNegocioException">desde es posterior a hasta.</exception>
        private static void ValidarRango(DateOnly? desde, DateOnly? hasta)
        {
            if (desde > hasta)
                throw new ReglaDeNegocioException("La fecha 'desde' no puede ser posterior a 'hasta'.");
        }
    }
}
