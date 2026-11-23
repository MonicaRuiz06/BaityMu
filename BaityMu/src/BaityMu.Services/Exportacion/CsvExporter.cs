using System.Globalization;
using System.Reflection;
using System.Text;

namespace BaityMu.Services.Exportacion
{
    /// <summary>
    /// Convierte una lista de DTOs en un archivo CSV (HU-21).
    /// </summary>
    /// <remarks>
    /// ¿Cómo sabe qué columnas poner?
    /// Usa reflexión: recorre las propiedades públicas del tipo T (en el orden en que están
    /// declaradas) y cada una es una columna. Así sirve para cualquier reporte sin escribir
    /// un exportador por cada uno.
    ///
    /// Formato pensado para Excel en español (Colombia):
    /// - separador ";" (con "," Excel en español junta todo en una sola columna);
    /// - números con coma decimal (16,67) y sin ceros sobrantes (25 en vez de 25,000);
    /// - fechas yyyy-MM-dd; enums por su nombre (Queso); bool como Sí / No;
    /// - UTF-8 con BOM, para que las tildes y la ñ se vean bien al abrirlo.
    /// Si un valor trae ";", comillas o saltos de línea, va entre comillas (RFC 4180).
    ///
    /// Solo se exportan las propiedades de tipo simple (texto, números, fechas, enums, bool).
    /// Las listas u objetos anidados se omiten: cada reporte exporta su lista principal.
    /// </remarks>
    public static class CsvExporter
    {
        /// <summary>Separador de columnas.</summary>
        public const char Separador = ';';

        /// <summary>Tipo MIME de un archivo CSV.</summary>
        public const string TipoContenido = "text/csv";

        private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-CO");

        /// <summary>Arma el texto CSV: una línea de encabezado y una línea por cada fila.</summary>
        /// <typeparam name="T">Tipo del DTO; sus propiedades simples son las columnas.</typeparam>
        /// <param name="filas">Los datos. Si está vacía, el CSV queda solo con el encabezado.</param>
        public static string Exportar<T>(IEnumerable<T> filas)
        {
            var columnas = typeof(T)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => EsSimple(p.PropertyType))
                .ToList();

            var csv = new StringBuilder();
            csv.AppendLine(string.Join(Separador, columnas.Select(c => Escapar(c.Name))));

            foreach (var fila in filas)
                csv.AppendLine(string.Join(Separador, columnas.Select(c => Escapar(Formatear(c.GetValue(fila))))));

            return csv.ToString();
        }

        /// <summary>El mismo CSV, en bytes UTF-8 con BOM, listo para devolverlo como archivo.</summary>
        public static byte[] ExportarBytes<T>(IEnumerable<T> filas)
        {
            var utf8ConBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
            return [.. utf8ConBom.GetPreamble(), .. utf8ConBom.GetBytes(Exportar(filas))];
        }

        /// <summary>Convierte un valor en el texto de su celda.</summary>
        private static string Formatear(object? valor) => valor switch
        {
            null => string.Empty,
            DateOnly fecha => fecha.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            DateTime fechaHora => fechaHora.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            decimal numero => numero.ToString("0.############", Cultura),
            double numero => numero.ToString("0.############", Cultura),
            bool siNo => siNo ? "Sí" : "No",
            Enum valorEnum => valorEnum.ToString(),
            IFormattable otro => otro.ToString(null, Cultura),
            _ => valor.ToString() ?? string.Empty,
        };

        /// <summary>Pone el valor entre comillas si trae el separador, comillas o saltos de línea.</summary>
        private static string Escapar(string valor) =>
            valor.IndexOfAny([Separador, '"', '\n', '\r']) >= 0
                ? $"\"{valor.Replace("\"", "\"\"")}\""
                : valor;

        /// <summary>true para los tipos que caben en una celda (también sus versiones con ?).</summary>
        private static bool EsSimple(Type tipo)
        {
            var t = Nullable.GetUnderlyingType(tipo) ?? tipo;
            return t.IsPrimitive || t.IsEnum
                || t == typeof(string) || t == typeof(decimal)
                || t == typeof(DateOnly) || t == typeof(DateTime);
        }
    }
}
