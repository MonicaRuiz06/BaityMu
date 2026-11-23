using BaityMu.Domain.Entities;
using BaityMu.Domain.Enums;
using BaityMu.Domain.Exceptions;
using BaityMu.Infrastructure.Data;
using BaityMu.Infrastructure.Repositories;
using BaityMu.Services;
using BaityMu.Services.Dtos;
using BaityMu.Tests.Fakes;
using BaityMu.Tests.Infraestructura;
using Microsoft.EntityFrameworkCore;

namespace BaityMu.Tests
{
    /// <summary>
    /// El escenario de referencia del taller, de punta a punta, con los servicios reales y
    /// una base InMemory. Cada paso usa el día que indica el escenario (FakeClock, RN-10).
    /// </summary>
    /// <remarks>
    /// | Día | Acción | Resultado esperado |
    /// | 1 | Producción y transformación | 150 L; Q1 = 10 kg; Y1 = 50 L, vence el 16 |
    /// | 2 | Producción y transformación | 150 L; Q2 = 15 kg |
    /// | 3 | Ventas | 12 kg de queso = 10 de Q1 + 2 de Q2; 20 L de yogurt de Y1 |
    /// | 17 | Vencimientos y venta | Y1: 30 L a dañado; vender 5 L de yogurt → 409 |
    /// | 17 | Reporte de producto total | Queso 25/12/13/0 · Yogurt 50/20/0/30 (HU-18) |
    /// </remarks>
    [TestFixture]
    public class EscenarioDeReferenciaTests
    {
        private BaityMuDbContext _db = null!;
        private FakeClock _reloj = null!;
        private ProduccionService _produccion = null!;
        private InventarioService _inventario = null!;
        private VentaService _ventas = null!;
        private GranjaService _granja = null!;
        private ReporteService _reportes = null!;

        [SetUp]
        public async Task SetUp()
        {
            _db = BaseDeDatosEnMemoria.Crear();
            _reloj = new FakeClock(new DateOnly(2026, 12, 1));
            var granjas = new GranjaRepository(_db);
            var producciones = new ProduccionRepository(_db);
            var lotes = new LoteRepository(_db);
            var unitOfWork = new UnitOfWork(_db);
            _produccion = new ProduccionService(granjas, producciones, lotes, unitOfWork, _reloj);
            _inventario = new InventarioService(lotes, unitOfWork, _reloj);
            var ventas = new VentaRepository(_db);
            _ventas = new VentaService(granjas, lotes, ventas, unitOfWork, _reloj);
            _reportes = new ReporteService(granjas, ventas, producciones, lotes, _reloj);
            _granja = new GranjaService(granjas, producciones, unitOfWork, _reloj);

            var granja = new Granja("Granja BaityMu", 15m);
            for (var i = 0; i < 10; i++)
                granja.AgregarVaca("Holstein", null, _reloj.Today);
            granja.AgregarProducto(TipoProducto.Queso, "kg", 10m, 30, 28000m);
            granja.AgregarProducto(TipoProducto.Yogurt, "L", 1m, 15, 6000m);
            _db.Granjas.Add(granja);
            await _db.SaveChangesAsync();
            _db.ChangeTracker.Clear();
        }

        [TearDown]
        public void TearDown() => _db.Dispose();

        /// <summary>Cada paso se ejecuta "como otra petición": sin entidades en memoria.</summary>
        private void NuevaPeticion() => _db.ChangeTracker.Clear();

        private async Task<LoteProducto> LoteAsync(int id) => await _db.LotesProducto.AsNoTracking().SingleAsync(l => l.Id == id);

