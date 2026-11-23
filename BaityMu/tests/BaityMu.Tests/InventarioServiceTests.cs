using BaityMu.Domain.Entities;
using BaityMu.Domain.Enums;
using BaityMu.Domain.Exceptions;
using BaityMu.Infrastructure.Data;
using BaityMu.Infrastructure.Repositories;
using BaityMu.Services;
using BaityMu.Services.Dtos;
using BaityMu.Tests.Fakes;
using BaityMu.Tests.Infraestructura;

namespace BaityMu.Tests
{
    /// <summary>
    /// Pruebas del inventario (HU-10) con los lotes del escenario de referencia:
    /// Q1 = 10 kg (día 1), Y1 = 50 L (día 1, vence el 16) y Q2 = 15 kg (día 2).
    /// </summary>
    [TestFixture]
    public class InventarioServiceTests
    {
        private static readonly DateOnly Dia1 = new(2026, 12, 1);
        private static readonly DateOnly Dia2 = new(2026, 12, 2);
        private static readonly DateOnly Dia3 = new(2026, 12, 3);

        private BaityMuDbContext _db = null!;
        private InventarioService _inventario = null!;
        private LoteProducto _q1 = null!;
        private LoteProducto _y1 = null!;
        private LoteProducto _q2 = null!;

        [SetUp]
        public async Task SetUp()
        {
            _db = BaseDeDatosEnMemoria.Crear();
            var reloj = new FakeClock(Dia3);
            var lotes = new LoteRepository(_db);
            var produccion = new ProduccionService(
                new GranjaRepository(_db), new ProduccionRepository(_db), lotes, new UnitOfWork(_db), reloj);
            _inventario = new InventarioService(lotes, new UnitOfWork(_db), reloj);

            var granja = new Granja("Granja BaityMu", 15m);
            for (var i = 0; i < 10; i++)
                granja.AgregarVaca("Holstein", null, Dia1);
            granja.AgregarProducto(TipoProducto.Queso, "kg", 10m, 30, 28000m);
            granja.AgregarProducto(TipoProducto.Yogurt, "L", 1m, 15, 6000m);
            _db.Granjas.Add(granja);
            await _db.SaveChangesAsync();
            _db.ChangeTracker.Clear();

            await produccion.RegistrarAsync(new RegistrarProduccionRequest { Fecha = Dia1 });
            await produccion.RegistrarAsync(new RegistrarProduccionRequest { Fecha = Dia2 });
            _q1 = await produccion.TransformarAsync(new() { Fecha = Dia1, TipoProducto = TipoProducto.Queso, Litros = 100m });
            _y1 = await produccion.TransformarAsync(new() { Fecha = Dia1, TipoProducto = TipoProducto.Yogurt, Litros = 50m });
            _q2 = await produccion.TransformarAsync(new() { Fecha = Dia2, TipoProducto = TipoProducto.Queso, Litros = 150m });
            _db.ChangeTracker.Clear();
        }

        [TearDown]
        public void TearDown() => _db.Dispose();

        /// <summary>Cambia las cantidades de un lote directamente en la base de prueba
        /// (vender y procesar vencimientos llegan con la HU-11 y la HU-13).</summary>
        private async Task CambiarCantidadesAsync(LoteProducto lote, decimal disponible, decimal danada)
        {
            _db.Attach(lote);
            _db.Entry(lote).Property(l => l.CantidadDisponible).CurrentValue = disponible;
            _db.Entry(lote).Property(l => l.CantidadDanada).CurrentValue = danada;
            await _db.SaveChangesAsync();
            _db.ChangeTracker.Clear();
        }

        [Test]
        public async Task ListarLotesAsync_SinFiltros_DevuelveTodosEnOrdenFifo()
        {
            // Act
            var lotes = await _inventario.ListarLotesAsync(null, null, null, Dia3);

            // Assert: queso (Q1, Q2) y luego yogurt (Y1); todos disponibles el día 3
            Assert.That(lotes.Select(l => l.Lote.Id), Is.EqualTo(new[] { _q1.Id, _q2.Id, _y1.Id }));
            Assert.That(lotes.All(l => l.Estado == EstadoLote.Disponible), Is.True);
        }

        [Test]
        public async Task ListarLotesAsync_FiltrandoYogurt_SoloTraeYogurt()
        {
            var lotes = await _inventario.ListarLotesAsync(TipoProducto.Yogurt, null, null, Dia3);

            Assert.That(lotes.Select(l => l.Lote.Id), Is.EqualTo(new[] { _y1.Id }));
        }

