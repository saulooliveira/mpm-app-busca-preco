using System;
using System.Threading;
using System.Threading.Tasks;

namespace BuscaPreco.Application.Interfaces
{
    public interface IEmailService
    {
        Task SendDailyReportAsync(DateTime referenceDate, CancellationToken cancellationToken);
    }
}