        [Test]
        public async Task EscenarioCompleto_Dias1Al17()
        {
            // ---------------- Día 1 (2026-12-01)
            var dia1 = await _produccion.RegistrarAsync(new RegistrarProduccionRequest());
            Assert.That(dia1.TotalLitros, Is.EqualTo(150m), "Día 1: 150 L");
            NuevaPeticion();

            var q1 = await _produccion.TransformarAsync(new() { TipoProducto = TipoProducto.Queso, Litros = 100m });
            var y1 = await _produccion.TransformarAsync(new() { TipoProducto = TipoProducto.Yogurt, Litros = 50m });
            Assert.That(q1.CantidadInicial, Is.EqualTo(10m), "Q1 = 10 kg");
            Assert.That(y1.CantidadInicial, Is.EqualTo(50m), "Y1 = 50 L");
            Assert.That(y1.FechaVencimiento, Is.EqualTo(new DateOnly(2026, 12, 16)), "Y1 vence el 16");
            NuevaPeticion();

            // ---------------- Día 2 (2026-12-02)
            _reloj.AvanzarDias(1);
            Assert.That((await _produccion.RegistrarAsync(new RegistrarProduccionRequest())).TotalLitros, Is.EqualTo(150m));
            NuevaPeticion();
            var q2 = await _produccion.TransformarAsync(new() { TipoProducto = TipoProducto.Queso, Litros = 150m });
            Assert.That(q2.CantidadInicial, Is.EqualTo(15m), "Q2 = 15 kg");
            NuevaPeticion();

            // ---------------- Día 3 (2026-12-03): ventas FIFO
            _reloj.AvanzarDias(1);
            var ventaQueso = await _ventas.RegistrarVentaAsync(new() { TipoProducto = TipoProducto.Queso, Cantidad = 12m });
            Assert.That(ventaQueso.Detalles.Select(d => (d.LoteId, d.Cantidad)),
                Is.EqualTo(new[] { (q1.Id, 10m), (q2.Id, 2m) }), "12 kg = 10 de Q1 + 2 de Q2");
            Assert.That(ventaQueso.Total, Is.EqualTo(336000m));
            NuevaPeticion();

            var ventaYogurt = await _ventas.RegistrarVentaAsync(new() { TipoProducto = TipoProducto.Yogurt, Cantidad = 20m });
            Assert.That(ventaYogurt.Detalles.Single().LoteId, Is.EqualTo(y1.Id), "20 L salen de Y1");
            NuevaPeticion();

            Assert.That((await LoteAsync(q1.Id)).CantidadDisponible, Is.EqualTo(0m), "Q1 agotado");
            Assert.That((await LoteAsync(q2.Id)).CantidadDisponible, Is.EqualTo(13m), "Quedan 13 kg de Q2");
            Assert.That((await LoteAsync(y1.Id)).CantidadDisponible, Is.EqualTo(30m), "Quedan 30 L de Y1");

            // ---------------- Día 17 (2026-12-17)
            _reloj.AvanzarDias(14);
            var vencimientos = await _inventario.ProcesarVencimientosAsync(null);
            Assert.That(vencimientos.Lotes.Single().Lote.Id, Is.EqualTo(y1.Id));
            Assert.That(vencimientos.Lotes.Single().CantidadDañada, Is.EqualTo(30m), "Y1: 30 L a dañado");
            NuevaPeticion();

            Assert.ThrowsAsync<InventarioInsuficienteException>(() =>
                _ventas.RegistrarVentaAsync(new() { TipoProducto = TipoProducto.Yogurt, Cantidad = 5m }),
                "Día 17: vender 5 L de yogurt → 409");
            NuevaPeticion();

            // Reporte de producto total al día 17 (HU-18): la tabla del taller
            var total = await _reportes.ProductoTotalAsync();
            Assert.That(total.Select(t => (t.TipoProducto, t.Producido, t.Vendido, t.Disponible, t.Dañado)),
                Is.EqualTo(new[]
                {
                    (TipoProducto.Queso, 25m, 12m, 13m, 0m),
                    (TipoProducto.Yogurt, 50m, 20m, 0m, 30m),
                }), "Queso 25/12/13/0 · Yogurt 50/20/0/30");

            // Resumen de inventario al día 17
            var resumen = await _inventario.ObtenerResumenAsync(null);
            var queso = resumen.Single(r => r.TipoProducto == TipoProducto.Queso);
            var yogurt = resumen.Single(r => r.TipoProducto == TipoProducto.Yogurt);
            Assert.That((queso.CantidadVendible, queso.CantidadDañada), Is.EqualTo((13m, 0m)));
            Assert.That((yogurt.CantidadVendible, yogurt.CantidadDañada), Is.EqualTo((0m, 30m)));
            Assert.That(await _db.Ventas.CountAsync(), Is.EqualTo(2), "La venta rechazada no se guardó");
        }

