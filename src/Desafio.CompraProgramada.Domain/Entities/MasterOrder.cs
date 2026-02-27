namespace Desafio.CompraProgramada.Domain.Entities;

public class MasterOrder
{
    private MasterOrder()
    {
    }

    public MasterOrder(string ticker, int quantidadeTotal, int quantidadeLotePadrao, int quantidadeFracionaria, decimal precoUnitario)
    {
        Ticker = ticker.Trim().ToUpperInvariant();
        QuantidadeTotal = quantidadeTotal;
        QuantidadeLotePadrao = quantidadeLotePadrao;
        QuantidadeFracionaria = quantidadeFracionaria;
        PrecoUnitario = precoUnitario;
        ValorTotal = precoUnitario * quantidadeTotal;
    }

    public int Id { get; private set; }
    public int PurchaseExecutionId { get; private set; }
    public string Ticker { get; private set; } = string.Empty;
    public int QuantidadeTotal { get; private set; }
    public int QuantidadeLotePadrao { get; private set; }
    public int QuantidadeFracionaria { get; private set; }
    public decimal PrecoUnitario { get; private set; }
    public decimal ValorTotal { get; private set; }
}
