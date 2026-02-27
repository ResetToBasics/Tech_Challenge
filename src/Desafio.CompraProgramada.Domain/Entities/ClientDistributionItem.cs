namespace Desafio.CompraProgramada.Domain.Entities;

public class ClientDistributionItem
{
    private ClientDistributionItem()
    {
    }

    public ClientDistributionItem(string ticker, int quantidade)
    {
        Ticker = ticker.Trim().ToUpperInvariant();
        Quantidade = quantidade;
    }

    public int Id { get; private set; }
    public int ClientDistributionId { get; private set; }
    public string Ticker { get; private set; } = string.Empty;
    public int Quantidade { get; private set; }
}
