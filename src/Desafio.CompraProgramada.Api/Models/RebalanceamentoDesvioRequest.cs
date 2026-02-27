using System.ComponentModel.DataAnnotations;

namespace Desafio.CompraProgramada.Api.Models;

public class RebalanceamentoDesvioRequest
{
    [Required]
    public DateOnly DataReferencia { get; set; }

    [Range(0.1, 100)]
    public decimal? LimiarDesvioPontosPercentuais { get; set; }
}