        [Test]
        public async Task VentaRechazada_NoModificaNingunLoteEnLaBaseDeDatos()
        {
            // Prueba mínima del taller, con la base de datos: hay 10 kg y se piden 11
            await _produccion.RegistrarAsync(new RegistrarProduccionRequest());
            NuevaPeticion();
            var q1 = await _produccion.TransformarAsync(new() { TipoProducto = TipoProducto.Queso, Litros = 100m });
            NuevaPeticion();

            Assert.ThrowsAsync<InventarioInsuficienteException>(() =>
                _ventas.RegistrarVentaAsync(new() { TipoProducto = TipoProducto.Queso, Cantidad = 11m }));
            NuevaPeticion();

            Assert.That((await LoteAsync(q1.Id)).CantidadDisponible, Is.EqualTo(10m));
            Assert.That(await _db.Ventas.CountAsync(), Is.EqualTo(0));
            Assert.That(await _db.VentaDetalles.CountAsync(), Is.EqualTo(0));
        }

        [Test]
        public async Task CambiarElPrecio_LasVentasAnterioresConservanElSuyo()
        {
            // Criterio 3 de la HU-06
            await _produccion.RegistrarAsync(new RegistrarProduccionRequest());
            NuevaPeticion();
            await _produccion.TransformarAsync(new() { TipoProducto = TipoProducto.Queso, Litros = 100m });
            NuevaPeticion();
            var antes = await _ventas.RegistrarVentaAsync(new() { TipoProducto = TipoProducto.Queso, Cantidad = 1m });
            NuevaPeticion();

            await _granja.ActualizarAsync(new ActualizarGranjaRequest
            {
                Productos = { new() { TipoProducto = TipoProducto.Queso, LitrosPorUnidad = 10m, VidaUtilDias = 30, PrecioUnitario = 30000m } }
            });
            NuevaPeticion();
            var despues = await _ventas.RegistrarVentaAsync(new() { TipoProducto = TipoProducto.Queso, Cantidad = 1m });
            NuevaPeticion();

            var guardada = await _db.Ventas.AsNoTracking().SingleAsync(v => v.Id == antes.Id);
            Assert.That(guardada.PrecioUnitario, Is.EqualTo(28000m));
            Assert.That(despues.PrecioUnitario, Is.EqualTo(30000m));
        }

        [Test]
        public async Task Vender_ProductoNoConfigurado_LanzaReglaDeNegocio()
        {
            var granja = await _db.Granjas.Include(g => g.Productos).SingleAsync();
            _db.ParametrosProducto.RemoveRange(granja.Productos.Where(p => p.TipoProducto == TipoProducto.Yogurt));
            await _db.SaveChangesAsync();
            NuevaPeticion();

            Assert.ThrowsAsync<ReglaDeNegocioException>(() =>
                _ventas.RegistrarVentaAsync(new() { TipoProducto = TipoProducto.Yogurt, Cantidad = 1m }));
        }

        // ----------------------------------------------------------------------------
        // HU-14 Consultar ventas
        // ----------------------------------------------------------------------------

