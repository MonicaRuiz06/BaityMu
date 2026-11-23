using BaityMu.Domain.Entities;
using BaityMu.Domain.Exceptions;
using BaityMu.Domain.Interfaces;
using BaityMu.Services.Dtos;
using BaityMu.Services.Interfaces;
using BaityMu.Services.Modelos;

namespace BaityMu.Services
{
    /// <summary>
    /// Servicio de la producción de leche: registra lo que dio cada vaca cada día.
    /// </summary>
    /// <remarks>
    /// Igual que GranjaService, no tiene reglas propias: las reglas de n × l y de no
    /// duplicar fechas están en Granja.GenerarProduccionDiaria.
    /// </remarks>
    public sealed class ProduccionService : IProduccionService
    {
        private readonly IGranjaRepository _granjas;
        private readonly IProduccionRepository _producciones;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IClock _reloj;

        /// <summary>Recibe sus dependencias por inyección de dependencias.</summary>
        public ProduccionService(
            IGranjaRepository granjas,
            IProduccionRepository producciones,
            IUnitOfWork unitOfWork,
            IClock reloj)
        {
            _granjas = granjas;
            _producciones = producciones;
            _unitOfWork = unitOfWork;
            _reloj = reloj;
        }

        /// <inheritdoc />
        public async Task<ProduccionDiaria> RegistrarAsync(RegistrarProduccionRequest request, CancellationToken ct = default)
        {
            var repetida = request.Vacas.GroupBy(v => v.VacaId).FirstOrDefault(g => g.Count() > 1);
            if (repetida is not null)
                throw new ReglaDeNegocioException($"La vaca con id {repetida.Key} viene más de una vez.");

            var granja = await _granjas.ObtenerCompletaAsync(ct)
                ?? throw new RecursoNoEncontradoException("La granja no está configurada.");

            // RN-10: sin fecha, se usa la de hoy según el reloj (FakeClock en las pruebas).
            var fecha = request.Fecha ?? _reloj.Today;
            var yaRegistradas = await _producciones.VacasConProduccionAsync(fecha, ct);
            var litrosEspecificos = request.Vacas.ToDictionary(v => v.VacaId, v => v.Litros);

            var nuevas = granja.GenerarProduccionDiaria(fecha, litrosEspecificos, yaRegistradas);

            await _producciones.AgregarVariosAsync(nuevas, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return ArmarResultado(fecha, nuevas, granja);
        }

        /// <summary>Junta cada registro con el nombre de su vaca.</summary>
        private static ProduccionDiaria ArmarResultado(
            DateOnly fecha,
            IEnumerable<ProduccionLeche> producciones,
            Granja granja)
        {
            var nombres = granja.Vacas.ToDictionary(v => v.Id, v => v.Nombre);
            var vacas = producciones
                .OrderBy(p => p.VacaId)
                .Select(p => new ProduccionDeVaca(p.VacaId, nombres[p.VacaId], p.Litros))
                .ToList();
            return new ProduccionDiaria(fecha, vacas);
        }
    }
}
