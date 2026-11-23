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
        private readonly IProduccionRepository _producciones;
        private readonly ILoteRepository _lotes;

        /// <summary>Recibe sus dependencias por inyección de dependencias.</summary>
        public ReporteService(IVentaRepository ventas, IProduccionRepository producciones, ILoteRepository lotes)
        {
            _ventas = ventas;
            _producciones = producciones;
            _lotes = lotes;
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

        /// <inheritdoc />
        /// <remarks>
        /// Junta dos consultas por fecha:
        /// - los ordeños del período, sumados por día (litros producidos);
        /// - los lotes producidos en el período, sumando sus LitrosUsados por día (transformados).
        /// Un lote siempre lleva la fecha de la leche que usó (RN-02), así que las dos se
        /// cruzan por fecha. Solo aparecen los días con producción.
        /// </remarks>
        public async Task<ReporteLecheDiariaResponse> LecheDiariaAsync(
            DateOnly? desde,
            DateOnly? hasta,
            CancellationToken ct = default)
        {
            ValidarRango(desde, hasta);

            var producciones = await _producciones.ListarAsync(desde, hasta, ct);
            var lotes = await _lotes.ListarAsync(null, desde, hasta, ct);

            // Diccionario fecha → litros transformados, para buscar cada día rápido.
            var transformadosPorDia = lotes
                .GroupBy(l => l.FechaProduccion)
                .ToDictionary(g => g.Key, g => g.Sum(l => l.LitrosUsados));

            var dias = producciones
                .GroupBy(p => p.Fecha)
                .OrderBy(g => g.Key)
                .Select(g =>
                {
                    var producidos = g.Sum(p => p.Litros);
                    // GetValueOrDefault: si ese día no se transformó nada, da 0.
                    var transformados = transformadosPorDia.GetValueOrDefault(g.Key);
                    return new LecheDelDia
                    {
                        Fecha = g.Key,
                        LitrosProducidos = producidos,
                        LitrosTransformados = transformados,
                        LitrosNoAprovechados = producidos - transformados,
                    };
                })
                .ToList();

            return new ReporteLecheDiariaResponse
            {
                Desde = desde,
                Hasta = hasta,
                Dias = dias,
                TotalProducido = dias.Sum(d => d.LitrosProducidos),
                TotalTransformado = dias.Sum(d => d.LitrosTransformados),
                TotalNoAprovechado = dias.Sum(d => d.LitrosNoAprovechados),
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
