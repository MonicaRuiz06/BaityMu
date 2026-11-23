namespace BaityMu.Services.Dtos
{
    /// <summary>Reporte de la leche de cada día: cuánta se produjo, se transformó y se perdió (HU-16).</summary>
    public sealed class ReporteLecheDiariaResponse
    {
        /// <summary>Fecha inicial del filtro (null = sin límite).</summary>
        public DateOnly? Desde { get; set; }

        /// <summary>Fecha final del filtro (null = sin límite).</summary>
        public DateOnly? Hasta { get; set; }

        /// <summary>Un renglón por cada día con producción, del más antiguo al más reciente.</summary>
        public List<LecheDelDia> Dias { get; set; } = new();

        /// <summary>Litros producidos en todo el período.</summary>
        public decimal TotalProducido { get; set; }

        /// <summary>Litros transformados en queso o yogurt en todo el período.</summary>
        public decimal TotalTransformado { get; set; }

        /// <summary>Litros que no se transformaron en todo el período.</summary>
        public decimal TotalNoAprovechado { get; set; }
    }

    /// <summary>La leche de un día.</summary>
    public sealed class LecheDelDia
    {
        /// <summary>Día del ordeño.</summary>
        public DateOnly Fecha { get; set; }

        /// <summary>Suma de los litros de todas las vacas ese día.</summary>
        public decimal LitrosProducidos { get; set; }

        /// <summary>Litros de ese día que se usaron en lotes de queso o yogurt.</summary>
        public decimal LitrosTransformados { get; set; }

        /// <summary>Producidos − transformados: la leche que no se aprovechó.</summary>
        public decimal LitrosNoAprovechados { get; set; }
    }
}
