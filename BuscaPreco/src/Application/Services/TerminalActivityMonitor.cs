using System;
using System.Threading;
using BuscaPreco.Application.Interfaces;

namespace BuscaPreco.Application.Services
{
    public class TerminalActivityMonitor : ITerminalActivityMonitor
    {
        private long _lastActivityUtcTicks = DateTime.UtcNow.Ticks;

        public DateTime LastActivityUtc => new DateTime(Interlocked.Read(ref _lastActivityUtcTicks), DateTimeKind.Utc);

        public void MarkActivity()
        {
            Interlocked.Exchange(ref _lastActivityUtcTicks, DateTime.UtcNow.Ticks);
        }
    }
}
