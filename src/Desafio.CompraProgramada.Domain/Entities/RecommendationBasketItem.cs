namespace Desafio.CompraProgramada.Domain.Entities;

public class RecommendationBasketItem
{
    private RecommendationBasketItem()
    {
    }

    public RecommendationBasketItem(string ticker, decimal percentual)
    {
        Ticker = ticker.Trim().ToUpperInvariant();
        Percentual = percentual;
    }

    public int Id { get; private set; }
    public int RecommendationBasketId { get; private set; }
    public string Ticker { get; private set; } = string.Empty;
    public decimal Percentual { get; private set; }
}