        /// <summary>Días 1 a 3 del escenario: Q1, Y1, Q2 y las dos ventas del día 3.</summary>
        private async Task<(Venta Queso, Venta Yogurt)> HastaElDia3Async()
        {
            await _produccion.RegistrarAsync(new RegistrarProduccionRequest());
            NuevaPeticion();
            await _produccion.TransformarAsync(new() { TipoProducto = TipoProducto.Queso, Litros = 100m });
            await _produccion.TransformarAsync(new() { TipoProducto = TipoProducto.Yogurt, Litros = 50m });
            NuevaPeticion();
            _reloj.AvanzarDias(1);
            await _produccion.RegistrarAsync(new RegistrarProduccionRequest());
            NuevaPeticion();
            await _produccion.TransformarAsync(new() { TipoProducto = TipoProducto.Queso, Litros = 150m });
            NuevaPeticion();
            _reloj.AvanzarDias(1);
            var queso = await _ventas.RegistrarVentaAsync(new() { TipoProducto = TipoProducto.Queso, Cantidad = 12m });
            NuevaPeticion();
            var yogurt = await _ventas.RegistrarVentaAsync(new() { TipoProducto = TipoProducto.Yogurt, Cantidad = 20m });
            NuevaPeticion();
            return (queso, yogurt);
        }

        [Test]
        public async Task ObtenerVenta_TraeSusDetallesConLaFechaDeCadaLote()
        {
            var (queso, _) = await HastaElDia3Async();

            var venta = await _ventas.ObtenerAsync(queso.Id);

            Assert.That(venta.Cantidad, Is.EqualTo(12m));
            Assert.That(venta.Detalles.Select(d => (d.Lote.FechaProduccion, d.Cantidad)),
                Is.EqualTo(new[] { (new DateOnly(2026, 12, 1), 10m), (new DateOnly(2026, 12, 2), 2m) }));
        }

        [Test]
        public void ObtenerVenta_IdInexistente_LanzaNoEncontrado()
        {
            Assert.ThrowsAsync<RecursoNoEncontradoException>(() => _ventas.ObtenerAsync(999));
        }

        [Test]
        public async Task ListarVentas_FiltrandoPorProducto_SoloTraeEseProducto()
        {
            var (_, yogurt) = await HastaElDia3Async();

            var ventas = await _ventas.ListarAsync(null, null, TipoProducto.Yogurt);

            Assert.That(ventas.Select(v => v.Id), Is.EqualTo(new[] { yogurt.Id }));
        }

        [Test]
        public async Task ListarVentas_FiltrandoPorFechas_SoloTraeEseRango()
        {
            await HastaElDia3Async();
            var dia3 = new DateOnly(2026, 12, 3);

            Assert.That(await _ventas.ListarAsync(dia3, dia3, null), Has.Count.EqualTo(2));
            Assert.That(await _ventas.ListarAsync(new DateOnly(2026, 12, 4), null, null), Is.Empty);
        }

        [Test]
        public void ListarVentas_DesdePosteriorAHasta_LanzaReglaDeNegocio()
        {
            Assert.ThrowsAsync<ReglaDeNegocioException>(() =>
                _ventas.ListarAsync(new DateOnly(2026, 12, 5), new DateOnly(2026, 12, 1), null));
        }

        // ------------------------------------------------------------ HU-12 Lotes próximos a vencer

        [Test]
        public async Task ProximosAVencer_ConPlazoDe3Dias_TraeElQueVenceEn2DiasYNoElVencido()
        {
            // Después del día 3: Q1 agotado (vence 31 dic), Y1 con 30 L (vence 16 dic), Q2 con 13 kg (vence 1 ene).
            var (_, ventaYogurt) = await HastaElDia3Async();
            var y1 = ventaYogurt.Detalles.Single().LoteId;

            // 14 dic: Y1 vence en 2 días → aparece; Q2 vence en 18 días → no.
            var dia14 = await _inventario.ListarProximosAVencerAsync(3, new DateOnly(2026, 12, 14));
            Assert.That(dia14.Select(l => l.Lote.Id), Is.EqualTo(new[] { y1 }));

            // 17 dic: Y1 ya venció → no aparece.
            var dia17 = await _inventario.ListarProximosAVencerAsync(3, new DateOnly(2026, 12, 17));
            Assert.That(dia17, Is.Empty);
        }

