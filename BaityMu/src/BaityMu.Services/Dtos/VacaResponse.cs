namespace BaityMu.Services.Dtos
{
    /// <summary>
    /// Vaca tal como la devuelve la API. Se llena desde la entidad Vaca con AutoMapper.
    /// </summary>
    public sealed class VacaResponse
    {
        /// <summary>Id asignado por la base de datos.</summary>
        public int Id { get; set; }

        /// <summary>Nombre de la vaca.</summary>
        public string Nombre { get; set; } = string.Empty;

        /// <summary>Raza de la vaca.</summary>
        public string Raza { get; set; } = string.Empty;

        /// <summary>true mientras la vaca está en el hato; false cuando se retira (HU-04).</summary>
        public bool Activa { get; set; }

        /// <summary>Día en que la vaca llegó a la granja.</summary>
        public DateOnly FechaIngreso { get; set; }

        /// <summary>
        /// Litros que ha producido en total. Una vaca recién registrada tiene 0 L
        /// (criterio 1 de la HU-01). Se calcula con la producción diaria a partir de la HU-07.
        /// </summary>
        public decimal LitrosProducidos { get; set; }
    }
}
