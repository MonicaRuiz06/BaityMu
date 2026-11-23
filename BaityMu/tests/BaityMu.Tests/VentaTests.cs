using BaityMu.Domain.Entities;
using BaityMu.Domain.Enums;
using BaityMu.Domain.Exceptions;

namespace BaityMu.Tests
{
    /// <summary>
    /// Pruebas del dominio para la venta FIFO (HU-13). Incluye las cuatro pruebas mínimas
    /// del taller sobre ventas.
    /// </summary>
    [TestFixture]
    public class VentaTests
    {
        private static readonly DateOnly Dia1 = new(2026, 12, 1);
        private static readonly DateOnly Dia2 = new(2026, 12, 2);
        private static readonly DateOnly Dia3 = new(2026, 12, 3);

        private ParametroProducto _queso = null!;
        private ParametroProducto _yogurt = null!;
        private LoteProducto _q1 = null!;   // 10 kg del día 1
        private LoteProducto _q2 = null!;   // 15 kg del día 2
        private LoteProducto _y1 = null!;   // 50 L del día 1, vence el 16

        [SetUp]
        public void SetUp()
        {
            var granja = new Granja("Granja BaityMu", 15m);
            _queso = granja.AgregarProducto(TipoProducto.Queso, "kg", 10m, 30, 28000m);
            _yogurt = granja.AgregarProducto(TipoProducto.Yogurt, "L", 1m, 15, 6000m);

            _q1 = LoteProducto.Crear(_queso, Dia1, 100m, litrosProducidosEseDia: 150m, litrosYaTransformados: 0m);
            _y1 = LoteProducto.Crear(_yogurt, Dia1, 50m, litrosProducidosEseDia: 150m, litrosYaTransformados: 100m);
            _q2 = LoteProducto.Crear(_queso, Dia2, 150m, litrosProducidosEseDia: 150m, litrosYaTransformados: 0m);
        }

        private LoteProducto[] TodosLosLotes => new[] { _q2, _y1, _q1 }; // en desorden a propósito

        // ------------------------------------------------------------ Pruebas mínimas del taller

        [Test]
        public void Registrar_TomaDelLoteMasAntiguoAntesQueDelMasNuevo()
        {
            // Act: 4 kg caben en Q1
            var venta = Venta.Registrar(_queso, Dia3, 4m, TodosLosLotes);

            // Assert: sale de Q1 (día 1), no de Q2 (día 2)
            Assert.That(venta.Detalles, Has.Count.EqualTo(1));
            Assert.That(venta.Detalles.Single().Lote, Is.SameAs(_q1));
            Assert.That(_q1.CantidadDisponible, Is.EqualTo(6m));
            Assert.That(_q2.CantidadDisponible, Is.EqualTo(15m));
        }

        [Test]
        public void Registrar_QueExcedeUnLote_ConsumeElSiguienteYGeneraVariosDetalles()
        {
            // Act: escenario día 3, 12 kg de queso
            var venta = Venta.Registrar(_queso, Dia3, 12m, TodosLosLotes);

            // Assert: 10 kg de Q1 (agotado) + 2 kg de Q2 (quedan 13)
            Assert.That(venta.Detalles.Select(d => (d.Lote, d.Cantidad)),
                Is.EqualTo(new[] { (_q1, 10m), (_q2, 2m) }));
            Assert.That(_q1.CantidadDisponible, Is.EqualTo(0m));
            Assert.That(_q2.CantidadDisponible, Is.EqualTo(13m));
        }

        [Test]
        public void Registrar_UnLoteVencidoNuncaSeVende_AunqueSeaElMasAntiguo()
        {
            // Arrange: un yogurt viejo (día 1, vence el 16) y uno nuevo (día 10, vence el 25)
            var yNuevo = LoteProducto.Crear(_yogurt, new DateOnly(2026, 12, 10), 40m, 150m, 0m);

            // Act: venta del día 17
            var venta = Venta.Registrar(_yogurt, new DateOnly(2026, 12, 17), 5m, new[] { _y1, yNuevo });

            // Assert: sale del nuevo; Y1 no se toca
            Assert.That(venta.Detalles.Single().Lote, Is.SameAs(yNuevo));
            Assert.That(_y1.CantidadDisponible, Is.EqualTo(50m));
        }

        [Test]
        public void Registrar_ConExistenciasInsuficientes_NoModificaElInventario()
        {
            // Act: hay 25 kg de queso y se piden 30
            Assert.Throws<InventarioInsuficienteException>(() =>
                Venta.Registrar(_queso, Dia3, 30m, TodosLosLotes));

            // Assert: ningún lote cambió (RN-07)
            Assert.That(_q1.CantidadDisponible, Is.EqualTo(10m));
            Assert.That(_q2.CantidadDisponible, Is.EqualTo(15m));
        }

        // ------------------------------------------------------------ Otros criterios

        [Test]
        public void Registrar_CopiaElPrecioYCalculaElTotal()
        {
            var venta = Venta.Registrar(_queso, Dia3, 12m, TodosLosLotes);

            Assert.That(venta.PrecioUnitario, Is.EqualTo(28000m));
            Assert.That(venta.Total, Is.EqualTo(336000m));
            Assert.That(venta.Fecha, Is.EqualTo(Dia3));
            Assert.That(venta.TipoProducto, Is.EqualTo(TipoProducto.Queso));
        }

        [Test]
        public void Registrar_ElDiaDelVencimiento_TodaviaSeVende()
        {
            var venta = Venta.Registrar(_yogurt, new DateOnly(2026, 12, 16), 5m, TodosLosLotes);

            Assert.That(venta.Detalles.Single().Lote, Is.SameAs(_y1));
        }

        [Test]
        public void Registrar_UnLoteProducidoDespuesDeLaVenta_NoSeUsa()
        {
            // Criterio 8: el día 1 solo existe Q1 (10 kg); Q2 es del día 2
            Assert.Throws<InventarioInsuficienteException>(() =>
                Venta.Registrar(_queso, Dia1, 12m, TodosLosLotes));
            Assert.That(_q2.CantidadDisponible, Is.EqualTo(15m));
        }

        [Test]
        public void Registrar_SoloUsaLotesDelMismoProducto()
        {
            var venta = Venta.Registrar(_yogurt, Dia3, 20m, TodosLosLotes);

            Assert.That(venta.Detalles.Single().Lote, Is.SameAs(_y1));
            Assert.That(_q1.CantidadDisponible, Is.EqualTo(10m));
        }

        [TestCase(0)]
        [TestCase(-3)]
        public void Registrar_CantidadMenorOIgualACero_LanzaReglaDeNegocio(decimal cantidad)
        {
            Assert.Throws<ReglaDeNegocioException>(() =>
                Venta.Registrar(_queso, Dia3, cantidad, TodosLosLotes));
        }

        [Test]
        public void Registrar_VariasVentasElMismoDia_SeAceptanMientrasHayaExistencias()
        {
            // RN-05: no hay tope diario
            Venta.Registrar(_queso, Dia3, 10m, TodosLosLotes);
            Venta.Registrar(_queso, Dia3, 10m, TodosLosLotes);
            Venta.Registrar(_queso, Dia3, 5m, TodosLosLotes);

            Assert.That(_q1.CantidadDisponible + _q2.CantidadDisponible, Is.EqualTo(0m));
            Assert.Throws<InventarioInsuficienteException>(() => Venta.Registrar(_queso, Dia3, 1m, TodosLosLotes));
        }
    }
}
