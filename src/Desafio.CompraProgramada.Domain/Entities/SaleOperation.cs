using Desafio.CompraProgramada.Domain.Enums;

namespace Desafio.CompraProgramada.Domain.Entities;

public class SaleOperation
{
    private SaleOperation()
    {
    }

    public SaleOperation(
        int clientId,
        string ticker,
        int quantidade,
        decimal precoVendaUnitario,
        decimal precoMedio,
        DateTime dataVendaUtc,
        RebalanceTriggerType gatilho)
    {
        ClientId = clientId;
        Ticker = ticker.Trim().ToUpperInvariant();
        Quantidade = quantidade;
        PrecoVendaUnitario = precoVendaUnitario;
        PrecoMedio = precoMedio;
        DataVendaUtc = dataVendaUtc;
        MesReferencia = dataVendaUtc.ToString("yyyy-MM");
        Gatilho = gatilho;

        ValorTotal = quantidade * precoVendaUnitario;
        Lucro = quantidade * (precoVendaUnitario - precoMedio);
    }

    public int Id { get; private set; }
    public int ClientId { get; private set; }
    public string Ticker { get; private set; } = string.Empty;
    public int Quantidade { get; private set; }
    public decimal PrecoVendaUnitario { get; private set; }
    public decimal PrecoMedio { get; private set; }
    public decimal ValorTotal { get; private set; }
    public decimal Lucro { get; private set; }
    public DateTime DataVendaUtc { get; private set; }
    public string MesReferencia { get; private set; } = string.Empty;
    public RebalanceTriggerType Gatilho { get; private set; }
}
