namespace Desafio.CompraProgramada.Domain.Entities;

public class ContributionChange
{
    private ContributionChange()
    {
    }

    public ContributionChange(int clientId, decimal valorAnterior, decimal valorNovo, DateTime dataAlteracaoUtc)
    {
        ClientId = clientId;
        ValorAnterior = valorAnterior;
        ValorNovo = valorNovo;
        DataAlteracaoUtc = dataAlteracaoUtc;
    }

    public int Id { get; private set; }
    public int ClientId { get; private set; }
    public decimal ValorAnterior { get; private set; }
    public decimal ValorNovo { get; private set; }
    public DateTime DataAlteracaoUtc { get; private set; }
}
