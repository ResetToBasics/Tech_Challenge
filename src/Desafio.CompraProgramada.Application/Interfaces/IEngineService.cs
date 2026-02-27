using Desafio.CompraProgramada.Application.DTOs;

namespace Desafio.CompraProgramada.Application.Interfaces;

public interface IEngineService
{
    Task<ExecucaoCompraResponse> ExecutarCompraAsync(DateOnly dataReferencia, CancellationToken cancellationToken = default);
    Task<ExecucaoRebalanceamentoResponse> RebalancearPorDesvioAsync(DateOnly dataReferencia, decimal limiarDesvioPontosPercentuais, CancellationToken cancellationToken = default);
}
