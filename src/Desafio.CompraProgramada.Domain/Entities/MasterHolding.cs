namespace Desafio.CompraProgramada.Domain.Entities;

public class MasterHolding
{
    private MasterHolding()
    {
    }

    public MasterHolding(string ticker)
    {
        Ticker = ticker.Trim().ToUpperInvariant();
        Origem = "Inicial";
        AtualizadoEmUtc = DateTime.UtcNow;
    }

    public int Id { get; private set; }
    public string Ticker { get; private set; } = string.Empty;
    public int Quantidade { get; private set; }
    public decimal PrecoMedio { get; private set; }
    public string Origem { get; private set; } = string.Empty;
    public DateTime AtualizadoEmUtc { get; private set; }

    public void Adicionar(int quantidade, decimal precoUnitario, string origem, DateTime atualizadoEmUtc)
    {
        if (quantidade <= 0)
        {
            return;
        }

        if (Quantidade == 0)
        {
            Quantidade = quantidade;
            PrecoMedio = precoUnitario;
            Origem = origem;
            AtualizadoEmUtc = atualizadoEmUtc;
            return;
        }

        var custoAnterior = Quantidade * PrecoMedio;
        var custoNovo = quantidade * precoUnitario;

        Quantidade += quantidade;
        PrecoMedio = (custoAnterior + custoNovo) / Quantidade;
        Origem = origem;
        AtualizadoEmUtc = atualizadoEmUtc;
    }

    public void Remover(int quantidade, string origem, DateTime atualizadoEmUtc)
    {
        if (quantidade <= 0)
        {
            return;
        }

        if (quantidade > Quantidade)
        {
            throw new InvalidOperationException("Quantidade para remocao maior que o saldo na conta master.");
        }

        Quantidade -= quantidade;
        Origem = origem;
        AtualizadoEmUtc = atualizadoEmUtc;

        if (Quantidade == 0)
        {
            PrecoMedio = 0;
        }
    }
}
