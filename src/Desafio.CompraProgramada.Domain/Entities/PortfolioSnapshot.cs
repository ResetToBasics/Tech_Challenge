namespace Desafio.CompraProgramada.Domain.Entities;

public class PortfolioSnapshot
{
    private PortfolioSnapshot()
    {
    }

    public PortfolioSnapshot(int clientId, DateOnly dataReferencia, decimal valorCarteira, decimal valorInvestido, decimal rentabilidade)
    {
        ClientId = clientId;
        DataReferencia = dataReferencia;
        ValorCarteira = valorCarteira;
        ValorInvestido = valorInvestido;
        Rentabilidade = rentabilidade;
    }

    public int Id { get; private set; }
    public int ClientId { get; private set; }
    public DateOnly DataReferencia { get; private set; }
    public decimal ValorCarteira { get; private set; }
    public decimal ValorInvestido { get; private set; }
    public decimal Rentabilidade { get; private set; }
}
