using BaityMu.Domain.Entities;
using BaityMu.Domain.Enums;
using BaityMu.Infrastructure.Data;
using BaityMu.Infrastructure.Seed;
using BaityMu.Tests.Fakes;
using BaityMu.Tests.Infraestructura;
using Microsoft.EntityFrameworkCore;

namespace BaityMu.Tests
{
    /// <summary>
    /// Pruebas de la siembra de la granja desde appsettings.json (HU-05, RNF-09).
    /// </summary>
    [TestFixture]
    public class SembradorDeDatosTests
    {
        private BaityMuDbContext _db = null!;
        private readonly FakeClock _reloj = new(new DateOnly(2026, 12, 1));

        [SetUp]
        public void SetUp() => _db = BaseDeDatosEnMemoria.Crear();

        [TearDown]
        public void TearDown() => _db.Dispose();

        /// <summary>Los mismos valores de appsettings.json.</summary>
        private static GranjaInicialOptions OpcionesDelTaller() => new()
        {
            Nombre = "Granja BaityMu",
            NumeroVacas = 10,
            LitrosPorVaca = 15m,
            Productos =
            {
                new() { TipoProducto = TipoProducto.Queso, Unidad = "kg", LitrosPorUnidad = 10m, VidaUtilDias = 30, PrecioUnitario = 28000m },
                new() { TipoProducto = TipoProducto.Yogurt, Unidad = "L", LitrosPorUnidad = 1m, VidaUtilDias = 15, PrecioUnitario = 6000m }
            }
        };

        private async Task<Granja> LeerGranjaAsync()
        {
            _db.ChangeTracker.Clear();
            return await _db.Granjas.Include(g => g.Vacas).Include(g => g.Productos).SingleAsync();
        }

        [Test]
        public async Task SembrarAsync_BaseVacia_CreaLaGranjaConDiezVacasYDosProductos()
        {
            // Act
            var sembro = await SembradorDeDatos.SembrarAsync(_db, OpcionesDelTaller(), _reloj);

            // Assert
            var granja = await LeerGranjaAsync();
            Assert.That(sembro, Is.True);
            Assert.That(granja.NumeroVacas, Is.EqualTo(10));
            Assert.That(granja.ProduccionDiariaEsperada, Is.EqualTo(150m));
            Assert.That(granja.Productos.Select(p => p.TipoProducto),
                Is.EquivalentTo(new[] { TipoProducto.Queso, TipoProducto.Yogurt }));
        }

        [Test]
        public async Task SembrarAsync_DosVeces_NoDuplicaNada()
        {
            // Arrange
            await SembradorDeDatos.SembrarAsync(_db, OpcionesDelTaller(), _reloj);

            // Act
            var sembro = await SembradorDeDatos.SembrarAsync(_db, OpcionesDelTaller(), _reloj);

            // Assert
            Assert.That(sembro, Is.False);
            Assert.That(await _db.Granjas.CountAsync(), Is.EqualTo(1));
            Assert.That(await _db.Vacas.CountAsync(), Is.EqualTo(10));
            Assert.That(await _db.ParametrosProducto.CountAsync(), Is.EqualTo(2));
        }

        [Test]
        public async Task SembrarAsync_GranjaSinProductos_SoloAgregaLosProductos()
        {
            // Arrange: una granja creada antes de la HU-05, con una vaca y sin productos
            var vieja = new Granja("Granja BaityMu", 15m);
            vieja.AgregarVaca("Holstein", null, _reloj.Today);
            _db.Granjas.Add(vieja);
            await _db.SaveChangesAsync();
            _db.ChangeTracker.Clear();

            // Act
            await SembradorDeDatos.SembrarAsync(_db, OpcionesDelTaller(), _reloj);

            // Assert: no se agregan vacas, solo los productos
            var granja = await LeerGranjaAsync();
            Assert.That(granja.NumeroVacas, Is.EqualTo(1));
            Assert.That(granja.Productos, Has.Count.EqualTo(2));
        }
    }
}
