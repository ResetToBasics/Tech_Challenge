using Desafio.CompraProgramada.Application.DTOs;

namespace Desafio.CompraProgramada.Application.Interfaces;

public interface IClientService
{
    Task<AdesaoClienteResponse> AderirAsync(AdesaoClienteRequest request, CancellationToken cancellationToken = default);
    Task<SaidaClienteResponse> SairAsync(int clienteId, CancellationToken cancellationToken = default);
    Task<AlteracaoValorMensalResponse> AlterarValorMensalAsync(int clienteId, decimal novoValorMensal, CancellationToken cancellationToken = default);
    Task<CarteiraClienteResponse> ObterCarteiraAsync(int clienteId, CancellationToken cancellationToken = default);
    Task<RentabilidadeClienteResponse> ObterRentabilidadeAsync(int clienteId, CancellationToken cancellationToken = default);
}
