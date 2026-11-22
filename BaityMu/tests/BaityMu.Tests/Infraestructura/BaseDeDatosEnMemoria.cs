using BaityMu.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BaityMu.Tests.Infraestructura
{
    public static class BaseDeDatosEnMemoria
    {
        public static BaityMuDbContext Crear()
        {
            var opciones = new DbContextOptionsBuilder<BaityMuDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new BaityMuDbContext(opciones);
        }
    }
}
