using BaityMu.Domain.Exceptions;

namespace BaityMu.Domain.Entities
{
    /// <summary>
    /// Litros de leche que dio una vaca en un día. Es la trazabilidad de la producción
    /// (criterio 5 de la HU-01): cada fila es un ordeño de una vaca en una fecha.
    /// </summary>
    /// <remarks>
    /// Se creó en la HU-02 para el listado de vacas. Los registros diarios los genera
    /// <see cref="Granja.GenerarProduccionDiaria"/> (HU-07, RN-01).
    /// </remarks>
    public class ProduccionLeche
    {
        /// <summary>Id asignado por la base de datos.</summary>
        public int Id { get; private set; }

        /// <summary>Vaca que dio la leche.</summary>
        public int VacaId { get; private set; }

        /// <summary>Día del ordeño.</summary>
        public DateOnly Fecha { get; private set; }

        /// <summary>Litros producidos ese día. Nunca negativos.</summary>
        public decimal Litros { get; private set; }

        // Constructor vacío privado requerido por EF Core
        private ProduccionLeche() { }

        /// <summary>Registra los litros que dio una vaca en una fecha.</summary>
        /// <param name="vacaId">Id de la vaca.</param>
        /// <param name="fecha">Día del ordeño.</param>
        /// <param name="litros">Litros producidos. Deben ser 0 o más.</param>
        /// <exception cref="ReglaDeNegocioException">Si los litros son negativos.</exception>
        public ProduccionLeche(int vacaId, DateOnly fecha, decimal litros)
        {
            if (litros < 0)
                throw new ReglaDeNegocioException("Los litros producidos no pueden ser negativos.");

            VacaId = vacaId;
            Fecha = fecha;
            Litros = litros;
        }
    }
}
