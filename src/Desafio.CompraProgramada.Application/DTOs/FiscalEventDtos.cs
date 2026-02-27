namespace Desafio.CompraProgramada.Application.DTOs;

public record FiscalSaleDetail(string Ticker, int Quantidade, decimal PrecoVenda, decimal PrecoMedio, decimal Lucro);

public record FiscalEventMessage(
    string Tipo,
    int ClienteId,
    string Cpf,
    string? Ticker,
    string TipoOperacao,
    int Quantidade,
    decimal PrecoUnitario,
    decimal ValorOperacao,
    decimal Aliquota,
    decimal ValorIr,
    DateTime DataOperacao,
    string? MesReferencia = null,
    decimal? TotalVendasMes = null,
    decimal? LucroLiquido = null,
    IReadOnlyList<FiscalSaleDetail>? Detalhes = null);
