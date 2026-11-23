namespace BaityMu.Services.Dtos
{
    /// <summary>Configuración de la granja (GET /api/granja, HU-05).</summary>
    public sealed class GranjaResponse
    {
        /// <summary>Id de la granja.</summary>
        public int Id { get; set; }

        /// <summary>Nombre de la granja.</summary>
        public string Nombre { get; set; } = string.Empty;

        /// <summary>n: número de vacas activas.</summary>
        public int NumeroVacas { get; set; }

        /// <summary>l: litros que da cada vaca por día.</summary>
        public decimal LitrosPorVaca { get; set; }

        /// <summary>n × l: litros que se esperan cada día. 150 L en el escenario de referencia.</summary>
        public decimal ProduccionDiariaEsperada { get; set; }

        /// <summary>Rendimiento, vida útil y precio de cada producto.</summary>
        public List<ParametroProductoResponse> Productos { get; set; } = new();
    }
}
