using System.ComponentModel.DataAnnotations;

namespace Desafio.CompraProgramada.Api.Models;

public class AlterarValorMensalApiRequest
{
    [Range(0.01, double.MaxValue)]
    public decimal NovoValorMensal { get; set; }
}