        [Test]
        public async Task ListarLotesAsync_FiltrandoPorFechaDeProduccion_SoloTraeEseRango()
        {
            var lotes = await _inventario.ListarLotesAsync(null, Dia2, Dia2, Dia3);

            Assert.That(lotes.Select(l => l.Lote.Id), Is.EqualTo(new[] { _q2.Id }));
        }

        [Test]
        public void ListarLotesAsync_DesdePosteriorAHasta_LanzaReglaDeNegocio()
        {
            Assert.ThrowsAsync<ReglaDeNegocioException>(() =>
                _inventario.ListarLotesAsync(null, Dia2, Dia1, Dia3));
        }

        [Test]
        public async Task ObtenerLoteAsync_Y1ElDia16_SigueDisponible()
        {
            // Borde: el día del vencimiento todavía se puede vender
            var y1 = await _inventario.ObtenerLoteAsync(_y1.Id, new DateOnly(2026, 12, 16));

            Assert.That(y1.Estado, Is.EqualTo(EstadoLote.Disponible));
        }

        [Test]
        public async Task ObtenerLoteAsync_Y1ElDia17_EstaVencido()
        {
            var y1 = await _inventario.ObtenerLoteAsync(_y1.Id, new DateOnly(2026, 12, 17));

            Assert.That(y1.Estado, Is.EqualTo(EstadoLote.Vencido));
        }

        [Test]
        public async Task ObtenerLoteAsync_SinExistenciasNiDanado_EstaAgotado()
        {
            await CambiarCantidadesAsync(_q1, disponible: 0m, danada: 0m);

            var q1 = await _inventario.ObtenerLoteAsync(_q1.Id, Dia3);

            Assert.That(q1.Estado, Is.EqualTo(EstadoLote.Agotado));
        }

        [Test]
        public async Task ObtenerLoteAsync_ConTodoLoQueQuedabaDanado_EstaDanado()
        {
            // Escenario día 17: de Y1 se vendieron 20 L y los 30 L restantes pasaron a dañado
            await CambiarCantidadesAsync(_y1, disponible: 0m, danada: 30m);

            var y1 = await _inventario.ObtenerLoteAsync(_y1.Id, new DateOnly(2026, 12, 17));

            Assert.That(y1.Estado, Is.EqualTo(EstadoLote.Dañado));
        }

        [Test]
        public void ObtenerLoteAsync_IdInexistente_LanzaNoEncontrado()
        {
            Assert.ThrowsAsync<RecursoNoEncontradoException>(() => _inventario.ObtenerLoteAsync(999, Dia3));
        }

        [Test]
        public async Task ObtenerResumenAsync_Dia3_TodoEsVendible()
        {
            var resumen = await _inventario.ObtenerResumenAsync(Dia3);

            var queso = resumen.Single(r => r.TipoProducto == TipoProducto.Queso);
            var yogurt = resumen.Single(r => r.TipoProducto == TipoProducto.Yogurt);
            Assert.That(queso.CantidadVendible, Is.EqualTo(25m));
            Assert.That(yogurt.CantidadVendible, Is.EqualTo(50m));
            Assert.That(yogurt.CantidadVencidaSinProcesar, Is.EqualTo(0m));
        }

        [Test]
        public async Task ObtenerResumenAsync_Dia17_ElYogurtEstaVencidoSinProcesar()
        {
            var resumen = await _inventario.ObtenerResumenAsync(new DateOnly(2026, 12, 17));

            var queso = resumen.Single(r => r.TipoProducto == TipoProducto.Queso);
            var yogurt = resumen.Single(r => r.TipoProducto == TipoProducto.Yogurt);
            Assert.That(queso.CantidadVendible, Is.EqualTo(25m), "El queso vence a los 30 días");
            Assert.That(yogurt.CantidadVendible, Is.EqualTo(0m));
            Assert.That(yogurt.CantidadVencidaSinProcesar, Is.EqualTo(50m));
            Assert.That(yogurt.CantidadDañada, Is.EqualTo(0m));
        }

        // ----------------------------------------------------------------------------
        // HU-11 Procesar vencimientos
        // ----------------------------------------------------------------------------

        private static readonly DateOnly Dia16 = new(2026, 12, 16);
        private static readonly DateOnly Dia17 = new(2026, 12, 17);

