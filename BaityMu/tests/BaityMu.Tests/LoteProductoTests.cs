using BaityMu.Domain.Entities;
using BaityMu.Domain.Enums;
using BaityMu.Domain.Exceptions;

namespace BaityMu.Tests
{
    /// <summary>
    /// Pruebas del dominio para la transformación (HU-09). Incluye la prueba mínima del
    /// taller: no se pueden transformar más litros de los disponibles ese día (RN-02).
    /// </summary>
    [TestFixture]
    public class LoteProductoTests
    {
        private static readonly DateOnly Dia1 = new(2026, 12, 1);
        private ParametroProducto _queso = null!;
        private ParametroProducto _yogurt = null!;

        [SetUp]
        public void SetUp()
        {
            // Parámetros del taller: queso 10 L/kg y 30 días; yogurt 1 L/L y 15 días.
            var granja = new Granja("Granja BaityMu", 15m);
            _queso = granja.AgregarProducto(TipoProducto.Queso, "kg", 10m, 30, 28000m);
            _yogurt = granja.AgregarProducto(TipoProducto.Yogurt, "L", 1m, 15, 6000m);
        }

        [Test]
        public void Crear_100LitrosEnQueso_Da10Kilos()
        {
            // Act (RN-03: litros ÷ rendimiento)
            var lote = LoteProducto.Crear(_queso, Dia1, 100m, litrosProducidosEseDia: 150m, litrosYaTransformados: 0m);

            // Assert
            Assert.That(lote.TipoProducto, Is.EqualTo(TipoProducto.Queso));
            Assert.That(lote.CantidadInicial, Is.EqualTo(10m));
            Assert.That(lote.CantidadDisponible, Is.EqualTo(10m));
            Assert.That(lote.CantidadDanada, Is.EqualTo(0m));
            Assert.That(lote.LitrosUsados, Is.EqualTo(100m));
        }

        [Test]
        public void Crear_50LitrosEnYogurt_Da50LitrosYVenceA15Dias()
        {
            // Act
            var lote = LoteProducto.Crear(_yogurt, Dia1, 50m, litrosProducidosEseDia: 150m, litrosYaTransformados: 100m);

            // Assert: 2026-12-01 + 15 días = 2026-12-16 (RN-04)
            Assert.That(lote.CantidadInicial, Is.EqualTo(50m));
            Assert.That(lote.FechaProduccion, Is.EqualTo(Dia1));
            Assert.That(lote.FechaVencimiento, Is.EqualTo(new DateOnly(2026, 12, 16)));
        }

        [Test]
        public void Crear_MasLitrosDeLosDisponiblesEseDia_LanzaLecheInsuficiente()
        {
            // Prueba mínima del taller: de 150 L ya se transformaron 100 L; quedan 50 L.
            var error = Assert.Throws<LecheInsuficienteException>(() =>
                LoteProducto.Crear(_queso, Dia1, 60m, litrosProducidosEseDia: 150m, litrosYaTransformados: 100m));

            Assert.That(error!.Message, Does.Contain("solo quedan 50 L"));
        }

        [Test]
        public void Crear_ExactamenteLoQueQueda_SiSePermite()
        {
            var lote = LoteProducto.Crear(_yogurt, Dia1, 50m, litrosProducidosEseDia: 150m, litrosYaTransformados: 100m);

            Assert.That(lote.LitrosUsados, Is.EqualTo(50m));
        }

        [Test]
        public void Crear_SinProduccionEseDia_LanzaLecheInsuficiente()
        {
            Assert.Throws<LecheInsuficienteException>(() =>
                LoteProducto.Crear(_queso, Dia1, 10m, litrosProducidosEseDia: 0m, litrosYaTransformados: 0m));
        }

        [TestCase(0)]
        [TestCase(-5)]
        public void Crear_LitrosMenoresOIgualesACero_LanzaReglaDeNegocio(decimal litros)
        {
            Assert.Throws<ReglaDeNegocioException>(() =>
                LoteProducto.Crear(_queso, Dia1, litros, litrosProducidosEseDia: 150m, litrosYaTransformados: 0m));
        }

        [Test]
        public void Crear_LitrosQueNoDanDivisionExacta_RedondeaHaciaAbajo()
        {
            // 33.339 L ÷ 10 = 3.3339 kg → 3.333 kg (3 decimales, nunca hacia arriba)
            var lote = LoteProducto.Crear(_queso, Dia1, 33.339m, litrosProducidosEseDia: 150m, litrosYaTransformados: 0m);

            Assert.That(lote.CantidadInicial, Is.EqualTo(3.333m));
        }

        [Test]
        public void EstaVencido_ElDiaDelVencimientoNo_ElDiaSiguienteSi()
        {
            // Arrange: vence el 2026-12-16
            var lote = LoteProducto.Crear(_yogurt, Dia1, 50m, litrosProducidosEseDia: 150m, litrosYaTransformados: 0m);

            // Assert (RN-08: vencido cuando fecha > vencimiento)
            Assert.That(lote.EstaVencido(new DateOnly(2026, 12, 16)), Is.False);
            Assert.That(lote.EstaVencido(new DateOnly(2026, 12, 17)), Is.True);
        }

        // ----------------------------------------------------------------------------
        // HU-11 Procesar vencimientos
        // ----------------------------------------------------------------------------

        [Test]
        public void MarcarComoDanado_LoteVencido_PasaLoDisponibleADanado()
        {
            // Arrange: Y1 vence el 2026-12-16
            var y1 = LoteProducto.Crear(_yogurt, Dia1, 50m, litrosProducidosEseDia: 150m, litrosYaTransformados: 0m);

            // Act
            var danado = y1.MarcarComoDanado(new DateOnly(2026, 12, 17));

            // Assert (RN-08)
            Assert.That(danado, Is.EqualTo(50m));
            Assert.That(y1.CantidadDisponible, Is.EqualTo(0m));
            Assert.That(y1.CantidadDanada, Is.EqualTo(50m));
            Assert.That(y1.ObtenerEstado(new DateOnly(2026, 12, 17)), Is.EqualTo(EstadoLote.Dañado));
        }

        [Test]
        public void MarcarComoDanado_ElMismoDiaDelVencimiento_LanzaReglaDeNegocio()
        {
            // Criterio 3: el día del vencimiento todavía no está vencido
            var y1 = LoteProducto.Crear(_yogurt, Dia1, 50m, litrosProducidosEseDia: 150m, litrosYaTransformados: 0m);

            Assert.Throws<ReglaDeNegocioException>(() => y1.MarcarComoDanado(new DateOnly(2026, 12, 16)));
            Assert.That(y1.CantidadDisponible, Is.EqualTo(50m));
        }

        [Test]
        public void MarcarComoDanado_DosVeces_NoDuplicaLoDanado()
        {
            var y1 = LoteProducto.Crear(_yogurt, Dia1, 50m, litrosProducidosEseDia: 150m, litrosYaTransformados: 0m);
            var dia17 = new DateOnly(2026, 12, 17);
            y1.MarcarComoDanado(dia17);

            var segundaVez = y1.MarcarComoDanado(dia17);

            Assert.That(segundaVez, Is.EqualTo(0m));
            Assert.That(y1.CantidadDanada, Is.EqualTo(50m));
        }
    }
}
