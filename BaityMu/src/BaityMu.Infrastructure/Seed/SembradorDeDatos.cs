using BaityMu.Domain.Entities;
using BaityMu.Domain.Interfaces;
using BaityMu.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BaityMu.Infrastructure.Seed
{
    /// <summary>
    /// Siembra (seed) los datos iniciales: crea la granja de Baity con sus n vacas la
    /// primera vez que arranca la aplicación.
    /// </summary>
    /// <remarks>
    /// ¿Qué es sembrar datos?
    /// Llenar la base de datos con lo mínimo para que la aplicación funcione. Aquí es la
    /// granja con n vacas (BaityCow, BaityCow 2... BaityCow 10), así el escenario de
    /// referencia del taller arranca con 10 vacas × 15 L = 150 L.
    ///
    /// Solo siembra si la tabla Granjas está vacía: los arranques siguientes no hacen nada
    /// y no se duplican datos.
    /// </remarks>
    public static class SembradorDeDatos
    {
        /// <summary>
        /// Crea la granja y sus vacas iniciales si todavía no existe ninguna granja.
        /// </summary>
        /// <param name="db">El DbContext de la aplicación.</param>
        /// <param name="opciones">Valores leídos de appsettings.json.</param>
        /// <param name="reloj">Da la fecha de ingreso de las vacas iniciales (hoy).</param>
        /// <param name="ct">Permite cancelar la operación.</param>
        /// <returns>true si sembró la granja; false si ya existía.</returns>
        public static async Task<bool> SembrarAsync(
            BaityMuDbContext db,
            GranjaInicialOptions opciones,
            IClock reloj,
            CancellationToken ct = default)
        {
            if (await db.Granjas.AnyAsync(ct))
                return false;

            // Se usan las mismas reglas del dominio: si appsettings.json trae l <= 0 o
            // una raza vacía, la aplicación no arranca con datos inválidos.
            var granja = new Granja(opciones.Nombre, opciones.LitrosPorVaca);
            for (var i = 0; i < opciones.NumeroVacas; i++)
            {
                granja.AgregarVaca(opciones.RazaVacasIniciales, null, reloj.Today);
            }

            await db.Granjas.AddAsync(granja, ct);
            await db.SaveChangesAsync(ct);
            return true;
        }
    }
}