        [Test]
        public async Task ProximosAVencer_UnLoteAgotado_NoAparece()
        {
            await HastaElDia3Async();

            // 29 dic con 3 días: Q1 (vence el 31) está agotado; solo sale Q2 (vence el 1 de enero).
            var lotes = await _inventario.ListarProximosAVencerAsync(3, new DateOnly(2026, 12, 29));

            Assert.That(lotes.Select(l => (l.Lote.TipoProducto, l.Lote.FechaVencimiento)),
                Is.EqualTo(new[] { (TipoProducto.Queso, new DateOnly(2027, 1, 1)) }));
        }

        [Test]
        public async Task ProximosAVencer_SinDias_UsaPorDefecto3()
        {
            await HastaElDia3Async();

            // 13 dic + 3 = 16 dic: entra Y1 justo en el límite.
            Assert.That(await _inventario.ListarProximosAVencerAsync(null, new DateOnly(2026, 12, 13)), Has.Count.EqualTo(1));
            // 12 dic + 3 = 15 dic: Y1 (vence el 16) queda por fuera.
            Assert.That(await _inventario.ListarProximosAVencerAsync(null, new DateOnly(2026, 12, 12)), Is.Empty);
        }

        [Test]
        public void ProximosAVencer_DiasNegativos_LanzaReglaDeNegocio()
        {
            Assert.ThrowsAsync<ReglaDeNegocioException>(() => _inventario.ListarProximosAVencerAsync(-1, null));
        }

        // ------------------------------------------------------------ HU-15 Reporte de ventas

        [Test]
        public async Task ReporteVentas_DelEscenario_TieneTotalesPorProductoYTotalGeneral()
        {
            await HastaElDia3Async();

            var reporte = await _reportes.VentasAsync(new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 3), null);

            // Cada venta: 12 kg × 28.000 y 20 L × 6.000
            Assert.That(reporte.Ventas.Select(v => (v.TipoProducto, v.Cantidad, v.PrecioUnitario, v.Total)),
                Is.EqualTo(new[]
                {
                    (TipoProducto.Queso, 12m, 28000m, 336000m),
                    (TipoProducto.Yogurt, 20m, 6000m, 120000m),
                }));
            Assert.That(reporte.TotalesPorProducto.Select(t => (t.TipoProducto, t.NumeroVentas, t.Total)),
                Is.EqualTo(new[] { (TipoProducto.Queso, 1, 336000m), (TipoProducto.Yogurt, 1, 120000m) }));
            Assert.That(reporte.TotalGeneral, Is.EqualTo(456000m));
        }

        [Test]
        public async Task ReporteVentas_FiltrandoPorProducto_SoloSumaEseProducto()
        {
            await HastaElDia3Async();

            var reporte = await _reportes.VentasAsync(null, null, TipoProducto.Queso);

            Assert.That(reporte.Ventas.Select(v => v.TipoProducto), Is.All.EqualTo(TipoProducto.Queso));
            Assert.That(reporte.TotalesPorProducto, Has.Count.EqualTo(1));
            Assert.That(reporte.TotalGeneral, Is.EqualTo(336000m));
        }

        [Test]
        public async Task ReporteVentas_SinVentasEnElRango_DevuelveListasVaciasYTotalCero()
        {
            await HastaElDia3Async();

            var reporte = await _reportes.VentasAsync(new DateOnly(2026, 12, 10), new DateOnly(2026, 12, 20), null);

            Assert.That(reporte.Ventas, Is.Empty);
            Assert.That(reporte.TotalesPorProducto, Is.Empty);
            Assert.That(reporte.TotalGeneral, Is.EqualTo(0m));
        }

