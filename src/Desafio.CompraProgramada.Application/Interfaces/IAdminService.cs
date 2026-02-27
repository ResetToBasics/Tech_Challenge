using Desafio.CompraProgramada.Application.DTOs;

namespace Desafio.CompraProgramada.Application.Interfaces;

public interface IAdminService
{
    Task<CestaAdminResponse> CriarOuAtualizarCestaAsync(CestaAdminRequest request, CancellationToken cancellationToken = default);
    Task<CestaAtualResponse> ObterCestaAtualAsync(CancellationToken cancellationToken = default);
    Task<HistoricoCestasResponse> ObterHistoricoCestasAsync(CancellationToken cancellationToken = default);
    Task<CustodiaMasterResponse> ObterCustodiaMasterAsync(CancellationToken cancellationToken = default);
}
