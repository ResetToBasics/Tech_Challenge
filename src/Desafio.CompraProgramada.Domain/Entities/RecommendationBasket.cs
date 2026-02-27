using Desafio.CompraProgramada.Domain.Constants;

namespace Desafio.CompraProgramada.Domain.Entities;

public class RecommendationBasket
{
    private RecommendationBasket()
    {
        Itens = [];
    }

    private RecommendationBasket(string nome, DateTime dataCriacaoUtc)
    {
        Nome = nome.Trim();
        Ativa = true;
        DataCriacaoUtc = dataCriacaoUtc;
    }

    public int Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public bool Ativa { get; private set; }
    public DateTime DataCriacaoUtc { get; private set; }
    public DateTime? DataDesativacaoUtc { get; private set; }
    public ICollection<RecommendationBasketItem> Itens { get; private set; } = [];

    public static RecommendationBasket Criar(string nome, IEnumerable<(string ticker, decimal percentual)> itens, DateTime dataCriacaoUtc)
    {
        var lista = itens.ToList();

        if (lista.Count != DomainConstraints.TamanhoCesta)
        {
            throw new InvalidOperationException($"A cesta deve conter exatamente {DomainConstraints.TamanhoCesta} ativos.");
        }

        if (lista.Any(item => item.percentual <= 0))
        {
            throw new InvalidOperationException("Cada percentual da cesta deve ser maior que 0.");
        }

        if (lista.Select(item => item.ticker.Trim().ToUpperInvariant()).Distinct().Count() != DomainConstraints.TamanhoCesta)
        {
            throw new InvalidOperationException("A cesta nao pode conter ativos duplicados.");
        }

        var soma = lista.Sum(item => item.percentual);
        if (Math.Abs(soma - 100m) > 0.0001m)
        {
            throw new InvalidOperationException($"A soma dos percentuais deve ser exatamente 100%. Soma atual: {soma:N2}%.");
        }

        var basket = new RecommendationBasket(nome, dataCriacaoUtc);
        foreach (var (ticker, percentual) in lista)
        {
            basket.Itens.Add(new RecommendationBasketItem(ticker, percentual));
        }

        return basket;
    }

    public void Desativar(DateTime dataDesativacaoUtc)
    {
        Ativa = false;
        DataDesativacaoUtc = dataDesativacaoUtc;
    }
}
