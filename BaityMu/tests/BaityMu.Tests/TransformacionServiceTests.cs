using BaityMu.Domain.Entities;
using BaityMu.Domain.Enums;
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
    /// Pruebas de ProduccionService.TransformarAsync (HU-09) con base de datos InMemory,
    /// siguiendo los días 1 y 2 del escenario de referencia.
    /// </summary>
    [TestFixture]
    public class TransformacionServiceTests
    {
        private static readonly DateOnly Dia1 = new(2026, 12, 1);
        private static readonly DateOnly Dia2 = new(2026, 12, 2);

        private BaityMuDbContext _db = null!;
        private ProduccionService _produccion = null!;
        private GranjaService _granjaService = null!;

        [SetUp]
        public async Task SetUp()
        {
            _db = BaseDeDatosEnMemoria.Crear();
            var reloj = new FakeClock(Dia1);
            var granjas = new GranjaRepository(_db);
            var producciones = new ProduccionRepository(_db);
            var unitOfWork = new UnitOfWork(_db);
            _produccion = new ProduccionService(granjas, producciones, new LoteRepository(_db), unitOfWork, reloj);
            _granjaService = new GranjaService(granjas, producciones, unitOfWork, reloj);

            // Granja del taller: 10 vacas × 15 L, queso y yogurt.
            var granja = new Granja("Granja BaityMu", 15m);
            for (var i = 0; i < 10; i++)
                granja.AgregarVaca("Holstein", null, Dia1);
            granja.AgregarProducto(TipoProducto.Queso, "kg", 10m, 30, 28000m);
            granja.AgregarProducto(TipoProducto.Yogurt, "L", 1m, 15, 6000m);
            _db.Granjas.Add(granja);
            await _db.SaveChangesAsync();
            _db.ChangeTracker.Clear();

            // Días 1 y 2: 150 L cada uno.
            await _produccion.RegistrarAsync(new RegistrarProduccionRequest { Fecha = Dia1 });
            await _produccion.RegistrarAsync(new RegistrarProduccionRequest { Fecha = Dia2 });
            _db.ChangeTracker.Clear();
        }

        [TearDown]
        public void TearDown() => _db.Dispose();

        private Task<LoteProducto> TransformarAsync(DateOnly fecha, TipoProducto tipo, decimal litros) =>
            _produccion.TransformarAsync(new TransformarLecheRequest { Fecha = fecha, TipoProducto = tipo, Litros = litros });

        [Test]
        public async Task TransformarAsync_EscenarioDia1_CreaQ1YY1()
        {
            // Act
            var q1 = await TransformarAsync(Dia1, TipoProducto.Queso, 100m);
            var y1 = await TransformarAsync(Dia1, TipoProducto.Yogurt, 50m);

            // Assert
            Assert.That(q1.Id, Is.GreaterThan(0));
            Assert.That(q1.CantidadInicial, Is.EqualTo(10m));
            Assert.That(y1.CantidadInicial, Is.EqualTo(50m));
            Assert.That(y1.FechaVencimiento, Is.EqualTo(new DateOnly(2026, 12, 16)));
            Assert.That(await _db.LotesProducto.CountAsync(), Is.EqualTo(2), "Cada transformación es su propio lote");
        }

        [Test]
        public async Task TransformarAsync_EscenarioDia2_Q2Da15Kilos()
        {
            var q2 = await TransformarAsync(Dia2, TipoProducto.Queso, 150m);

            Assert.That(q2.CantidadInicial, Is.EqualTo(15m));
        }

        [Test]
        public async Task TransformarAsync_MasDeLoQueQueda_LanzaLecheInsuficienteYNoCreaLote()
        {
            // Arrange: de 150 L ya se transformaron 100 L
            await TransformarAsync(Dia1, TipoProducto.Queso, 100m);

            // Act y Assert
            Assert.ThrowsAsync<LecheInsuficienteException>(() => TransformarAsync(Dia1, TipoProducto.Yogurt, 60m));
            Assert.That(await _db.LotesProducto.CountAsync(), Is.EqualTo(1));
        }

        [Test]
        public void TransformarAsync_DiaSinProduccion_LanzaLecheInsuficiente()
        {
            Assert.ThrowsAsync<LecheInsuficienteException>(() =>
                TransformarAsync(new DateOnly(2026, 12, 5), TipoProducto.Queso, 10m));
        }

        [Test]
        public async Task TransformarAsync_LecheDeOtroDia_NoSeMezcla()
        {
            // Arrange: se usa toda la leche del día 1
            await TransformarAsync(Dia1, TipoProducto.Queso, 150m);

            // Act: el día 2 sigue teniendo sus 150 L
            var lote = await TransformarAsync(Dia2, TipoProducto.Yogurt, 150m);

            // Assert
            Assert.That(lote.FechaProduccion, Is.EqualTo(Dia2));
        }

        [Test]
        public async Task ObtenerProduccion_DespuesDeTransformar100De150_Quedan50Disponibles()
        {
            // Arrange (tarea de la HU-08, ahora con lotes de verdad)
            await TransformarAsync(Dia1, TipoProducto.Queso, 100m);

            // Act
            var dia1 = await _produccion.ObtenerAsync(Dia1);

            // Assert: los 50 L sobrantes son la leche no aprovechada (criterio 8)
            Assert.That(dia1.TotalLitros, Is.EqualTo(150m));
            Assert.That(dia1.LitrosTransformados, Is.EqualTo(100m));
            Assert.That(dia1.LitrosDisponibles, Is.EqualTo(50m));
        }

        [Test]
        public async Task TransformarAsync_ProductoNoConfigurado_LanzaReglaDeNegocio()
        {
            // Arrange: una granja sin productos
            var granja = await _db.Granjas.Include(g => g.Productos).SingleAsync();
            _db.ParametrosProducto.RemoveRange(granja.Productos);
            await _db.SaveChangesAsync();
            _db.ChangeTracker.Clear();

            // Act y Assert (criterio 6: 400)
            Assert.ThrowsAsync<ReglaDeNegocioException>(() => TransformarAsync(Dia1, TipoProducto.Queso, 10m));
        }

        [Test]
        public async Task ActualizarVidaUtil_DespuesDeCrearUnLote_ElLoteConservaSuVencimiento()
        {
            // Criterio 3 de la HU-06: los lotes anteriores conservan sus valores.
            var y1 = await TransformarAsync(Dia1, TipoProducto.Yogurt, 50m);
            _db.ChangeTracker.Clear();

            // Act: el yogurt pasa a durar 5 días
            await _granjaService.ActualizarAsync(new ActualizarGranjaRequest
            {
                Productos = { new() { TipoProducto = TipoProducto.Yogurt, LitrosPorUnidad = 1m, VidaUtilDias = 5, PrecioUnitario = 6000m } }
            });
            _db.ChangeTracker.Clear();
            var y2 = await TransformarAsync(Dia2, TipoProducto.Yogurt, 10m);

            // Assert: Y1 sigue venciendo el 16; el lote nuevo usa los 5 días
            var y1Guardado = await _db.LotesProducto.AsNoTracking().SingleAsync(l => l.Id == y1.Id);
            Assert.That(y1Guardado.FechaVencimiento, Is.EqualTo(new DateOnly(2026, 12, 16)));
            Assert.That(y2.FechaVencimiento, Is.EqualTo(new DateOnly(2026, 12, 7)));
        }
    }
}
