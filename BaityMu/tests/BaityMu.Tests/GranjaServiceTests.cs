using BaityMu.Domain.Entities;
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
    /// Pruebas de GranjaService con una base de datos InMemory: comprueban que el caso de
    /// uso completo (cargar, cambiar, guardar) funciona, no solo la regla del dominio.
    /// </summary>
    [TestFixture]
    public class GranjaServiceTests
    {
        private BaityMuDbContext _db = null!;
        private FakeClock _reloj = null!;
        private GranjaService _servicio = null!;

        [SetUp]
        public void SetUp()
        {
            _db = BaseDeDatosEnMemoria.Crear();
            _reloj = new FakeClock(new DateOnly(2026, 12, 1));
            _servicio = new GranjaService(new GranjaRepository(_db), new UnitOfWork(_db), _reloj);
        }

        [TearDown]
        public void TearDown() => _db.Dispose();

        /// <summary>Guarda una granja vacía y limpia el seguimiento, como en una petición nueva.</summary>
        private async Task SembrarGranjaAsync()
        {
            _db.Granjas.Add(new Granja("Granja BaityMu", 15m));
            await _db.SaveChangesAsync();
            _db.ChangeTracker.Clear();
        }

        [Test]
        public async Task AgregarVacaAsync_ConRazaValida_GuardaLaVacaConId()
        {
            // Arrange
            await SembrarGranjaAsync();

            // Act
            var vaca = await _servicio.AgregarVacaAsync(new CrearVacaRequest { Raza = "Holstein" });

            // Assert
            Assert.That(vaca.Id, Is.GreaterThan(0));
            Assert.That(vaca.Nombre, Is.EqualTo("BaityCow"));
            Assert.That(await _db.Vacas.CountAsync(), Is.EqualTo(1));
        }

        [Test]
        public async Task AgregarVacaAsync_SinFecha_UsaLaFechaDeHoy()
        {
            // Arrange
            await SembrarGranjaAsync();

            // Act
            var vaca = await _servicio.AgregarVacaAsync(new CrearVacaRequest { Raza = "Jersey" });

            // Assert
            Assert.That(vaca.FechaIngreso, Is.EqualTo(new DateOnly(2026, 12, 1)));
        }

        [Test]
        public async Task AgregarVacaAsync_ConRazaVacia_NoGuardaNada()
        {
            // Arrange
            await SembrarGranjaAsync();

            // Act y Assert
            Assert.ThrowsAsync<ReglaDeNegocioException>(() =>
                _servicio.AgregarVacaAsync(new CrearVacaRequest { Raza = "  " }));
            Assert.That(await _db.Vacas.CountAsync(), Is.EqualTo(0));
        }

        [Test]
        public async Task AgregarVacaAsync_AumentaNEnLaGranjaGuardada()
        {
            // Arrange
            await SembrarGranjaAsync();

            // Act
            await _servicio.AgregarVacaAsync(new CrearVacaRequest { Raza = "Holstein" });
            await _servicio.AgregarVacaAsync(new CrearVacaRequest { Raza = "Jersey" });

            // Assert: se vuelve a leer desde la base de datos
            _db.ChangeTracker.Clear();
            var granja = await new GranjaRepository(_db).ObtenerConVacasAsync();
            Assert.That(granja!.NumeroVacas, Is.EqualTo(2));
        }

        [Test]
        public void AgregarVacaAsync_SinGranja_LanzaNoEncontrado()
        {
            Assert.ThrowsAsync<RecursoNoEncontradoException>(() =>
                _servicio.AgregarVacaAsync(new CrearVacaRequest { Raza = "Holstein" }));
        }
    }
}
