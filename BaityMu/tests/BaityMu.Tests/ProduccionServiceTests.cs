using BaityMu.Domain.Entities;
using BaityMu.Domain.Exceptions;
using BaityMu.Infrastructure.Data;
using BaityMu.Infrastructure.Repositories;
using BaityMu.Services;
using BaityMu.Services.Dtos;
using BaityMu.Services.Modelos;
using BaityMu.Tests.Fakes;
using BaityMu.Tests.Infraestructura;
using Microsoft.EntityFrameworkCore;

namespace BaityMu.Tests
{
    /// <summary>
    /// Pruebas de la producción diaria (HU-07). Incluye la prueba mínima del taller:
    /// la producción diaria es n × l y no se registra dos veces la misma fecha (RN-01).
    /// </summary>
    [TestFixture]
    public class ProduccionServiceTests
    {
        private static readonly DateOnly Dia1 = new(2026, 12, 1);

        private BaityMuDbContext _db = null!;
        private FakeClock _reloj = null!;
        private ProduccionService _servicio = null!;
        private GranjaService _granjaService = null!;

        [SetUp]
        public async Task SetUp()
        {
            _db = BaseDeDatosEnMemoria.Crear();
            _reloj = new FakeClock(Dia1);
            var granjas = new GranjaRepository(_db);
            var producciones = new ProduccionRepository(_db);
            var unitOfWork = new UnitOfWork(_db);
            _servicio = new ProduccionService(granjas, producciones, unitOfWork, _reloj);
            _granjaService = new GranjaService(granjas, producciones, unitOfWork, _reloj);

            // Escenario de referencia: 10 vacas (Id 1 a 10) de 15 L.
            var granja = new Granja("Granja BaityMu", 15m);
            for (var i = 0; i < 10; i++)
                granja.AgregarVaca("Holstein", null, Dia1);
            _db.Granjas.Add(granja);
            await _db.SaveChangesAsync();
            _db.ChangeTracker.Clear();
        }

        [TearDown]
        public void TearDown() => _db.Dispose();

        private Task<int> ContarRegistrosAsync() => _db.ProduccionesLeche.CountAsync();

        [Test]
        public async Task RegistrarAsync_SinLitrosEspecificos_LaProduccionEsNPorL()
        {
            // Act: escenario de referencia, día 1
            var produccion = await _servicio.RegistrarAsync(new RegistrarProduccionRequest { Fecha = Dia1 });

            // Assert: 10 vacas × 15 L = 150 L
            Assert.That(produccion.TotalLitros, Is.EqualTo(150m));
            Assert.That(produccion.Vacas, Has.Count.EqualTo(10));
            Assert.That(produccion.Vacas.All(v => v.Litros == 15m), Is.True);
            Assert.That(await ContarRegistrosAsync(), Is.EqualTo(10));
        }

        [Test]
        public async Task RegistrarAsync_MismaFechaDosVeces_LanzaDuplicadaYNoGuardaNada()
        {
            // Arrange
            await _servicio.RegistrarAsync(new RegistrarProduccionRequest { Fecha = Dia1 });
            _db.ChangeTracker.Clear();

            // Act y Assert
            Assert.ThrowsAsync<ProduccionDuplicadaException>(() =>
                _servicio.RegistrarAsync(new RegistrarProduccionRequest { Fecha = Dia1 }));
            Assert.That(await ContarRegistrosAsync(), Is.EqualTo(10), "Siguen solo los 10 del primer registro");
        }

        [Test]
        public async Task RegistrarAsync_OtraFecha_SiSePermite()
        {
            // Arrange
            await _servicio.RegistrarAsync(new RegistrarProduccionRequest { Fecha = Dia1 });

            // Act
            var dia2 = await _servicio.RegistrarAsync(new RegistrarProduccionRequest { Fecha = Dia1.AddDays(1) });

            // Assert
            Assert.That(dia2.TotalLitros, Is.EqualTo(150m));
            Assert.That(await ContarRegistrosAsync(), Is.EqualTo(20));
        }

