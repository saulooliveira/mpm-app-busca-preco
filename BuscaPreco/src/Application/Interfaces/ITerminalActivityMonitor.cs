using System;

namespace BuscaPreco.Application.Interfaces
{
    public interface ITerminalActivityMonitor
    {
        void MarkActivity();
        DateTime LastActivityUtc { get; }
    }
}
