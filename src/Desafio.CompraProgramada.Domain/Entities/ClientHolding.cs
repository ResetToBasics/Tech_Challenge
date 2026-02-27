namespace Desafio.CompraProgramada.Domain.Entities;

public class ClientHolding
{
    private ClientHolding()
    {
    }

    public ClientHolding(int clientId, string ticker)
    {
        ClientId = clientId;
        Ticker = ticker.Trim().ToUpperInvariant();
    }

    public int Id { get; private set; }
    public int ClientId { get; private set; }
    public string Ticker { get; private set; } = string.Empty;
    public int Quantidade { get; private set; }
    public decimal PrecoMedio { get; private set; }

    public void AdicionarCompra(int quantidade, decimal precoUnitario)
    {
        if (quantidade <= 0)
        {
            return;
        }

        if (Quantidade == 0)
        {
            Quantidade = quantidade;
            PrecoMedio = precoUnitario;
            return;
        }

        var custoAnterior = Quantidade * PrecoMedio;
        var custoNovo = quantidade * precoUnitario;

        Quantidade += quantidade;
        PrecoMedio = (custoAnterior + custoNovo) / Quantidade;
    }

    public void Vender(int quantidade)
    {
        if (quantidade <= 0)
        {
            return;
        }

        if (quantidade > Quantidade)
        {
            throw new InvalidOperationException("Quantidade de venda maior que a posicao em custodia.");
        }

        Quantidade -= quantidade;

        if (Quantidade == 0)
        {
            PrecoMedio = 0;
        }
    }
}
