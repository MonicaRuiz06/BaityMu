using BaityMu.Tests.Fakes;

namespace BaityMu.Tests
{
    [TestFixture]
    public class FakeClockTests
    {
        [Test]
        public void Today_DevuelveLaFechaDelConstructor()
        {
            var fechaInicial = new DateOnly(2026, 12, 1);

            var clock = new FakeClock(fechaInicial);

            Assert.That(clock.Today, Is.EqualTo(fechaInicial));
        }

        [Test]
        public void AvanzarDias_MueveLaFecha()
        {
            // Arrange
            var clock = new FakeClock(new DateOnly(2026, 12, 1));

            // Act
            clock.AvanzarDias(16);

            // Assert
            Assert.That(clock.Today, Is.EqualTo(new DateOnly(2026, 12, 17)));
        }
    }
}