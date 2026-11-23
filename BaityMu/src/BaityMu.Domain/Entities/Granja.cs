using BaityMu.Domain.Enums;
using BaityMu.Domain.Exceptions;

namespace BaityMu.Domain.Entities
{
    /// <summary>
    /// Granja lechera de Baity. Es la dueña del hato y conoce cuántos litros da cada vaca (l).
    /// </summary>
    public class Granja
    {
        private readonly List<Vaca> _vacas = new();
        private readonly List<ParametroProducto> _productos = new();

        public int Id { get; private set; }
        public string Nombre { get; private set; } = string.Empty;
        public decimal LitrosPorVaca { get; private set; }

        public IReadOnlyCollection<Vaca> Vacas => _vacas;

        /// <summary>Parámetros de cada producto (queso y yogurt).</summary>
        public IReadOnlyCollection<ParametroProducto> Productos => _productos;

        /// <summary>n: número de vacas activas. Se calcula, no se guarda en la base de datos.</summary>
        public int NumeroVacas => _vacas.Count(v => v.Activa);

        /// <summary>
        /// Producción diaria esperada: n × l (RN-01). En el escenario de referencia,
        /// 10 vacas × 15 L = 150 L. Se calcula, no se guarda.
        /// </summary>
        public decimal ProduccionDiariaEsperada => NumeroVacas * LitrosPorVaca;

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

        /// <summary>
        /// Agrega los parámetros de un producto (rendimiento, vida útil y precio). Se usa
        /// al sembrar la granja desde appsettings.json (HU-05).
        /// </summary>
        /// <returns>Los parámetros creados.</returns>
        /// <exception cref="ReglaDeNegocioException">Si algún valor no es válido (400).</exception>
        /// <exception cref="ConflictoException">Si ese producto ya está configurado (409).</exception>
        public ParametroProducto AgregarProducto(
            TipoProducto tipoProducto,
            string unidad,
            decimal litrosPorUnidad,
            int vidaUtilDias,
            decimal precioUnitario)
        {
            if (_productos.Any(p => p.TipoProducto == tipoProducto))
                throw new ConflictoException($"El producto {tipoProducto} ya está configurado en la granja.");

            var producto = new ParametroProducto(tipoProducto, unidad, litrosPorUnidad, vidaUtilDias, precioUnitario);
            _productos.Add(producto);
            return producto;
        }

        /// <summary>Cambia l, los litros que da cada vaca por día (HU-06).</summary>
        /// <exception cref="ReglaDeNegocioException">Si l es 0 o negativo (400).</exception>
        public void ActualizarLitrosPorVaca(decimal litrosPorVaca)
        {
            if (litrosPorVaca <= 0)
                throw new ReglaDeNegocioException("Los litros por vaca deben ser mayores a cero.");

            LitrosPorVaca = litrosPorVaca;
        }

        /// <summary>
        /// Configura un producto (HU-06): si ya existe, actualiza sus parámetros; si no,
        /// lo agrega.
        /// </summary>
        /// <param name="tipoProducto">Queso o Yogurt.</param>
        /// <param name="unidad">Unidad de venta. Si es null se conserva la actual.</param>
        /// <param name="litrosPorUnidad">Rendimiento. Mayor que 0.</param>
        /// <param name="vidaUtilDias">Vida útil en días. Mayor que 0.</param>
        /// <param name="precioUnitario">Precio por unidad. 0 o más.</param>
        /// <returns>Los parámetros del producto ya actualizados.</returns>
        /// <exception cref="ReglaDeNegocioException">Si algún valor no es válido (400).</exception>
        public ParametroProducto ConfigurarProducto(
            TipoProducto tipoProducto,
            string? unidad,
            decimal litrosPorUnidad,
            int vidaUtilDias,
            decimal precioUnitario)
        {
            var producto = _productos.FirstOrDefault(p => p.TipoProducto == tipoProducto);
            if (producto is null)
                return AgregarProducto(tipoProducto, unidad ?? string.Empty, litrosPorUnidad, vidaUtilDias, precioUnitario);

            producto.Actualizar(unidad ?? producto.Unidad, litrosPorUnidad, vidaUtilDias, precioUnitario);
            return producto;
        }

        /// <summary>Busca los parámetros de un producto.</summary>
        /// <exception cref="RecursoNoEncontradoException">Si el producto no está configurado (404).</exception>
        public ParametroProducto ObtenerProducto(TipoProducto tipoProducto) =>
            _productos.FirstOrDefault(p => p.TipoProducto == tipoProducto)
                ?? throw new RecursoNoEncontradoException($"El producto {tipoProducto} no está configurado en la granja.");

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

        /// <summary>
        /// Genera la producción de leche de un día (HU-07). Es la prueba mínima del taller:
        /// la producción diaria es n × l y no se registra dos veces la misma fecha (RN-01).
        /// </summary>
        /// <param name="fecha">Día del ordeño.</param>
        /// <param name="litrosEspecificos">
        /// Litros por vaca (VacaId → litros). Si viene vacío, cada vaca activa da l litros
        /// y el total es n × l. Si trae datos, solo se registran esas vacas con esos litros.
        /// </param>
        /// <param name="vacasConProduccionEseDia">Ids de las vacas que ya tienen registro en esa fecha.</param>
        /// <returns>Los registros nuevos, uno por vaca. Todavía no están guardados.</returns>
        /// <exception cref="RecursoNoEncontradoException">Una vaca no existe o está retirada (404).</exception>
        /// <exception cref="ReglaDeNegocioException">Litros negativos (400).</exception>
        /// <exception cref="ProduccionDuplicadaException">Alguna vaca ya tenía producción ese día (409).</exception>
        /// <remarks>
        /// Primero valida todo y luego crea los registros: si una sola vaca falla, no se
        /// registra ninguna (criterio 4: "no se guarda nada").
        /// </remarks>
        public IReadOnlyList<ProduccionLeche> GenerarProduccionDiaria(
            DateOnly fecha,
            IReadOnlyDictionary<int, decimal> litrosEspecificos,
            IReadOnlySet<int> vacasConProduccionEseDia)
        {
            var vacasActivas = _vacas.Where(v => v.Activa).ToList();

            // Qué vaca da cuántos litros.
            List<(Vaca Vaca, decimal Litros)> ordenos;
            if (litrosEspecificos.Count == 0)
            {
                ordenos = vacasActivas.Select(v => (v, LitrosPorVaca)).ToList();
            }
            else
            {
                ordenos = new();
                foreach (var (vacaId, litros) in litrosEspecificos)
                {
                    var vaca = vacasActivas.FirstOrDefault(v => v.Id == vacaId)
                        ?? throw new RecursoNoEncontradoException(
                            $"No existe una vaca activa con id {vacaId}. No se puede registrar su producción.");
                    ordenos.Add((vaca, litros));
                }
            }

            var repetidas = ordenos.Where(o => vacasConProduccionEseDia.Contains(o.Vaca.Id)).ToList();
            if (repetidas.Count > 0)
                throw new ProduccionDuplicadaException(fecha, repetidas.Select(o => o.Vaca.Nombre));

            // El constructor de ProduccionLeche rechaza los litros negativos. Como se valida
            // todo antes de devolver, si una falla no queda ninguna a medias.
            return ordenos
                .Select(o => new ProduccionLeche(o.Vaca.Id, fecha, o.Litros))
                .ToList();
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
