using BaityMu.Infrastructure.Data;
using BaityMu.Tests.Infraestructura;

namespace BaityMu.Tests
{
    [TestFixture]
    public class UnitOfWorkTests
    {
        [Test]
        public async Task SaveChangesAsync_SinCambios_DevuelveCero()
        {
            // Arrange
            await using var contexto = BaseDeDatosEnMemoria.Crear();
            var unitOfWork = new UnitOfWork(contexto);

            // Act
            var guardados = await unitOfWork.SaveChangesAsync();

            // Assert
            Assert.That(guardados, Is.EqualTo(0));
        }
    }
}