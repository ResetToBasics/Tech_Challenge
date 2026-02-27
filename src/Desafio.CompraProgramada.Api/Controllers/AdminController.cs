using Desafio.CompraProgramada.Application.DTOs;
using Desafio.CompraProgramada.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Desafio.CompraProgramada.Api.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminController(IAdminService adminService) : ControllerBase
{
    [HttpPost("cesta")]
    [ProducesResponseType(typeof(CestaAdminResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> CriarOuAtualizarCesta([FromBody] CestaAdminRequest request, CancellationToken cancellationToken)
    {
        var response = await adminService.CriarOuAtualizarCestaAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpGet("cesta/atual")]
    [ProducesResponseType(typeof(CestaAtualResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterCestaAtual(CancellationToken cancellationToken)
    {
        var response = await adminService.ObterCestaAtualAsync(cancellationToken);
        return Ok(response);
    }

    [HttpGet("cesta/historico")]
    [ProducesResponseType(typeof(HistoricoCestasResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterHistorico(CancellationToken cancellationToken)
    {
        var response = await adminService.ObterHistoricoCestasAsync(cancellationToken);
        return Ok(response);
    }

    [HttpGet("conta-master/custodia")]
    [ProducesResponseType(typeof(CustodiaMasterResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterCustodiaMaster(CancellationToken cancellationToken)
    {
        var response = await adminService.ObterCustodiaMasterAsync(cancellationToken);
        return Ok(response);
    }
}