        [Test]
        public void ReporteVentas_DesdePosteriorAHasta_LanzaReglaDeNegocio()
        {
            Assert.ThrowsAsync<ReglaDeNegocioException>(() =>
                _reportes.VentasAsync(new DateOnly(2026, 12, 5), new DateOnly(2026, 12, 1), null));
        }

        // ------------------------------------------------------------ HU-16 Reporte de leche por día

        [Test]
        public async Task ReporteLecheDiaria_DelEscenario_150LCadaDiaY0NoAprovechados()
        {
            await HastaElDia3Async();

            var reporte = await _reportes.LecheDiariaAsync(new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 3));

            // El día 3 no hubo ordeño: no aparece.
            Assert.That(reporte.Dias.Select(d => (d.Fecha, d.LitrosProducidos, d.LitrosTransformados, d.LitrosNoAprovechados)),
                Is.EqualTo(new[]
                {
                    (new DateOnly(2026, 12, 1), 150m, 150m, 0m),
                    (new DateOnly(2026, 12, 2), 150m, 150m, 0m),
                }));
            Assert.That(reporte.TotalProducido, Is.EqualTo(300m));
            Assert.That(reporte.TotalNoAprovechado, Is.EqualTo(0m));
        }

        [Test]
        public async Task ReporteLecheDiaria_LecheSinTransformar_ApareceComoNoAprovechada()
        {
            // Día 1: 150 L y solo 100 L a queso
            await _produccion.RegistrarAsync(new RegistrarProduccionRequest());
            NuevaPeticion();
            await _produccion.TransformarAsync(new() { TipoProducto = TipoProducto.Queso, Litros = 100m });
            NuevaPeticion();

            var reporte = await _reportes.LecheDiariaAsync(null, null);

            Assert.That(reporte.Dias.Single().LitrosNoAprovechados, Is.EqualTo(50m));
        }

        [Test]
        public async Task ReporteLecheDiaria_SinProduccion_DevuelveListaVaciaYTotalesCero()
        {
            var reporte = await _reportes.LecheDiariaAsync(new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 31));

            Assert.That(reporte.Dias, Is.Empty);
            Assert.That(reporte.TotalProducido, Is.EqualTo(0m));
        }

        [Test]
        public void ReporteLecheDiaria_DesdePosteriorAHasta_LanzaReglaDeNegocio()
        {
            Assert.ThrowsAsync<ReglaDeNegocioException>(() =>
                _reportes.LecheDiariaAsync(new DateOnly(2026, 12, 5), new DateOnly(2026, 12, 1)));
        }

        // ------------------------------------------------------------ HU-17 Reporte de producto por día

        [Test]
        public async Task ReporteProductoDiario_CoincideConElEscenario()
        {
            await HastaElDia3Async();

            var reporte = await _reportes.ProductoDiarioAsync(new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 31));

            // Día 1: 10 kg de queso y 50 L de yogurt; día 2: 15 kg de queso.
            // La cantidad es la elaborada: las ventas del día 3 no la cambian.
            Assert.That(reporte.Dias.Select(d => (d.Fecha, d.TipoProducto, d.Cantidad, d.Unidad, d.LitrosUsados)),
                Is.EqualTo(new (DateOnly?, TipoProducto, decimal, string, decimal)[]
                {
                    (new DateOnly(2026, 12, 1), TipoProducto.Queso, 10m, "kg", 100m),
                    (new DateOnly(2026, 12, 1), TipoProducto.Yogurt, 50m, "L", 50m),
                    (new DateOnly(2026, 12, 2), TipoProducto.Queso, 15m, "kg", 150m),
                }));
            Assert.That(reporte.TotalesPorProducto.Select(t => (t.TipoProducto, t.Cantidad, t.NumeroLotes)),
                Is.EqualTo(new[] { (TipoProducto.Queso, 25m, 2), (TipoProducto.Yogurt, 50m, 1) }));
        }

        [Test]
        public async Task ReporteProductoDiario_SinLotes_DevuelveListasVacias()
        {
            var reporte = await _reportes.ProductoDiarioAsync(null, null);

            Assert.That(reporte.Dias, Is.Empty);
            Assert.That(reporte.TotalesPorProducto, Is.Empty);
        }

        [Test]
        public void ReporteProductoDiario_DesdePosteriorAHasta_LanzaReglaDeNegocio()
        {
            Assert.ThrowsAsync<ReglaDeNegocioException>(() =>
                _reportes.ProductoDiarioAsync(new DateOnly(2026, 12, 5), new DateOnly(2026, 12, 1)));
        }

        // ------------------------------------------------------------ HU-18 Reporte de producto total

        [Test]
        public async Task ReporteProductoTotal_VencidoSinProcesar_NoCuentaComoDisponible()
        {
            // Día 17 sin procesar vencimientos: los 30 L de Y1 siguen en el lote pero ya no se venden.
            await HastaElDia3Async();
            _reloj.AvanzarDias(14);

            var yogurt = (await _reportes.ProductoTotalAsync()).Single(t => t.TipoProducto == TipoProducto.Yogurt);

            Assert.That((yogurt.Producido, yogurt.Vendido, yogurt.Disponible, yogurt.VencidoSinProcesar, yogurt.Dañado),
                Is.EqualTo((50m, 20m, 0m, 30m, 0m)));
        }

        [Test]
        public async Task ReporteProductoTotal_ConFecha_UsaEsaFechaParaLoVencido()
        {
            await HastaElDia3Async();   // el reloj queda en el día 3

            var dia3 = (await _reportes.ProductoTotalAsync()).Single(t => t.TipoProducto == TipoProducto.Yogurt);
            var dia17 = (await _reportes.ProductoTotalAsync(new DateOnly(2026, 12, 17))).Single(t => t.TipoProducto == TipoProducto.Yogurt);

            Assert.That((dia3.Disponible, dia3.VencidoSinProcesar), Is.EqualTo((30m, 0m)));
            Assert.That((dia17.Disponible, dia17.VencidoSinProcesar), Is.EqualTo((0m, 30m)));
        }

        // ------------------------------------------------------------ HU-20 Reporte de vacas

        [Test]
        public async Task ReporteVacas_ConLitrosDistintos_IdentificaMayorMenorYLasDeAbajo()
        {
            // Ejemplo de la HU: BaityCow (Id 1) da 20 L y la vaca 2 da 10 L.
            // Promedio por vaca = (20 + 10) ÷ 2 = 15 L; el hato dio 30 L ese día.
            await _produccion.RegistrarAsync(new RegistrarProduccionRequest
            {
                Vacas = { new() { VacaId = 1, Litros = 20m }, new() { VacaId = 2, Litros = 10m } },
            });
            NuevaPeticion();

            var reporte = await _reportes.VacasAsync(null, null);

            Assert.That(reporte.VacasEvaluadas, Is.EqualTo(2));
            Assert.That(reporte.PromedioPorVaca, Is.EqualTo(15m));
            Assert.That(reporte.PromedioDiarioHato, Is.EqualTo(30m));
            Assert.That((reporte.VacaMayor!.VacaId, reporte.VacaMayor.Nombre, reporte.VacaMayor.PromedioDiario),
                Is.EqualTo((1, "BaityCow", 20m)));
            Assert.That((reporte.VacaMenor!.VacaId, reporte.VacaMenor.PromedioDiario), Is.EqualTo((2, 10m)));
            Assert.That(reporte.PorDebajoDelPromedio.Select(v => (v.VacaId, v.Faltante)),
                Is.EqualTo(new[] { (2, 5m) }), "Solo la vaca 2 está por debajo: le faltan 5 L");
        }

        [Test]
        public async Task ReporteVacas_TodasIguales_NingunaQuedaPorDebajo()
        {
            await HastaElDia3Async();   // todas las vacas dan 15 L los días 1 y 2

            var reporte = await _reportes.VacasAsync(new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 3));

            Assert.That(reporte.VacasEvaluadas, Is.EqualTo(10));
            Assert.That(reporte.DiasConProduccion, Is.EqualTo(2));
            Assert.That(reporte.PromedioDiarioHato, Is.EqualTo(150m));
            Assert.That(reporte.PromedioPorVaca, Is.EqualTo(15m));
            Assert.That(reporte.PorDebajoDelPromedio, Is.Empty);
        }

        [Test]
        public async Task ReporteVacas_SinProduccionEnElRango_NoEvaluaVacas()
        {
            var reporte = await _reportes.VacasAsync(new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 31));

            Assert.That(reporte.VacasEvaluadas, Is.EqualTo(0));
            Assert.That(reporte.Vacas, Is.Empty);
            Assert.That(reporte.VacaMayor, Is.Null);
            Assert.That(reporte.VacaMenor, Is.Null);
        }

        [Test]
        public void ReporteVacas_DesdePosteriorAHasta_LanzaReglaDeNegocio()
        {
            Assert.ThrowsAsync<ReglaDeNegocioException>(() =>
                _reportes.VacasAsync(new DateOnly(2026, 12, 5), new DateOnly(2026, 12, 1)));
        }

        // ------------------------------------------------------------ HU-19 Reporte de producto dañado

        [Test]
        public async Task ReporteProductoDanado_DespuesDeProcesarElDia17_ApareceY1Con30L()
        {
            await HastaElDia3Async();
            _reloj.AvanzarDias(14);   // día 17

            // Antes de procesar, Y1 está vencido pero todavía no es "dañado".
            Assert.That((await _reportes.ProductoDanadoAsync(null, null, null)).Lotes, Is.Empty);

            await _inventario.ProcesarVencimientosAsync(null);
            NuevaPeticion();

            var reporte = await _reportes.ProductoDanadoAsync(new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 31), null);

            var y1 = reporte.Lotes.Single();
            Assert.That((y1.TipoProducto, y1.FechaProduccion, y1.FechaVencimiento, y1.CantidadDañada, y1.Unidad),
                Is.EqualTo((TipoProducto.Yogurt, new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 16), 30m, "L")));
            Assert.That(reporte.TotalesPorProducto.Select(t => (t.TipoProducto, t.Cantidad)),
                Is.EqualTo(new[] { (TipoProducto.Yogurt, 30m) }));
        }

        [Test]
        public async Task ReporteProductoDanado_FiltraPorFechaDeVencimientoYProducto()
        {
            await HastaElDia3Async();
            _reloj.AvanzarDias(14);
            await _inventario.ProcesarVencimientosAsync(null);
            NuevaPeticion();

            // Y1 venció el 16: no entra si el rango termina el 15, ni si se pide solo queso.
            Assert.That((await _reportes.ProductoDanadoAsync(null, new DateOnly(2026, 12, 15), null)).Lotes, Is.Empty);
            Assert.That((await _reportes.ProductoDanadoAsync(null, null, TipoProducto.Queso)).Lotes, Is.Empty);
        }

        [Test]
        public void ReporteProductoDanado_DesdePosteriorAHasta_LanzaReglaDeNegocio()
        {
            Assert.ThrowsAsync<ReglaDeNegocioException>(() =>
                _reportes.ProductoDanadoAsync(new DateOnly(2026, 12, 5), new DateOnly(2026, 12, 1), null));
        }

        [Test]
        public async Task ReporteProductoTotal_SinLotes_MuestraLosProductosEnCero()
        {
            var total = await _reportes.ProductoTotalAsync();

            Assert.That(total.Select(t => (t.TipoProducto, t.Unidad, t.Producido)),
                Is.EqualTo(new[] { (TipoProducto.Queso, "kg", 0m), (TipoProducto.Yogurt, "L", 0m) }));
        }
    }
}
