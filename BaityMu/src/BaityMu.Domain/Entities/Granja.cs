using BaityMu.Domain.Exceptions;

namespace BaityMu.Domain.Entities
{

    /// <summary>
    /// Granja lechera de Baity. Es la dueña del hato y conoce cuántos litros da cada vaca (l).
    /// </summary>
    public class Granja
    {
        private readonly List<Vaca> _vacas = new();

        public int Id { get; private set; }
        public string Nombre { get; private set; } = string.Empty;
        public decimal LitrosPorVaca { get; private set; }

        public IReadOnlyCollection<Vaca> Vacas => _vacas;

        /// <summary>n: número de vacas activas. Se calcula, no se guarda en la base de datos.</summary>
        public int NumeroVacas => _vacas.Count(v => v.Activa);

        // Constructor vacío privado requerido por EF Core
        private Granja() { }

        public Granja(string nombre, decimal litrosPorVaca)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                throw new ReglaDeNegocioException("El nombre de la granja es obligatorio.");

            if (litrosPorVaca <= 0)
                throw new ReglaDeNegocioException("Los litros por vaca deben ser mayores a cero.");

            Nombre = nombre.Trim();
            LitrosPorVaca = litrosPorVaca;
        }


        /// <summary>Registra una vaca nueva en la granja (HU-01).</summary>
        /// <param name="raza">Raza de la vaca. Es obligatoria.</param>
        /// <param name="nombre">Nombre opcional. Si no se envía, la primera se llama "BaityCow",
        /// la segunda "BaityCow 2", y así sucesivamente.</param>
        /// <param name="fechaIngreso">Día en que la vaca llegó a la granja.</param>
        /// <returns>La vaca creada, activa.</returns>
        /// <exception cref="ReglaDeNegocioException">Si la raza viene vacía.</exception>
        public Vaca AgregarVaca(string raza, string? nombre, DateOnly fechaIngreso)
        {
            var nombreFinal = string.IsNullOrWhiteSpace(nombre)
                ? (_vacas.Count == 0 ? "BaityCow" : $"BaityCow {_vacas.Count + 1}")
                : nombre.Trim();

            var vaca = new Vaca(nombreFinal, raza, fechaIngreso);
            _vacas.Add(vaca);

            return vaca;
        }

    }
}
