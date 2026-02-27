using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using Desafio.CompraProgramada.Application.Interfaces;
using Desafio.CompraProgramada.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Desafio.CompraProgramada.Infrastructure.Cotacoes;

public class CotahistQuoteProvider : IQuoteProvider
{
    private readonly CotacaoOptions _options;
    private readonly ILogger<CotahistQuoteProvider> _logger;
    private readonly ConcurrentDictionary<string, IReadOnlyDictionary<string, decimal>> _cache = new();

    public CotahistQuoteProvider(IOptions<CotacaoOptions> options, ILogger<CotahistQuoteProvider> logger)
    {
        _options = options.Value;
        _logger = logger;

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public async Task<decimal> ObterCotacaoFechamentoAsync(string ticker, DateOnly dataReferencia, CancellationToken cancellationToken = default)
    {
        var cotacoes = await ObterCotacoesFechamentoAsync([ticker], dataReferencia, cancellationToken);
        return cotacoes[NormalizarTicker(ticker)];
    }

    public Task<IDictionary<string, decimal>> ObterCotacoesFechamentoAsync(IEnumerable<string> tickers, DateOnly dataReferencia, CancellationToken cancellationToken = default)
    {
        var requestedTickers = tickers
            .Select(NormalizarTicker)
            .Distinct()
            .ToList();

        if (requestedTickers.Count == 0)
        {
            return Task.FromResult<IDictionary<string, decimal>>(new Dictionary<string, decimal>());
        }

        var pastaCotacoes = Path.GetFullPath(_options.PastaCotacoes);
        if (!Directory.Exists(pastaCotacoes))
        {
            throw new InvalidOperationException($"Pasta de cotacoes nao encontrada: {pastaCotacoes}");
        }

        var arquivos = Directory.GetFiles(pastaCotacoes, "COTAHIST_D*.TXT")
            .Select(caminho => new { caminho, data = ExtrairDataDoNomeArquivo(caminho) })
            .Where(item => item.data is not null)
            .Select(item => new { item.caminho, data = item.data!.Value })
            .Where(item => item.data <= dataReferencia)
            .OrderByDescending(item => item.data)
            .ToList();

        if (arquivos.Count == 0)
        {
            throw new InvalidOperationException($"Arquivo COTAHIST nao encontrado para a data {dataReferencia:yyyy-MM-dd}.");
        }

        var encontradas = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        foreach (var arquivo in arquivos)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var cotacoesArquivo = _cache.GetOrAdd(arquivo.caminho, ParseArquivo);

            foreach (var ticker in requestedTickers)
            {
                if (encontradas.ContainsKey(ticker))
                {
                    continue;
                }

                if (cotacoesArquivo.TryGetValue(ticker, out var preco))
                {
                    encontradas[ticker] = preco;
                }
            }

            if (encontradas.Count == requestedTickers.Count)
            {
                break;
            }
        }

        var faltantes = requestedTickers.Where(ticker => !encontradas.ContainsKey(ticker)).ToList();
        if (faltantes.Count > 0)
        {
            throw new InvalidOperationException($"Cotacao nao encontrada para os tickers: {string.Join(", ", faltantes)}.");
        }

        return Task.FromResult<IDictionary<string, decimal>>(encontradas);
    }

    private IReadOnlyDictionary<string, decimal> ParseArquivo(string caminhoArquivo)
    {
        var mapa = new Dictionary<string, (decimal preco, int prioridade)>(StringComparer.OrdinalIgnoreCase);

        var encoding = Encoding.GetEncoding("ISO-8859-1");
        foreach (var linha in File.ReadLines(caminhoArquivo, encoding))
        {
            if (linha.Length < 245)
            {
                continue;
            }

            var tipreg = linha[..2];
            if (tipreg != "01")
            {
                continue;
            }

            var codBdi = linha.Substring(10, 2).Trim();
            if (codBdi is not ("02" or "96"))
            {
                continue;
            }

            if (!int.TryParse(linha.Substring(24, 3), out var tipoMercado))
            {
                continue;
            }

            if (tipoMercado is not (10 or 20))
            {
                continue;
            }

            var tickerBruto = linha.Substring(12, 12).Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(tickerBruto))
            {
                continue;
            }

            var ticker = NormalizarTicker(tickerBruto);
            var precoFechamento = ParsePreco(linha.Substring(108, 13));
            if (precoFechamento <= 0)
            {
                continue;
            }

            // Prioriza mercado a vista (TPMERC = 010), depois fracionario (020).
            var prioridade = tipoMercado == 10 ? 2 : 1;
            if (!mapa.TryGetValue(ticker, out var atual) || prioridade > atual.prioridade)
            {
                mapa[ticker] = (precoFechamento, prioridade);
            }
        }

        _logger.LogInformation("Arquivo COTAHIST processado: {Arquivo}. Tickers carregados: {Quantidade}", caminhoArquivo, mapa.Count);

        return mapa.ToDictionary(item => item.Key, item => item.Value.preco, StringComparer.OrdinalIgnoreCase);
    }

    private static decimal ParsePreco(string valor)
    {
        if (!long.TryParse(valor.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var valorInteiro))
        {
            return 0;
        }

        return valorInteiro / 100m;
    }

    private static DateOnly? ExtrairDataDoNomeArquivo(string caminho)
    {
        var nome = Path.GetFileNameWithoutExtension(caminho);
        if (nome.Length < 17)
        {
            return null;
        }

        var dataTexto = nome[^8..];
        return DateOnly.TryParseExact(dataTexto, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var data)
            ? data
            : null;
    }

    private static string NormalizarTicker(string ticker)
    {
        var resultado = ticker.Trim().ToUpperInvariant();
        return resultado.EndsWith('F') ? resultado[..^1] : resultado;
    }
}
