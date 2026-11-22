namespace BaityMu.Infrastructure.Seed
{
    /// <summary>
    /// Valores iniciales de la granja. Se leen de la sección "GranjaInicial" de
    /// appsettings.json, para que n y l no queden fijos en el código (RNF-09).
    /// </summary>
    /// <remarks>
    /// Los parámetros de cada producto (sección "Productos") se agregan en la HU-05.
    /// </remarks>
    public sealed class GranjaInicialOptions
    {
        /// <summary>Nombre de la sección en appsettings.json.</summary>
        public const string Seccion = "GranjaInicial";

        /// <summary>Nombre de la granja. Por defecto "Granja BaityMu".</summary>
        public string Nombre { get; set; } = "Granja BaityMu";

        /// <summary>n inicial: cuántas vacas se registran al crear la granja.</summary>
        public int NumeroVacas { get; set; }

        /// <summary>l: litros de leche que da cada vaca por día.</summary>
        public decimal LitrosPorVaca { get; set; }

        /// <summary>Raza con la que se registran las vacas iniciales.</summary>
        public string RazaVacasIniciales { get; set; } = "Holstein";
    }
}
