using BaityMu.Domain.Enums;
using BaityMu.Services.Exportacion;

namespace BaityMu.Tests
{
    /// <summary>Pruebas del exportador CSV (HU-21): formato de los valores y escape.</summary>
    [TestFixture]
    public class CsvExporterTests
    {
        private sealed class Fila
        {
            public string Nombre { get; set; } = string.Empty;
            public decimal Litros { get; set; }
            public DateOnly? Fecha { get; set; }
            public TipoProducto Tipo { get; set; }
            public bool Activa { get; set; }
            public List<int> Ignorada { get; set; } = new();
        }

        private static string[] Lineas(string csv) => csv.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        [Test]
        public void Exportar_PoneEncabezadoYFormateaCadaTipo()
        {
            var csv = CsvExporter.Exportar(new[]
            {
                new Fila { Nombre = "BaityCow", Litros = 16.6667m, Fecha = new DateOnly(2026, 12, 1), Tipo = TipoProducto.Yogurt, Activa = true },
            });

            // La lista (Ignorada) no es una columna; decimal con coma y sin ceros sobrantes.
            Assert.That(Lineas(csv), Is.EqualTo(new[]
            {
                "Nombre;Litros;Fecha;Tipo;Activa",
                "BaityCow;16,6667;2026-12-01;Yogurt;Sí",
            }));
        }

        [Test]
        public void Exportar_ValoresConSeparadorOComillas_VanEntreComillas()
        {
            var csv = CsvExporter.Exportar(new[] { new Fila { Nombre = "Lola; la \"grande\"", Litros = 25.000m, Tipo = TipoProducto.Queso } });

            Assert.That(Lineas(csv)[1], Is.EqualTo("\"Lola; la \"\"grande\"\"\";25;;Queso;No"));
        }

        [Test]
        public void Exportar_SinFilas_DejaSoloElEncabezado()
        {
            Assert.That(Lineas(CsvExporter.Exportar(Array.Empty<Fila>())), Has.Length.EqualTo(1));
        }

        [Test]
        public void ExportarBytes_EmpiezaConElBomDeUtf8()
        {
            var bytes = CsvExporter.ExportarBytes(Array.Empty<Fila>());

            Assert.That(bytes.Take(3), Is.EqualTo(new byte[] { 0xEF, 0xBB, 0xBF }));
        }
    }
}
