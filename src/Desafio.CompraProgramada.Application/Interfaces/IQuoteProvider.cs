namespace Desafio.CompraProgramada.Application.Interfaces;

public interface IQuoteProvider
{
    Task<decimal> ObterCotacaoFechamentoAsync(string ticker, DateOnly dataReferencia, CancellationToken cancellationToken = default);
    Task<IDictionary<string, decimal>> ObterCotacoesFechamentoAsync(IEnumerable<string> tickers, DateOnly dataReferencia, CancellationToken cancellationToken = default);
}
