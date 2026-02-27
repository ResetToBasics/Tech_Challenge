namespace Desafio.CompraProgramada.Application.DTOs;

public record AdesaoClienteRequest(string Nome, string Cpf, string Email, decimal ValorMensal);

public record ContaGraficaResponse(int Id, string NumeroConta, string Tipo, DateTime DataCriacao);

public record AdesaoClienteResponse(
    int ClienteId,
    string Nome,
    string Cpf,
    string Email,
    decimal ValorMensal,
    bool Ativo,
    DateTime DataAdesao,
    ContaGraficaResponse ContaGrafica);

public record SaidaClienteResponse(int ClienteId, string Nome, bool Ativo, DateTime DataSaida, string Mensagem);

public record AlteracaoValorMensalResponse(
    int ClienteId,
    decimal ValorMensalAnterior,
    decimal ValorMensalNovo,
    DateTime DataAlteracao,
    string Mensagem);

public record ResumoCarteiraResponse(decimal ValorTotalInvestido, decimal ValorAtualCarteira, decimal PlTotal, decimal RentabilidadePercentual);

public record AtivoCarteiraResponse(
    string Ticker,
    int Quantidade,
    decimal PrecoMedio,
    decimal CotacaoAtual,
    decimal ValorAtual,
    decimal Pl,
    decimal PlPercentual,
    decimal ComposicaoCarteira);

public record CarteiraClienteResponse(
    int ClienteId,
    string Nome,
    string ContaGrafica,
    DateTime DataConsulta,
    ResumoCarteiraResponse Resumo,
    IReadOnlyList<AtivoCarteiraResponse> Ativos);

public record HistoricoAporteResponse(DateOnly Data, decimal Valor, string Parcela);

public record EvolucaoCarteiraResponse(DateOnly Data, decimal ValorCarteira, decimal ValorInvestido, decimal Rentabilidade);

public record RentabilidadeResumoResponse(decimal ValorTotalInvestido, decimal ValorAtualCarteira, decimal PlTotal, decimal RentabilidadePercentual);

public record RentabilidadeClienteResponse(
    int ClienteId,
    string Nome,
    DateTime DataConsulta,
    RentabilidadeResumoResponse Rentabilidade,
    IReadOnlyList<HistoricoAporteResponse> HistoricoAportes,
    IReadOnlyList<EvolucaoCarteiraResponse> EvolucaoCarteira);
