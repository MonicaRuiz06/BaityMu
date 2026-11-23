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
        /// <exception cref="ConflictoException">Si ya hay una vaca con ese nombre.</exception>
        public Vaca AgregarVaca(string raza, string? nombre, DateOnly fechaIngreso)
        {
            string nombreFinal;
            if (string.IsNullOrWhiteSpace(nombre))
            {
                nombreFinal = GenerarNombre();
            }
            else
            {
                nombreFinal = nombre.Trim();
                if (ExisteNombre(nombreFinal))
                    throw new ConflictoException($"Ya existe una vaca llamada {nombreFinal} en la granja.");
            }

            var vaca = new Vaca(nombreFinal, raza, fechaIngreso);
            _vacas.Add(vaca);

            return vaca;
        }

        /// <summary>Retira una vaca del hato (HU-04). n disminuye en 1 y su historial se conserva.</summary>
        /// <param name="vacaId">Id de la vaca.</param>
        /// <returns>La vaca retirada.</returns>
        /// <exception cref="RecursoNoEncontradoException">Si la vaca no existe (404).</exception>
        /// <exception cref="ConflictoException">Si la vaca ya estaba retirada (409).</exception>
        /// <exception cref="ReglaDeNegocioException">Si es la última vaca activa: la granja
        /// necesita al menos una para producir (400).</exception>
        public Vaca RetirarVaca(int vacaId)
        {
            var vaca = _vacas.FirstOrDefault(v => v.Id == vacaId)
                ?? throw new RecursoNoEncontradoException("una vaca", vacaId);

            if (!vaca.Activa)
                throw new ConflictoException($"La vaca {vaca.Nombre} ya estaba retirada.");

            if (NumeroVacas == 1)
                throw new ReglaDeNegocioException(
                    $"No se puede retirar a {vaca.Nombre}: es la última vaca activa y la granja necesita al menos una.");

            vaca.Retirar();
            return vaca;
        }

        /// <summary>
        /// Genera el nombre automático: "BaityCow" para la primera vaca y "BaityCow 2",
        /// "BaityCow 3"... para las siguientes. Si ese nombre ya está ocupado, prueba el
        /// siguiente número, para que nunca se repita.
        /// </summary>
        private string GenerarNombre()
        {
            var numero = _vacas.Count + 1;
            var nombre = numero == 1 ? "BaityCow" : $"BaityCow {numero}";

            while (ExisteNombre(nombre))
            {
                numero++;
                nombre = $"BaityCow {numero}";
            }

            return nombre;
        }

        /// <summary>Indica si alguna vaca de la granja ya tiene ese nombre (sin importar mayúsculas).</summary>
        private bool ExisteNombre(string nombre) =>
            _vacas.Any(v => string.Equals(v.Nombre, nombre, StringComparison.OrdinalIgnoreCase));
    }
}
