using BaityMu.Domain.Interfaces;

namespace BaityMu.Tests.Fakes
{
    public sealed class FakeClock : IClock
    {
        public DateOnly Today { get; set; }

        public FakeClock(DateOnly hoy)
        {
            Today = hoy;
        }

        public void AvanzarDias(int dias)
        {
            Today = Today.AddDays(dias);
        }
    }
}