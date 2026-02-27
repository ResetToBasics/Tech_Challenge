namespace Desafio.CompraProgramada.Domain.Entities;

public class ClientDistribution
{
    private ClientDistribution()
    {
        Ativos = [];
    }

    public ClientDistribution(int clientId, string nomeCliente, decimal valorAporte)
    {
        ClientId = clientId;
        NomeCliente = nomeCliente;
        ValorAporte = valorAporte;
    }

    public int Id { get; private set; }
    public int PurchaseExecutionId { get; private set; }
    public int ClientId { get; private set; }
    public string NomeCliente { get; private set; } = string.Empty;
    public decimal ValorAporte { get; private set; }

    public ICollection<ClientDistributionItem> Ativos { get; private set; } = [];

    public void AdicionarAtivo(string ticker, int quantidade)
    {
        Ativos.Add(new ClientDistributionItem(ticker, quantidade));
    }
}
