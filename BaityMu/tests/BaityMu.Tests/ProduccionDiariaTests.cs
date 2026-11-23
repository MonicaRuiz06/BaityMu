using BaityMu.Domain.Entities;

namespace BaityMu.Tests
{
    /// <summary>
    /// Pruebas del dominio para la producción diaria (HU-07, RN-01), sin base de datos.
    /// </summary>
    [TestFixture]
    public class ProduccionDiariaTests
    {
        private static readonly DateOnly Dia1 = new(2026, 12, 1);
        private static readonly IReadOnlyDictionary<int, decimal> SinLitrosEspecificos = new Dictionary<int, decimal>();
        private static readonly IReadOnlySet<int> NingunaRegistrada = new HashSet<int>();

        [Test]
        public void GenerarProduccionDiaria_DiezVacasDeQuinceLitros_Genera150Litros()
        {
            // Arrange
            var granja = new Granja("Granja BaityMu", 15m);
            for (var i = 0; i < 10; i++)
                granja.AgregarVaca("Holstein", null, Dia1);

            // Act
            var producciones = granja.GenerarProduccionDiaria(Dia1, SinLitrosEspecificos, NingunaRegistrada);

            // Assert: n × l
            Assert.That(producciones, Has.Count.EqualTo(10));
            Assert.That(producciones.Sum(p => p.Litros), Is.EqualTo(150m));
            Assert.That(producciones.All(p => p.Fecha == Dia1), Is.True);
        }

        [Test]
        public void GenerarProduccionDiaria_ConOtroL_UsaElLDeLaGranja()
        {
            // Arrange: l = 20
            var granja = new Granja("Granja BaityMu", 20m);
            for (var i = 0; i < 3; i++)
                granja.AgregarVaca("Jersey", null, Dia1);

            // Act
            var producciones = granja.GenerarProduccionDiaria(Dia1, SinLitrosEspecificos, NingunaRegistrada);

            // Assert: 3 × 20 = 60
            Assert.That(producciones.Sum(p => p.Litros), Is.EqualTo(60m));
        }
    }
}
