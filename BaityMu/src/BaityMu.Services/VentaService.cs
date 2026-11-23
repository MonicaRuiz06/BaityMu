using BaityMu.Domain.Entities;
using BaityMu.Domain.Exceptions;
using BaityMu.Domain.Interfaces;
using BaityMu.Services.Dtos;
using BaityMu.Services.Interfaces;

namespace BaityMu.Services
{
    /// <summary>
    /// Servicio de ventas. Coordina el FIFO: carga los lotes vendibles, le pide a
    /// <see cref="Venta.Registrar"/> que descuente, y guarda todo en una sola transacción.
    /// </summary>
    /// <remarks>
    /// Atomicidad (RN-07, criterio 9): la venta nueva y los lotes descontados se guardan con
    /// un solo SaveChangesAsync. Si no alcanza el producto, el dominio lanza la excepción
    /// antes de tocar un lote y no se guarda nada.
    /// </remarks>
    public sealed class VentaService : IVentaService
    {
        private readonly IGranjaRepository _granjas;
        private readonly ILoteRepository _lotes;
        private readonly IVentaRepository _ventas;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IClock _reloj;

        /// <summary>Recibe sus dependencias por inyección de dependencias.</summary>
        public VentaService(
            IGranjaRepository granjas,
            ILoteRepository lotes,
            IVentaRepository ventas,
            IUnitOfWork unitOfWork,
            IClock reloj)
        {
            _granjas = granjas;
            _lotes = lotes;
            _ventas = ventas;
            _unitOfWork = unitOfWork;
            _reloj = reloj;
        }

        /// <inheritdoc />
        public async Task<Venta> RegistrarVentaAsync(VentaRequest request, CancellationToken ct = default)
        {
            var granja = await _granjas.ObtenerCompletaAsync(ct)
                ?? throw new RecursoNoEncontradoException("La granja no está configurada.");

            // Criterio 7: un producto que no existe es un dato inválido de la petición (400).
            var producto = granja.Productos.FirstOrDefault(p => p.TipoProducto == request.TipoProducto)
                ?? throw new ReglaDeNegocioException($"El producto {request.TipoProducto} no está configurado en la granja.");

            var fecha = request.Fecha ?? _reloj.Today;
            var vendibles = await _lotes.ObtenerVendiblesAsync(request.TipoProducto, fecha, ct);

            var venta = Venta.Registrar(producto, fecha, request.Cantidad, vendibles);

            await _ventas.AgregarAsync(venta, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return venta;
        }
    }
}