        [Test]
        public async Task RegistrarAsync_ConLitrosEspecificos_SoloRegistraEsasVacas()
        {
            // Arrange
            var request = new RegistrarProduccionRequest
            {
                Fecha = Dia1,
                Vacas = { new() { VacaId = 1, Litros = 14.5m }, new() { VacaId = 2, Litros = 16m } }
            };

            // Act
            var produccion = await _servicio.RegistrarAsync(request);

            // Assert
            Assert.That(produccion.Vacas.Select(v => v.VacaId), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(produccion.TotalLitros, Is.EqualTo(30.5m));
            Assert.That(produccion.Vacas[0].Nombre, Is.EqualTo("BaityCow"));
        }

        [Test]
        public async Task RegistrarAsync_UnaVacaYaRegistrada_NoGuardaNingunaDeLaPeticion()
        {
            // Arrange: la vaca 1 ya tiene producción el día 1
            await _servicio.RegistrarAsync(new RegistrarProduccionRequest
            {
                Fecha = Dia1,
                Vacas = { new() { VacaId = 1, Litros = 15m } }
            });
            _db.ChangeTracker.Clear();

            // Act: se piden la vaca 2 (nueva) y la 1 (repetida)
            Assert.ThrowsAsync<ProduccionDuplicadaException>(() => _servicio.RegistrarAsync(new RegistrarProduccionRequest
            {
                Fecha = Dia1,
                Vacas = { new() { VacaId = 2, Litros = 15m }, new() { VacaId = 1, Litros = 15m } }
            }));

            // Assert: tampoco se guardó la vaca 2
            Assert.That(await ContarRegistrosAsync(), Is.EqualTo(1));
        }

        [Test]
        public async Task RegistrarAsync_LitrosNegativos_LanzaReglaDeNegocioYNoGuardaNada()
        {
            Assert.ThrowsAsync<ReglaDeNegocioException>(() => _servicio.RegistrarAsync(new RegistrarProduccionRequest
            {
                Fecha = Dia1,
                Vacas = { new() { VacaId = 1, Litros = 15m }, new() { VacaId = 2, Litros = -1m } }
            }));
            Assert.That(await ContarRegistrosAsync(), Is.EqualTo(0));
        }

        [Test]
        public void RegistrarAsync_VacaInexistente_LanzaNoEncontrado()
        {
            Assert.ThrowsAsync<RecursoNoEncontradoException>(() => _servicio.RegistrarAsync(new RegistrarProduccionRequest
            {
                Fecha = Dia1,
                Vacas = { new() { VacaId = 99, Litros = 15m } }
            }));
        }

        [Test]
        public async Task RegistrarAsync_VacaRetirada_NoCuentaEnNPorLYNoSePuedeRegistrar()
        {
            // Arrange: se retira la vaca 10 → n = 9
            await _granjaService.RetirarVacaAsync(10);
            _db.ChangeTracker.Clear();

            // Act y Assert: 9 × 15 = 135
            var produccion = await _servicio.RegistrarAsync(new RegistrarProduccionRequest { Fecha = Dia1 });
            Assert.That(produccion.TotalLitros, Is.EqualTo(135m));

            Assert.ThrowsAsync<RecursoNoEncontradoException>(() => _servicio.RegistrarAsync(new RegistrarProduccionRequest
            {
                Fecha = Dia1.AddDays(1),
                Vacas = { new() { VacaId = 10, Litros = 15m } }
            }));
        }

        [Test]
        public async Task RegistrarAsync_SinFecha_UsaLaFechaDelReloj()
        {
            // Arrange: el reloj simula que hoy es el día 3 (RN-10)
            _reloj.AvanzarDias(2);

            // Act
            var produccion = await _servicio.RegistrarAsync(new RegistrarProduccionRequest());

            // Assert
            Assert.That(produccion.Fecha, Is.EqualTo(new DateOnly(2026, 12, 3)));
        }

        [Test]
        public void RegistrarAsync_VacaRepetidaEnLaPeticion_LanzaReglaDeNegocio()
        {
            Assert.ThrowsAsync<ReglaDeNegocioException>(() => _servicio.RegistrarAsync(new RegistrarProduccionRequest
            {
                Fecha = Dia1,
                Vacas = { new() { VacaId = 1, Litros = 15m }, new() { VacaId = 1, Litros = 10m } }
            }));
        }

        // ----------------------------------------------------------------------------
        // HU-08 Consultar la producción de un día
        // ----------------------------------------------------------------------------

        [Test]
        public async Task ObtenerAsync_ConProduccion_DevuelveCadaVacaYElTotal()
        {
            // Arrange
            await _servicio.RegistrarAsync(new RegistrarProduccionRequest { Fecha = Dia1 });
            _db.ChangeTracker.Clear();

            // Act
            var produccion = await _servicio.ObtenerAsync(Dia1);

            // Assert
            Assert.That(produccion.Vacas, Has.Count.EqualTo(10));
            Assert.That(produccion.Vacas[0].Nombre, Is.EqualTo("BaityCow"));
            Assert.That(produccion.TotalLitros, Is.EqualTo(150m));
            Assert.That(produccion.LitrosTransformados, Is.EqualTo(0m));
            Assert.That(produccion.LitrosDisponibles, Is.EqualTo(150m));
        }

        [Test]
        public async Task ObtenerAsync_SoloTraeLaFechaPedida()
        {
            // Arrange: días 1 y 2
            await _servicio.RegistrarAsync(new RegistrarProduccionRequest { Fecha = Dia1 });
            await _servicio.RegistrarAsync(new RegistrarProduccionRequest
            {
                Fecha = Dia1.AddDays(1),
                Vacas = { new() { VacaId = 1, Litros = 20m } }
            });

            // Act
            var dia2 = await _servicio.ObtenerAsync(Dia1.AddDays(1));

            // Assert
            Assert.That(dia2.TotalLitros, Is.EqualTo(20m));
        }

        [Test]
        public void ObtenerAsync_SinProduccion_LanzaNoEncontrado()
        {
            Assert.ThrowsAsync<RecursoNoEncontradoException>(() => _servicio.ObtenerAsync(Dia1));
        }

        [Test]
        public void ProduccionDiaria_DespuesDeTransformar100De150_Quedan50Disponibles()
        {
            // Arrange: prueba de la tarea de la HU-08. Los 100 L transformados vendrán de
            // los lotes en la HU-09; aquí se prueba el cálculo.
            var vacas = Enumerable.Range(1, 10)
                .Select(i => new ProduccionDeVaca(i, $"Vaca {i}", 15m))
                .ToList();

            // Act
            var produccion = new ProduccionDiaria(Dia1, vacas, LitrosTransformados: 100m);

            // Assert
            Assert.That(produccion.LitrosDisponibles, Is.EqualTo(50m));
        }
    }
}
