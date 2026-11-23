using BaityMu.Domain.Entities;
using BaityMu.Domain.Enums;
using BaityMu.Domain.Exceptions;

namespace BaityMu.Tests
{
    /// <summary>
    /// Pruebas de la configuración de la granja (HU-05): n × l y parámetros de producto.
    /// </summary>
    [TestFixture]
    public class ConfiguracionGranjaTests
    {
        private Granja _granja = null!;
        private readonly DateOnly _fecha = new DateOnly(2026, 12, 1);

        [SetUp]
        public void SetUp()
        {
            _granja = new Granja("Granja BaityMu", 15m);
        }

        [Test]
        public void ProduccionDiariaEsperada_DiezVacasDeQuinceLitros_Es150()
        {
            // Arrange
            for (var i = 0; i < 10; i++)
                _granja.AgregarVaca("Holstein", null, _fecha);

            // Act y Assert: escenario de referencia del taller
            Assert.That(_granja.ProduccionDiariaEsperada, Is.EqualTo(150m));
        }

        [Test]
        public void ProduccionDiariaEsperada_SinVacas_EsCero()
        {
            Assert.That(_granja.ProduccionDiariaEsperada, Is.EqualTo(0m));
        }

        [Test]
        public void AgregarProducto_ConValoresValidos_QuedaConfigurado()
        {
            // Act
            _granja.AgregarProducto(TipoProducto.Queso, "kg", 10m, 30, 28000m);

            // Assert
            var queso = _granja.ObtenerProducto(TipoProducto.Queso);
            Assert.That(queso.LitrosPorUnidad, Is.EqualTo(10m));
            Assert.That(queso.VidaUtilDias, Is.EqualTo(30));
            Assert.That(queso.PrecioUnitario, Is.EqualTo(28000m));
        }

        [TestCase(0, 30, 28000, TestName = "AgregarProducto_RendimientoCero_LanzaReglaDeNegocio")]
        [TestCase(10, 0, 28000, TestName = "AgregarProducto_VidaUtilCero_LanzaReglaDeNegocio")]
        [TestCase(10, 30, -1, TestName = "AgregarProducto_PrecioNegativo_LanzaReglaDeNegocio")]
        public void AgregarProducto_ConValorInvalido_LanzaReglaDeNegocio(decimal litros, int vidaUtil, decimal precio)
        {
            Assert.Throws<ReglaDeNegocioException>(() =>
                _granja.AgregarProducto(TipoProducto.Queso, "kg", litros, vidaUtil, precio));
            Assert.That(_granja.Productos, Is.Empty, "No se agrega nada");
        }

        [Test]
        public void AgregarProducto_Repetido_LanzaConflicto()
        {
            // Arrange
            _granja.AgregarProducto(TipoProducto.Yogurt, "L", 1m, 15, 6000m);

            // Act y Assert
            Assert.Throws<ConflictoException>(() =>
                _granja.AgregarProducto(TipoProducto.Yogurt, "L", 1m, 15, 6000m));
        }

        [Test]
        public void ObtenerProducto_NoConfigurado_LanzaNoEncontrado()
        {
            Assert.Throws<RecursoNoEncontradoException>(() =>
                _granja.ObtenerProducto(TipoProducto.Queso));
        }
    }
}