        [Test]
        public async Task ProcesarVencimientosAsync_Dia17_LosTreintaLitrosDeY1PasanADanadoYElResumenLoRefleja()
        {
            // Prueba mínima del taller. Arrange: el día 3 se vendieron 20 L de Y1 y quedan 30 L
            // (la venta llega con la HU-13; aquí se deja Y1 en 30 L directamente).
            await CambiarCantidadesAsync(_y1, disponible: 30m, danada: 0m);

            // Act
            var resultado = await _inventario.ProcesarVencimientosAsync(Dia17);

            // Assert: el resultado
            Assert.That(resultado.Lotes, Has.Count.EqualTo(1));
            Assert.That(resultado.Lotes[0].Lote.Id, Is.EqualTo(_y1.Id));
            Assert.That(resultado.Lotes[0].CantidadDañada, Is.EqualTo(30m));
            Assert.That(resultado.TotalesPorProducto.Single(t => t.TipoProducto == TipoProducto.Yogurt).CantidadDañada, Is.EqualTo(30m));
            Assert.That(resultado.TotalesPorProducto.Single(t => t.TipoProducto == TipoProducto.Queso).CantidadDañada, Is.EqualTo(0m));

            // Assert: lo guardado y el resumen del inventario
            _db.ChangeTracker.Clear();
            var y1 = await _inventario.ObtenerLoteAsync(_y1.Id, Dia17);
            Assert.That(y1.Lote.CantidadDisponible, Is.EqualTo(0m));
            Assert.That(y1.Lote.CantidadDanada, Is.EqualTo(30m));
            Assert.That(y1.Estado, Is.EqualTo(EstadoLote.Dañado));

            var yogurt = (await _inventario.ObtenerResumenAsync(Dia17)).Single(r => r.TipoProducto == TipoProducto.Yogurt);
            Assert.That(yogurt.CantidadDañada, Is.EqualTo(30m));
            Assert.That(yogurt.CantidadVencidaSinProcesar, Is.EqualTo(0m));
        }

        [Test]
        public async Task ProcesarVencimientosAsync_ElDiaDelVencimiento_NoDanaNada()
        {
            // Criterio 3: Y1 vence el 16; ese día todavía se vende
            var resultado = await _inventario.ProcesarVencimientosAsync(Dia16);

            Assert.That(resultado.Lotes, Is.Empty);
            _db.ChangeTracker.Clear();
            Assert.That((await _inventario.ObtenerLoteAsync(_y1.Id, Dia16)).Lote.CantidadDisponible, Is.EqualTo(50m));
        }

        [Test]
        public async Task ProcesarVencimientosAsync_DosVecesLaMismaFecha_NoDuplicaLoDanado()
        {
            // Arrange
            await _inventario.ProcesarVencimientosAsync(Dia17);
            _db.ChangeTracker.Clear();

            // Act
            var segunda = await _inventario.ProcesarVencimientosAsync(Dia17);

            // Assert
            Assert.That(segunda.Lotes, Is.Empty);
            _db.ChangeTracker.Clear();
            Assert.That((await _inventario.ObtenerLoteAsync(_y1.Id, Dia17)).Lote.CantidadDanada, Is.EqualTo(50m));
        }

        [Test]
        public async Task ProcesarVencimientosAsync_LoteAgotado_NoSeModifica()
        {
            // Arrange: Y1 se vendió completo
            await CambiarCantidadesAsync(_y1, disponible: 0m, danada: 0m);

            // Act
            var resultado = await _inventario.ProcesarVencimientosAsync(Dia17);

            // Assert: criterio 4
            Assert.That(resultado.Lotes, Is.Empty);
            _db.ChangeTracker.Clear();
            var y1 = await _inventario.ObtenerLoteAsync(_y1.Id, Dia17);
            Assert.That(y1.Lote.CantidadDanada, Is.EqualTo(0m));
            Assert.That(y1.Estado, Is.EqualTo(EstadoLote.Agotado));
        }

        [Test]
        public async Task ProcesarVencimientosAsync_ElQuesoDelEscenario_NoVenceElDia17()
        {
            var resultado = await _inventario.ProcesarVencimientosAsync(Dia17);

            Assert.That(resultado.Lotes.Select(l => l.Lote.TipoProducto), Is.All.EqualTo(TipoProducto.Yogurt));
        }
    }
}
