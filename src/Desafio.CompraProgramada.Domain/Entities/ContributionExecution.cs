namespace Desafio.CompraProgramada.Domain.Entities;

public class ContributionExecution
{
    private ContributionExecution()
    {
    }

    public ContributionExecution(int clientId, DateOnly dataReferencia, decimal valor, string parcela)
    {
        ClientId = clientId;
        DataReferencia = dataReferencia;
        Valor = valor;
        Parcela = parcela;
    }

    public int Id { get; private set; }
    public int ClientId { get; private set; }
    public DateOnly DataReferencia { get; private set; }
    public decimal Valor { get; private set; }
    public string Parcela { get; private set; } = string.Empty;
}
