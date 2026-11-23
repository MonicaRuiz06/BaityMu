using BaityMu.Domain.Entities;
using BaityMu.Domain.Exceptions;

namespace BaityMu.Tests
{
    [TestFixture]
    public class ProduccionLecheTests
    {
        [Test]
        public void Crear_ConLitrosValidos_GuardaLosDatos()
        {
            // Act
            var produccion = new ProduccionLeche(1, new DateOnly(2026, 12, 1), 15m);

            // Assert
            Assert.That(produccion.VacaId, Is.EqualTo(1));
            Assert.That(produccion.Litros, Is.EqualTo(15m));
        }

        [Test]
        public void Crear_ConLitrosNegativos_LanzaReglaDeNegocio()
        {
            Assert.Throws<ReglaDeNegocioException>(() =>
                new ProduccionLeche(1, new DateOnly(2026, 12, 1), -1m));
        }
    }
}
