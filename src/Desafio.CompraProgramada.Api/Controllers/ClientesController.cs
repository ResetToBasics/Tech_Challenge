using Desafio.CompraProgramada.Application.DTOs;
using Desafio.CompraProgramada.Application.Interfaces;
using Desafio.CompraProgramada.Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace Desafio.CompraProgramada.Api.Controllers;

[ApiController]
[Route("api/clientes")]
public class ClientesController(IClientService clientService) : ControllerBase
{
    [HttpPost("adesao")]
    [ProducesResponseType(typeof(AdesaoClienteResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Aderir([FromBody] AdesaoClienteRequest request, CancellationToken cancellationToken)
    {
        var response = await clientService.AderirAsync(request, cancellationToken);
        return CreatedAtAction(nameof(ObterCarteira), new { clienteId = response.ClienteId }, response);
    }

    [HttpPost("{clienteId:int}/saida")]
    [ProducesResponseType(typeof(SaidaClienteResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Sair(int clienteId, CancellationToken cancellationToken)
    {
        var response = await clientService.SairAsync(clienteId, cancellationToken);
        return Ok(response);
    }

    [HttpPut("{clienteId:int}/valor-mensal")]
    [ProducesResponseType(typeof(AlteracaoValorMensalResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> AlterarValorMensal(int clienteId, [FromBody] AlterarValorMensalApiRequest request, CancellationToken cancellationToken)
    {
        var response = await clientService.AlterarValorMensalAsync(clienteId, request.NovoValorMensal, cancellationToken);
        return Ok(response);
    }

    [HttpGet("{clienteId:int}/carteira")]
    [ProducesResponseType(typeof(CarteiraClienteResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterCarteira(int clienteId, CancellationToken cancellationToken)
    {
        var response = await clientService.ObterCarteiraAsync(clienteId, cancellationToken);
        return Ok(response);
    }

    [HttpGet("{clienteId:int}/rentabilidade")]
    [ProducesResponseType(typeof(RentabilidadeClienteResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterRentabilidade(int clienteId, CancellationToken cancellationToken)
    {
        var response = await clientService.ObterRentabilidadeAsync(clienteId, cancellationToken);
        return Ok(response);
    }
}
