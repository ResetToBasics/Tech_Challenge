namespace Desafio.CompraProgramada.Application.DTOs;

public record ExecucaoCompraRequest(DateOnly DataReferencia);

public record OrdemDetalheResponse(string Tipo, string Ticker, int Quantidade);

public record OrdemCompraResponse(
    string Ticker,
    int QuantidadeTotal,
    IReadOnlyList<OrdemDetalheResponse> Detalhes,
    decimal PrecoUnitario,
    decimal ValorTotal);

public record DistribuicaoAtivoResponse(string Ticker, int Quantidade);

public record DistribuicaoClienteResponse(int ClienteId, string Nome, decimal ValorAporte, IReadOnlyList<DistribuicaoAtivoResponse> Ativos);

public record ResiduoMasterResponse(string Ticker, int Quantidade);

public record ExecucaoCompraResponse(
    DateTime DataExecucao,
    int TotalClientes,
    decimal TotalConsolidado,
    IReadOnlyList<OrdemCompraResponse> OrdensCompra,
    IReadOnlyList<DistribuicaoClienteResponse> Distribuicoes,
    IReadOnlyList<ResiduoMasterResponse> ResiduosCustMaster,
    int EventosIrPublicados,
    string Mensagem);

public record ExecucaoRebalanceamentoResponse(
    DateTime DataExecucao,
    string Tipo,
    int ClientesProcessados,
    decimal ValorTotalVendas,
    decimal ValorTotalCompras,
    decimal ValorTotalIrVenda,
    string Mensagem);
