using Desafio.CompraProgramada.Application.DTOs;

namespace Desafio.CompraProgramada.Application.Interfaces;

public interface IFiscalEventPublisher
{
    Task PublicarAsync(FiscalEventMessage evento, CancellationToken cancellationToken = default);
}
