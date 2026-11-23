using BaityMu.Domain.Entities;
using BaityMu.Domain.Interfaces;
using BaityMu.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BaityMu.Infrastructure.Seed
{
    /// <summary>
    /// Siembra (seed) los datos iniciales: crea la granja de Baity con sus n vacas y los
    /// parámetros del queso y el yogurt la primera vez que arranca la aplicación.
    /// </summary>
    /// <remarks>
    /// ¿Qué es sembrar datos?
    /// Llenar la base de datos con lo mínimo para que la aplicación funcione. Aquí es la
    /// granja con n vacas (BaityCow, BaityCow 2... BaityCow 10), así el escenario de
    /// referencia del taller arranca con 10 vacas × 15 L = 150 L.
    ///
    /// Es idempotente: se puede ejecutar en cada arranque sin duplicar nada.
    /// - Si no hay granja, la crea con sus vacas y productos.
    /// - Si ya hay granja, solo agrega los productos que le falten (por ejemplo, en una
    ///   base creada antes de la HU-05). Nunca cambia valores que ya existen: eso se
    ///   hace con PUT /api/granja (HU-06).
    /// </remarks>
    public static class SembradorDeDatos
    {
        /// <summary>
        /// Crea la granja (si no existe) y los productos que le falten.
        /// </summary>
        /// <param name="db">El DbContext de la aplicación.</param>
        /// <param name="opciones">Valores leídos de appsettings.json.</param>
        /// <param name="reloj">Da la fecha de ingreso de las vacas iniciales (hoy).</param>
        /// <param name="ct">Permite cancelar la operación.</param>
        /// <returns>true si sembró algo; false si no había nada que agregar.</returns>
        public static async Task<bool> SembrarAsync(
            BaityMuDbContext db,
            GranjaInicialOptions opciones,
            IClock reloj,
            CancellationToken ct = default)
        {
            var granja = await db.Granjas
                .Include(g => g.Productos)
                .OrderBy(g => g.Id)
                .FirstOrDefaultAsync(ct);

            if (granja is null)
            {
                // Se usan las mismas reglas del dominio: si appsettings.json trae l <= 0 o
                // una raza vacía, la aplicación no arranca con datos inválidos.
                granja = new Granja(opciones.Nombre, opciones.LitrosPorVaca);
                for (var i = 0; i < opciones.NumeroVacas; i++)
                {
                    granja.AgregarVaca(opciones.RazaVacasIniciales, null, reloj.Today);
                }

                await db.Granjas.AddAsync(granja, ct);
            }

            foreach (var producto in opciones.Productos)
            {
                if (granja.Productos.Any(p => p.TipoProducto == producto.TipoProducto))
                    continue;

                granja.AgregarProducto(
                    producto.TipoProducto,
                    producto.Unidad,
                    producto.LitrosPorUnidad,
                    producto.VidaUtilDias,
                    producto.PrecioUnitario);
            }

            return await db.SaveChangesAsync(ct) > 0;
        }
    }
}
