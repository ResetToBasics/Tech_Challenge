using Desafio.CompraProgramada.Infrastructure.Cotacoes;
using Desafio.CompraProgramada.Infrastructure.Options;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Desafio.CompraProgramada.UnitTests;

public class CotahistQuoteProviderTests
{
    [Fact]
    public async Task DeveLerCotacoesDoArquivoCotahist()
    {
        var options = Options.Create(new CotacaoOptions
        {
            PastaCotacoes = ObterPastaCotacoes()
        });

        var provider = new CotahistQuoteProvider(options, NullLogger<CotahistQuoteProvider>.Instance);

        var preco = await provider.ObterCotacaoFechamentoAsync("PETR4", new DateOnly(2026, 2, 26));

        preco.Should().Be(35.80m);
    }

    [Fact]
    public async Task DeveFalharQuandoTickerNaoExiste()
    {
        var options = Options.Create(new CotacaoOptions
        {
            PastaCotacoes = ObterPastaCotacoes()
        });

        var provider = new CotahistQuoteProvider(options, NullLogger<CotahistQuoteProvider>.Instance);

        var action = async () => await provider.ObterCotacaoFechamentoAsync("XXXX4", new DateOnly(2026, 2, 26));

        await action.Should().ThrowAsync<InvalidOperationException>();
    }

    private static string ObterPastaCotacoes()
    {
        var diretorioAtual = new DirectoryInfo(AppContext.BaseDirectory);

        while (diretorioAtual is not null)
        {
            var candidate = Path.Combine(diretorioAtual.FullName, "cotacoes");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            diretorioAtual = diretorioAtual.Parent;
        }

        throw new DirectoryNotFoundException("Pasta cotacoes nao encontrada para os testes.");
    }
}
