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
            _servicio = new GranjaService(
                new GranjaRepository(_db), new ProduccionRepository(_db), new UnitOfWork(_db), _reloj);
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

        // ----------------------------------------------------------------------------
        // HU-02 Ver el listado de vacas
        // ----------------------------------------------------------------------------

        /// <summary>Registra dos vacas: BaityCow (Id 1) y Lola (Id 2).</summary>
        private async Task<(Vaca BaityCow, Vaca Lola)> SembrarDosVacasAsync()
        {
            await SembrarGranjaAsync();
            var baityCow = await _servicio.AgregarVacaAsync(new CrearVacaRequest { Raza = "Holstein" });
            var lola = await _servicio.AgregarVacaAsync(new CrearVacaRequest { Raza = "Jersey", Nombre = "Lola" });
            return (baityCow, lola);
        }

        [Test]
        public async Task ListarVacasAsync_ConProduccion_SumaLosLitrosDeCadaVaca()
        {
            // Arrange: BaityCow da 15 L el día 1 y 14.5 L el día 2
            var (baityCow, _) = await SembrarDosVacasAsync();
            _db.ProduccionesLeche.AddRange(
                new ProduccionLeche(baityCow.Id, new DateOnly(2026, 12, 1), 15m),
                new ProduccionLeche(baityCow.Id, new DateOnly(2026, 12, 2), 14.5m));
            await _db.SaveChangesAsync();

            // Act
            var vacas = await _servicio.ListarVacasAsync(incluirRetiradas: false);

            // Assert
            Assert.That(vacas, Has.Count.EqualTo(2));
            Assert.That(vacas[0].Vaca.Nombre, Is.EqualTo("BaityCow"));
            Assert.That(vacas[0].LitrosProducidos, Is.EqualTo(29.5m));
        }

        [Test]
        public async Task ListarVacasAsync_VacaSinOrdenar_SaleConCeroLitros()
        {
            // Arrange
            await SembrarDosVacasAsync();

            // Act
            var vacas = await _servicio.ListarVacasAsync(incluirRetiradas: false);

            // Assert
            Assert.That(vacas[1].Vaca.Nombre, Is.EqualTo("Lola"));
            Assert.That(vacas[1].LitrosProducidos, Is.EqualTo(0m));
        }

        [Test]
        public async Task ListarVacasAsync_SinVacas_DevuelveListaVacia()
        {
            // Arrange
            await SembrarGranjaAsync();

            // Act
            var vacas = await _servicio.ListarVacasAsync(incluirRetiradas: false);

            // Assert
            Assert.That(vacas, Is.Empty);
        }

        [Test]
        public async Task ListarVacasAsync_ConVacaRetirada_SoloApareceSiSePide()
        {
            // Arrange: se retira a Lola cambiando Activa directamente en la base de datos,
            // porque Granja.RetirarVaca llega con la HU-04.
            var (_, lola) = await SembrarDosVacasAsync();
            _db.Entry(lola).Property(v => v.Activa).CurrentValue = false;
            await _db.SaveChangesAsync();
            _db.ChangeTracker.Clear();

            // Act
            var activas = await _servicio.ListarVacasAsync(incluirRetiradas: false);
            var todas = await _servicio.ListarVacasAsync(incluirRetiradas: true);

            // Assert
            Assert.That(activas.Select(v => v.Vaca.Nombre), Is.EqualTo(new[] { "BaityCow" }));
            Assert.That(todas, Has.Count.EqualTo(2));
            Assert.That(todas.Single(v => v.Vaca.Nombre == "Lola").Vaca.Activa, Is.False);
        }
    
        // ----------------------------------------------------------------------------
        // HU-03 Consultar una vaca
        // ----------------------------------------------------------------------------

        [Test]
        public async Task ObtenerVacaAsync_VacaExistente_DevuelveSuHistorialOrdenado()
        {
            // Arrange: se guardan los días en desorden a propósito
            var (baityCow, lola) = await SembrarDosVacasAsync();
            _db.ProduccionesLeche.AddRange(
                new ProduccionLeche(baityCow.Id, new DateOnly(2026, 12, 2), 14m),
                new ProduccionLeche(baityCow.Id, new DateOnly(2026, 12, 1), 15m),
                new ProduccionLeche(lola.Id, new DateOnly(2026, 12, 1), 20m));
            await _db.SaveChangesAsync();

            // Act
            var detalle = await _servicio.ObtenerVacaAsync(baityCow.Id);

            // Assert: solo los días de BaityCow, del más antiguo al más reciente
            Assert.That(detalle.Vaca.Nombre, Is.EqualTo("BaityCow"));
            Assert.That(detalle.Historial.Select(p => p.Fecha),
                Is.EqualTo(new[] { new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 2) }));
            Assert.That(detalle.LitrosProducidos, Is.EqualTo(29m));
        }

        [Test]
        public async Task ObtenerVacaAsync_SinOrdenar_TieneHistorialVacioYCeroLitros()
        {
            // Arrange
            var (_, lola) = await SembrarDosVacasAsync();

            // Act
            var detalle = await _servicio.ObtenerVacaAsync(lola.Id);

            // Assert
            Assert.That(detalle.Historial, Is.Empty);
            Assert.That(detalle.LitrosProducidos, Is.EqualTo(0m));
        }

        [Test]
        public async Task ObtenerVacaAsync_IdInexistente_LanzaNoEncontrado()
        {
            // Arrange
            await SembrarDosVacasAsync();

            // Act y Assert
            var error = Assert.ThrowsAsync<RecursoNoEncontradoException>(() =>
                _servicio.ObtenerVacaAsync(99));
            Assert.That(error!.Message, Is.EqualTo("No existe una vaca con id 99."));
        }

        // ----------------------------------------------------------------------------
        // HU-04 Retirar una vaca
        // ----------------------------------------------------------------------------

        [Test]
        public async Task RetirarVacaAsync_VacaActiva_BajaNYQuedaInactiva()
        {
            // Arrange
            var (_, lola) = await SembrarDosVacasAsync();

            // Act
            await _servicio.RetirarVacaAsync(lola.Id);

            // Assert: se vuelve a leer desde la base de datos
            _db.ChangeTracker.Clear();
            var granja = await new GranjaRepository(_db).ObtenerConVacasAsync();
            Assert.That(granja!.NumeroVacas, Is.EqualTo(1));
            Assert.That(granja.Vacas.Single(v => v.Id == lola.Id).Activa, Is.False);
            Assert.That(await _db.Vacas.CountAsync(), Is.EqualTo(2), "La vaca no se borra");
        }

        [Test]
        public async Task RetirarVacaAsync_ConservaElHistorialDeProduccion()
        {
            // Arrange
            var (_, lola) = await SembrarDosVacasAsync();
            _db.ProduccionesLeche.Add(new ProduccionLeche(lola.Id, new DateOnly(2026, 12, 1), 20m));
            await _db.SaveChangesAsync();

            // Act
            await _servicio.RetirarVacaAsync(lola.Id);

            // Assert
            var detalle = await _servicio.ObtenerVacaAsync(lola.Id);
            Assert.That(detalle.Vaca.Activa, Is.False);
            Assert.That(detalle.LitrosProducidos, Is.EqualTo(20m));
        }

        [Test]
        public async Task RetirarVacaAsync_UltimaVacaActiva_LanzaReglaDeNegocio()
        {
            // Arrange: se retira a Lola y queda solo BaityCow
            var (baityCow, lola) = await SembrarDosVacasAsync();
            await _servicio.RetirarVacaAsync(lola.Id);

            // Act y Assert
            Assert.ThrowsAsync<ReglaDeNegocioException>(() =>
                _servicio.RetirarVacaAsync(baityCow.Id));
        }

        [Test]
        public async Task RetirarVacaAsync_IdInexistente_LanzaNoEncontrado()
        {
            // Arrange
            await SembrarDosVacasAsync();

            // Act y Assert
            Assert.ThrowsAsync<RecursoNoEncontradoException>(() =>
                _servicio.RetirarVacaAsync(99));
        }

        [Test]
        public async Task RetirarVacaAsync_YaRetirada_LanzaConflicto()
        {
            // Arrange: tres vacas, para que retirar a Lola no deje solo una
            var (_, lola) = await SembrarDosVacasAsync();
            await _servicio.AgregarVacaAsync(new CrearVacaRequest { Raza = "Normando" });
            await _servicio.RetirarVacaAsync(lola.Id);

            // Act y Assert
            Assert.ThrowsAsync<ConflictoException>(() =>
                _servicio.RetirarVacaAsync(lola.Id));
        }
    }
}
