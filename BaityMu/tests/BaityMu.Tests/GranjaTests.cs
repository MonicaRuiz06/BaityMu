using BaityMu.Domain.Entities;
using BaityMu.Domain.Exceptions;

namespace BaityMu.Tests
{
    
    [TestFixture]
    public class GranjaTests
    {
        private Granja _granja = null!;
        private readonly DateOnly _fecha = new DateOnly(2026, 12, 1);

        [SetUp]
        public void SetUp()
        {
            _granja = new Granja("Granja BaityMu", 15m);
        }
        [Test]
        public void AgregarVaca_ConRazaValida_CreaVacaActiva()
        {
            // Act
            var vaca = _granja.AgregarVaca("Holstein", "Lola", _fecha);

            // Assert
            Assert.That(vaca.Raza, Is.EqualTo("Holstein"));
            Assert.That(vaca.Activa, Is.True);

        }

        [Test]
        public void AgregarVaca_SinNombre_LaPrimeraSeLlamaBaityCow()
        {
            // Act
            var vaca = _granja.AgregarVaca("Holstein", null, _fecha);

            // Assert
            Assert.That(vaca.Nombre, Is.EqualTo("BaityCow"));

        }

        [Test]
        public void AgregarVaca_ConRazaVacia_LanzaReglaDeNegocio()
        {
            // Act y Assert
            Assert.Throws<ReglaDeNegocioException>(() =>
                _granja.AgregarVaca("", null, _fecha));

        }

        [Test]
        public void AgregarVaca_AumentaElNumeroDeVacas()
        {
            //Arrange
            Assert.That(_granja.NumeroVacas, Is.EqualTo(0));

            // Act
            _granja.AgregarVaca("Holstein", null, _fecha);

            // Assert
            Assert.That(_granja.NumeroVacas, Is.EqualTo(1));
        }

        [Test]
        public void AgregarVaca_SinNombre_LaSegundaSeLlamaBaityCow2()
        {
            // Arrange
            _granja.AgregarVaca("Holstein", null, _fecha);

            // Act
            var segunda = _granja.AgregarVaca("Jersey", null, _fecha);

            // Assert
            Assert.That(segunda.Nombre, Is.EqualTo("BaityCow 2"));
        }

        [Test]
        public void AgregarVaca_ConNombreRepetido_LanzaConflicto()
        {
            // Arrange
            _granja.AgregarVaca("Holstein", "Lola", _fecha);

            // Act y Assert
            Assert.Throws<ConflictoException>(() =>
                _granja.AgregarVaca("Jersey", "lola", _fecha));
        }

        [Test]
        public void AgregarVaca_SinNombreYNombreOcupado_UsaElSiguienteNumero()
        {
            // Arrange: una vaca se llamó "BaityCow 3" a mano
            _granja.AgregarVaca("Holstein", null, _fecha);           // BaityCow
            _granja.AgregarVaca("Holstein", "BaityCow 3", _fecha);   // nombre a mano

            // Act: la automática sería "BaityCow 3", pero está ocupada
            var vaca = _granja.AgregarVaca("Holstein", null, _fecha);

            // Assert
            Assert.That(vaca.Nombre, Is.EqualTo("BaityCow 4"));
        }
    }
}