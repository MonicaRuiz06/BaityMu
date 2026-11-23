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
    /// El reporte de producto total (Queso 25/12/13/0 · Yogurt 50/20/0/30) se agrega con la HU-18.
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
            _ventas = new VentaService(granjas, lotes, new VentaRepository(_db), unitOfWork, _reloj);
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

            // Resumen al día 17 (el reporte de producto total llega con la HU-18)
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
    }
}
