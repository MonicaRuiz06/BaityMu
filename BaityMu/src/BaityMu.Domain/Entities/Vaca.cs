using BaityMu.Domain.Exceptions;

namespace BaityMu.Domain.Entities

{

    /// <summary>
    /// Vaca del hato de Baity. Se registra con su raza y fecha de ingreso y nace activa.
    /// </summary>
    /// <remarks>
    /// Solo se crea por medio de <see cref="Granja.AgregarVaca"/>, para que siempre cumpla
    /// las reglas de la granja. Nunca se borra: al retirarla se desactiva (RNF-11).
    /// </remarks>
    public class Vaca
    {
        public int Id { get; private set; }
        public int GranjaId { get; private set; }
        public string Nombre { get; private set; } = string.Empty;
        public string Raza { get; private set; } = string.Empty;
        public bool Activa { get; private set; }
        public DateOnly FechaIngreso { get; private set; }

        // Constructor vacío privado requerido por EF Core
        private Vaca() { }

        /// <summary>Crea una vaca activa. Lo usa solo <see cref="Granja"/>.</summary>
        /// <param name="nombre">Nombre de la vaca, ya definido por la granja.</param>
        /// <param name="raza">Raza de la vaca (Holstein, Jersey...). Es obligatoria.</param>
        /// <param name="fechaIngreso">Día en que la vaca llegó a la granja.</param>
        /// <exception cref="ReglaDeNegocioException">Si la raza viene vacía.</exception>
        internal Vaca(string nombre, string raza, DateOnly fechaIngreso)
        {
            if (string.IsNullOrWhiteSpace(raza))
                throw new ReglaDeNegocioException("La raza de la vaca es obligatoria.");

            Nombre = nombre;
            Raza = raza.Trim();
            FechaIngreso = fechaIngreso;
            Activa = true;
        }

        /// <summary>
        /// Retira la vaca del hato: deja de contar en n, pero no se borra, para conservar su
        /// historial (RNF-11). Lo usa solo <see cref="Granja.RetirarVaca"/>.
        /// </summary>
        internal void Retirar() => Activa = false;
    }
}
