namespace Desafio.CompraProgramada.Domain.Entities;

public class FiscalEventLog
{
    private FiscalEventLog()
    {
    }

    public FiscalEventLog(
        string tipo,
        int clientId,
        string cpf,
        string? ticker,
        decimal valorOperacao,
        decimal valorIr,
        DateTime dataEventoUtc,
        string payloadJson,
        string? mesReferencia = null)
    {
        Tipo = tipo;
        ClientId = clientId;
        Cpf = cpf;
        Ticker = ticker;
        ValorOperacao = valorOperacao;
        ValorIr = valorIr;
        DataEventoUtc = dataEventoUtc;
        PayloadJson = payloadJson;
        MesReferencia = mesReferencia;
    }

    public int Id { get; private set; }
    public string Tipo { get; private set; } = string.Empty;
    public int ClientId { get; private set; }
    public string Cpf { get; private set; } = string.Empty;
    public string? Ticker { get; private set; }
    public decimal ValorOperacao { get; private set; }
    public decimal ValorIr { get; private set; }
    public string? MesReferencia { get; private set; }
    public string PayloadJson { get; private set; } = string.Empty;
    public DateTime DataEventoUtc { get; private set; }
}
