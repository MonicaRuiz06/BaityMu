using BaityMu.Domain.Entities;
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
        private readonly IGranjaRepository _granjas;
        private readonly IVentaRepository _ventas;
        private readonly IProduccionRepository _producciones;
        private readonly ILoteRepository _lotes;
        private readonly IClock _reloj;

        /// <summary>Recibe sus dependencias por inyección de dependencias.</summary>
        public ReporteService(
            IGranjaRepository granjas,
            IVentaRepository ventas,
            IProduccionRepository producciones,
            ILoteRepository lotes,
            IClock reloj)
        {
            _granjas = granjas;
            _ventas = ventas;
            _producciones = producciones;
            _lotes = lotes;
            _reloj = reloj;
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

        /// <inheritdoc />
        /// <remarks>
        /// Agrupa los lotes del período por (fecha, producto). La cantidad es la inicial de
        /// cada lote: lo que se elaboró, aunque después se haya vendido o dañado.
        /// La unidad no está en el lote sino en el producto de la granja (ParametroProducto).
        /// </remarks>
        public async Task<ReporteProductoDiarioResponse> ProductoDiarioAsync(
            DateOnly? desde,
            DateOnly? hasta,
            CancellationToken ct = default)
        {
            ValidarRango(desde, hasta);

            var lotes = await _lotes.ListarAsync(null, desde, hasta, ct);
            var unidades = await UnidadesAsync(ct);

            return new ReporteProductoDiarioResponse
            {
                Desde = desde,
                Hasta = hasta,
                Dias = lotes
                    .GroupBy(l => (l.FechaProduccion, l.TipoProducto))
                    .OrderBy(g => g.Key.FechaProduccion)
                    .ThenBy(g => g.Key.TipoProducto)
                    .Select(g => Elaborado(g.Key.FechaProduccion, g.Key.TipoProducto, g, unidades))
                    .ToList(),
                TotalesPorProducto = lotes
                    .GroupBy(l => l.TipoProducto)
                    .OrderBy(g => g.Key)
                    .Select(g => Elaborado(null, g.Key, g, unidades))
                    .ToList(),
            };
        }

        /// <inheritdoc />
        /// <remarks>
        /// - Producido: suma de CantidadInicial de los lotes.
        /// - Vendido: suma de las ventas (no se deduce de los lotes: se toma de las ventas reales).
        /// - Disponible / VencidoSinProcesar: lo que queda en los lotes, separado según si el
        ///   lote está vencido hoy (LoteProducto.EstaVencido, RN-08).
        /// - Dañado: suma de CantidadDanada.
        /// Salen todos los productos configurados en la granja, aunque no tengan lotes (en 0).
        /// </remarks>
        public async Task<IReadOnlyList<ProductoTotalItem>> ProductoTotalAsync(DateOnly? fecha = null, CancellationToken ct = default)
        {
            var hoy = fecha ?? _reloj.Today;
            var lotes = await _lotes.ListarAsync(null, null, null, ct);
            var ventas = await _ventas.ListarAsync(null, null, null, ct);
            var unidades = await UnidadesAsync(ct);

            // Productos configurados + los que tengan lotes (por si alguno se quitó de la granja).
            var tipos = unidades.Keys
                .Concat(lotes.Select(l => l.TipoProducto))
                .Distinct()
                .OrderBy(t => t);

            return tipos.Select(tipo =>
            {
                var delTipo = lotes.Where(l => l.TipoProducto == tipo).ToList();
                return new ProductoTotalItem
                {
                    TipoProducto = tipo,
                    Unidad = unidades.GetValueOrDefault(tipo, string.Empty),
                    Producido = delTipo.Sum(l => l.CantidadInicial),
                    Vendido = ventas.Where(v => v.TipoProducto == tipo).Sum(v => v.Cantidad),
                    Disponible = delTipo.Where(l => !l.EstaVencido(hoy)).Sum(l => l.CantidadDisponible),
                    VencidoSinProcesar = delTipo.Where(l => l.EstaVencido(hoy)).Sum(l => l.CantidadDisponible),
                    Dañado = delTipo.Sum(l => l.CantidadDanada),
                };
            }).ToList();
        }

        /// <inheritdoc />
        /// <remarks>
        /// Solo aparecen lotes a los que ya se les procesó el vencimiento (HU-11): ahí es
        /// cuando su existencia pasa a CantidadDanada. Un lote vencido sin procesar todavía
        /// no cuenta como dañado (se ve en el reporte de producto total como "vencido sin procesar").
        /// </remarks>
        public async Task<ReporteProductoDanadoResponse> ProductoDanadoAsync(
            DateOnly? desde,
            DateOnly? hasta,
            TipoProducto? tipoProducto,
            CancellationToken ct = default)
        {
            ValidarRango(desde, hasta);

            var lotes = await _lotes.ListarDanadosAsync(tipoProducto, desde, hasta, ct);
            var unidades = await UnidadesAsync(ct);

            return new ReporteProductoDanadoResponse
            {
                Desde = desde,
                Hasta = hasta,
                TipoProducto = tipoProducto,
                Lotes = lotes.Select(l => new LoteDanadoItem
                {
                    LoteId = l.Id,
                    TipoProducto = l.TipoProducto,
                    Unidad = unidades.GetValueOrDefault(l.TipoProducto, string.Empty),
                    FechaProduccion = l.FechaProduccion,
                    FechaVencimiento = l.FechaVencimiento,
                    CantidadDañada = l.CantidadDanada,
                }).ToList(),
                TotalesPorProducto = lotes
                    .GroupBy(l => l.TipoProducto)
                    .OrderBy(g => g.Key)
                    .Select(g => new TotalDanadoProducto
                    {
                        TipoProducto = g.Key,
                        Unidad = unidades.GetValueOrDefault(g.Key, string.Empty),
                        NumeroLotes = g.Count(),
                        Cantidad = g.Sum(l => l.CantidadDanada),
                    }).ToList(),
            };
        }

        /// <summary>Suma un grupo de lotes en un renglón del reporte de producto.</summary>
        private static ProductoElaboradoItem Elaborado(
            DateOnly? fecha,
            TipoProducto tipo,
            IEnumerable<LoteProducto> lotes,
            IReadOnlyDictionary<TipoProducto, string> unidades) =>
            new()
            {
                Fecha = fecha,
                TipoProducto = tipo,
                Unidad = unidades.GetValueOrDefault(tipo, string.Empty),
                Cantidad = lotes.Sum(l => l.CantidadInicial),
                LitrosUsados = lotes.Sum(l => l.LitrosUsados),
                NumeroLotes = lotes.Count(),
            };

        /// <summary>Unidad de cada producto configurado ("kg" para el queso, "L" para el yogurt).</summary>
        private async Task<IReadOnlyDictionary<TipoProducto, string>> UnidadesAsync(CancellationToken ct)
        {
            var granja = await _granjas.ObtenerCompletaAsync(ct);
            return granja?.Productos.ToDictionary(p => p.TipoProducto, p => p.Unidad)
                ?? new Dictionary<TipoProducto, string>();
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
