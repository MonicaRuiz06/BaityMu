namespace BaityMu.Services.Dtos
{
    /// <summary>Reporte de producción por vaca: promedio, la mejor, la peor y las que están por debajo (HU-20).</summary>
    /// <remarks>
    /// Hay dos promedios distintos:
    /// - PromedioDiarioHato: litros de TODO el hato en un día (total ÷ días con ordeño).
    /// - PromedioPorVaca: litros de UNA vaca en un día (total ÷ número de ordeños).
    /// Cada vaca se compara contra PromedioPorVaca para saber si está por debajo.
    /// Si no hubo producción en el rango, VacasEvaluadas = 0, las listas van vacías y
    /// VacaMayor / VacaMenor van en null.
    /// </remarks>
    public sealed class ReporteVacasResponse
    {
        /// <summary>Fecha inicial del filtro (null = sin límite).</summary>
        public DateOnly? Desde { get; set; }

        /// <summary>Fecha final del filtro (null = sin límite).</summary>
        public DateOnly? Hasta { get; set; }

        /// <summary>Cuántos días distintos hubo ordeño en el período.</summary>
        public int DiasConProduccion { get; set; }

        /// <summary>Cuántas vacas tuvieron producción en el período.</summary>
        public int VacasEvaluadas { get; set; }

        /// <summary>Litros de todo el hato en el período.</summary>
        public decimal LitrosTotales { get; set; }

        /// <summary>Litros del hato completo en un día con ordeño (total ÷ días).</summary>
        public decimal PromedioDiarioHato { get; set; }

        /// <summary>Litros que da una vaca en un día, en promedio (total ÷ ordeños).</summary>
        public decimal PromedioPorVaca { get; set; }

        /// <summary>La vaca con el mayor promedio diario (null si no hubo producción).</summary>
        public VacaProduccionItem? VacaMayor { get; set; }

        /// <summary>La vaca con el menor promedio diario (null si no hubo producción).</summary>
        public VacaProduccionItem? VacaMenor { get; set; }

        /// <summary>Todas las vacas evaluadas, de la que más produce a la que menos.</summary>
        public List<VacaProduccionItem> Vacas { get; set; } = new();

        /// <summary>Las vacas con promedio menor que PromedioPorVaca, de la que más le falta a la que menos.</summary>
        public List<VacaProduccionItem> PorDebajoDelPromedio { get; set; } = new();
    }

    /// <summary>La producción de una vaca en el período.</summary>
    public sealed class VacaProduccionItem
    {
        /// <summary>Id de la vaca.</summary>
        public int VacaId { get; set; }

        /// <summary>Nombre de la vaca.</summary>
        public string Nombre { get; set; } = string.Empty;

        /// <summary>Raza de la vaca.</summary>
        public string Raza { get; set; } = string.Empty;

        /// <summary>false si la vaca ya se retiró (igual se evalúa lo que produjo).</summary>
        public bool Activa { get; set; }

        /// <summary>Días que se ordeñó en el período.</summary>
        public int DiasOrdenada { get; set; }

        /// <summary>Litros que dio en el período.</summary>
        public decimal LitrosTotales { get; set; }

        /// <summary>Litros por día ordeñada (redondeado a 2 decimales).</summary>
        public decimal PromedioDiario { get; set; }

        /// <summary>Litros por día que le faltan para alcanzar el promedio por vaca (0 si no está por debajo).</summary>
        public decimal Faltante { get; set; }
    }
}
