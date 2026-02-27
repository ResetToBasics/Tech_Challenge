using Desafio.CompraProgramada.Api.Models;
using Desafio.CompraProgramada.Application.DTOs;
using Desafio.CompraProgramada.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Desafio.CompraProgramada.Api.Controllers;

[ApiController]
[Route("api/motor")]
public class MotorController(IEngineService engineService) : ControllerBase
{
    [HttpPost("executar-compra")]
    [ProducesResponseType(typeof(ExecucaoCompraResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExecutarCompra([FromBody] ExecucaoCompraRequest request, CancellationToken cancellationToken)
    {
        var response = await engineService.ExecutarCompraAsync(request.DataReferencia, cancellationToken);
        return Ok(response);
    }

    [HttpPost("rebalancear-desvio")]
    [ProducesResponseType(typeof(ExecucaoRebalanceamentoResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> RebalancearDesvio([FromBody] RebalanceamentoDesvioRequest request, CancellationToken cancellationToken)
    {
        var response = await engineService.RebalancearPorDesvioAsync(
            request.DataReferencia,
            request.LimiarDesvioPontosPercentuais ?? 5m,
            cancellationToken);

        return Ok(response);
    }
}
