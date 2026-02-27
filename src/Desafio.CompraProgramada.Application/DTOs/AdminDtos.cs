namespace Desafio.CompraProgramada.Application.DTOs;

public record CestaItemRequest(string Ticker, decimal Percentual);

public record CestaAdminRequest(string Nome, IReadOnlyList<CestaItemRequest> Itens);

public record CestaItemResponse(string Ticker, decimal Percentual);

public record CestaItemCotacaoResponse(string Ticker, decimal Percentual, decimal CotacaoAtual);

public record CestaAnteriorResponse(int CestaId, string Nome, DateTime DataDesativacao);

public record CestaAdminResponse(
    int CestaId,
    string Nome,
    bool Ativa,
    DateTime DataCriacao,
    IReadOnlyList<CestaItemResponse> Itens,
    bool RebalanceamentoDisparado,
    CestaAnteriorResponse? CestaAnteriorDesativada,
    IReadOnlyList<string> AtivosRemovidos,
    IReadOnlyList<string> AtivosAdicionados,
    string Mensagem);

public record CestaAtualResponse(int CestaId, string Nome, bool Ativa, DateTime DataCriacao, IReadOnlyList<CestaItemCotacaoResponse> Itens);

public record CestaHistoricoItemResponse(
    int CestaId,
    string Nome,
    bool Ativa,
    DateTime DataCriacao,
    DateTime? DataDesativacao,
    IReadOnlyList<CestaItemResponse> Itens);

public record HistoricoCestasResponse(IReadOnlyList<CestaHistoricoItemResponse> Cestas);

public record ContaMasterResponse(int Id, string NumeroConta, string Tipo);

public record CustodiaMasterItemResponse(string Ticker, int Quantidade, decimal PrecoMedio, decimal ValorAtual, string Origem);

public record CustodiaMasterResponse(ContaMasterResponse ContaMaster, IReadOnlyList<CustodiaMasterItemResponse> Custodia, decimal ValorTotalResiduo);
