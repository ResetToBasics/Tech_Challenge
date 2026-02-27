using Desafio.CompraProgramada.Application.DTOs;
using Desafio.CompraProgramada.Application.Exceptions;
using Desafio.CompraProgramada.Application.Interfaces;
using Desafio.CompraProgramada.Infrastructure.Data;
using Desafio.CompraProgramada.Infrastructure.Options;
using Desafio.CompraProgramada.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Desafio.CompraProgramada.UnitTests;

public class TradingPlatformServiceTests
{
    [Fact]
    public async Task DeveExecutarFluxoCompletoDeCompraProgramada()
    {
        var clock = new FakeClock(new DateTime(2026, 2, 5, 10, 0, 0, DateTimeKind.Utc));
        var quoteProvider = new FakeQuoteProvider()
            .WithQuotes(new DateOnly(2026, 2, 5), new Dictionary<string, decimal>
            {
                ["PETR4"] = 35m,
                ["VALE3"] = 62m,
                ["ITUB4"] = 30m,
                ["BBDC4"] = 15m,
                ["WEGE3"] = 40m,
                ["ABEV3"] = 14m,
                ["RENT3"] = 48m
            });

        var publisher = new FakeFiscalPublisher();

        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext, quoteProvider, publisher, clock);

        await service.CriarOuAtualizarCestaAsync(new CestaAdminRequest(
            "Top Five - Fevereiro",
            [
                new CestaItemRequest("PETR4", 30),
                new CestaItemRequest("VALE3", 25),
                new CestaItemRequest("ITUB4", 20),
                new CestaItemRequest("BBDC4", 15),
                new CestaItemRequest("WEGE3", 10)
            ]));

        await service.AderirAsync(new AdesaoClienteRequest("Cliente A", "11111111111", "a@teste.com", 3000));
        await service.AderirAsync(new AdesaoClienteRequest("Cliente B", "22222222222", "b@teste.com", 6000));

        var resultadoCompra = await service.ExecutarCompraAsync(new DateOnly(2026, 2, 5));

        resultadoCompra.TotalClientes.Should().Be(2);
        resultadoCompra.TotalConsolidado.Should().Be(3000m);
        resultadoCompra.OrdensCompra.Should().HaveCount(5);
        resultadoCompra.Distribuicoes.Should().HaveCount(2);
        resultadoCompra.EventosIrPublicados.Should().BeGreaterThan(0);

        var carteiraA = await service.ObterCarteiraAsync(1);
        carteiraA.Ativos.Should().Contain(item => item.Ticker == "PETR4" && item.Quantidade == 8);
        carteiraA.Ativos.Should().Contain(item => item.Ticker == "VALE3" && item.Quantidade == 3);
        carteiraA.Ativos.Should().Contain(item => item.Ticker == "ITUB4" && item.Quantidade == 6);

        dbContext.EventosFiscais.Count(item => item.Tipo == "IR_DEDO_DURO").Should().BeGreaterThan(0);
        publisher.Events.Count(item => item.Tipo == "IR_DEDO_DURO").Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task DeveDispararRebalanceamentoEIrVendaQuandoTrocarCestaComLucroAcimaDoLimite()
    {
        var clock = new FakeClock(new DateTime(2026, 2, 5, 10, 0, 0, DateTimeKind.Utc));
        var quoteProvider = new FakeQuoteProvider()
            .WithQuotes(new DateOnly(2026, 2, 5), new Dictionary<string, decimal>
            {
                ["PETR4"] = 20m,
                ["VALE3"] = 20m,
                ["ITUB4"] = 20m,
                ["BBDC4"] = 10m,
                ["WEGE3"] = 10m,
                ["ABEV3"] = 10m,
                ["RENT3"] = 10m
            })
            .WithQuotes(new DateOnly(2026, 3, 1), new Dictionary<string, decimal>
            {
                ["PETR4"] = 20m,
                ["VALE3"] = 20m,
                ["ITUB4"] = 20m,
                ["BBDC4"] = 22m,
                ["WEGE3"] = 25m,
                ["ABEV3"] = 10m,
                ["RENT3"] = 10m
            });

        var publisher = new FakeFiscalPublisher();

        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext, quoteProvider, publisher, clock);

        await service.CriarOuAtualizarCestaAsync(new CestaAdminRequest(
            "Top Five - Base",
            [
                new CestaItemRequest("BBDC4", 40),
                new CestaItemRequest("WEGE3", 30),
                new CestaItemRequest("PETR4", 10),
                new CestaItemRequest("VALE3", 10),
                new CestaItemRequest("ITUB4", 10)
            ]));

        await service.AderirAsync(new AdesaoClienteRequest("Investidor Grande", "33333333333", "big@teste.com", 900000));
        await service.ExecutarCompraAsync(new DateOnly(2026, 2, 5));

        clock.UtcNow = new DateTime(2026, 3, 1, 10, 0, 0, DateTimeKind.Utc);

        var respostaCesta = await service.CriarOuAtualizarCestaAsync(new CestaAdminRequest(
            "Top Five - Nova",
            [
                new CestaItemRequest("PETR4", 20),
                new CestaItemRequest("VALE3", 20),
                new CestaItemRequest("ITUB4", 20),
                new CestaItemRequest("ABEV3", 20),
                new CestaItemRequest("RENT3", 20)
            ]));

        respostaCesta.RebalanceamentoDisparado.Should().BeTrue();
        respostaCesta.AtivosRemovidos.Should().Contain(new[] { "BBDC4", "WEGE3" });

        dbContext.EventosFiscais.Should().Contain(item => item.Tipo == "IR_VENDA" && item.ValorIr > 0);
        publisher.Events.Should().Contain(item => item.Tipo == "IR_VENDA" && item.ValorIr > 0);
    }

    [Fact]
    public async Task DeveLancarErroQuandoCompraExecutadaForaDaDataValida()
    {
        var clock = new FakeClock(new DateTime(2026, 2, 5, 10, 0, 0, DateTimeKind.Utc));
        var quoteProvider = new FakeQuoteProvider()
            .WithQuotes(new DateOnly(2026, 2, 5), new Dictionary<string, decimal>
            {
                ["PETR4"] = 35m,
                ["VALE3"] = 62m,
                ["ITUB4"] = 30m,
                ["BBDC4"] = 15m,
                ["WEGE3"] = 40m
            });

        var publisher = new FakeFiscalPublisher();

        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext, quoteProvider, publisher, clock);

        await service.CriarOuAtualizarCestaAsync(new CestaAdminRequest(
            "Top Five - Fevereiro",
            [
                new CestaItemRequest("PETR4", 30),
                new CestaItemRequest("VALE3", 25),
                new CestaItemRequest("ITUB4", 20),
                new CestaItemRequest("BBDC4", 15),
                new CestaItemRequest("WEGE3", 10)
            ]));

        await service.AderirAsync(new AdesaoClienteRequest("Cliente A", "44444444444", "c@teste.com", 3000));

        var action = async () => await service.ExecutarCompraAsync(new DateOnly(2026, 2, 6));

        var exception = await action.Should().ThrowAsync<BusinessException>();
        exception.Which.Codigo.Should().Be("DATA_EXECUCAO_INVALIDA");
    }

    [Fact]
    public async Task DeveConsultarCestaAtualHistoricoECustodiaMaster()
    {
        var clock = new FakeClock(new DateTime(2026, 2, 5, 10, 0, 0, DateTimeKind.Utc));
        var quoteProvider = new FakeQuoteProvider()
            .WithQuotes(new DateOnly(2026, 2, 5), new Dictionary<string, decimal>
            {
                ["PETR4"] = 35m,
                ["VALE3"] = 62m,
                ["ITUB4"] = 30m,
                ["BBDC4"] = 15m,
                ["WEGE3"] = 40m
            });

        var publisher = new FakeFiscalPublisher();

        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext, quoteProvider, publisher, clock);

        await service.CriarOuAtualizarCestaAsync(new CestaAdminRequest(
            "Top Five - Fevereiro",
            [
                new CestaItemRequest("PETR4", 30),
                new CestaItemRequest("VALE3", 25),
                new CestaItemRequest("ITUB4", 20),
                new CestaItemRequest("BBDC4", 15),
                new CestaItemRequest("WEGE3", 10)
            ]));

        await service.AderirAsync(new AdesaoClienteRequest("Cliente A", "55555555555", "a@teste.com", 3000));
        await service.ExecutarCompraAsync(new DateOnly(2026, 2, 5));

        var cestaAtual = await service.ObterCestaAtualAsync();
        var historico = await service.ObterHistoricoCestasAsync();
        var custodiaMaster = await service.ObterCustodiaMasterAsync();

        cestaAtual.Itens.Should().HaveCount(5);
        historico.Cestas.Should().HaveCount(1);
        custodiaMaster.ContaMaster.NumeroConta.Should().Be("MST-000001");
    }

    [Fact]
    public async Task DeveExecutarRebalanceamentoPorDesvio()
    {
        var clock = new FakeClock(new DateTime(2026, 2, 5, 10, 0, 0, DateTimeKind.Utc));
        var quoteProvider = new FakeQuoteProvider()
            .WithQuotes(new DateOnly(2026, 2, 5), new Dictionary<string, decimal>
            {
                ["PETR4"] = 35m,
                ["VALE3"] = 62m,
                ["ITUB4"] = 30m,
                ["BBDC4"] = 15m,
                ["WEGE3"] = 40m
            })
            .WithQuotes(new DateOnly(2026, 2, 25), new Dictionary<string, decimal>
            {
                ["PETR4"] = 90m,
                ["VALE3"] = 50m,
                ["ITUB4"] = 25m,
                ["BBDC4"] = 10m,
                ["WEGE3"] = 20m
            });

        var publisher = new FakeFiscalPublisher();

        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext, quoteProvider, publisher, clock);

        await service.CriarOuAtualizarCestaAsync(new CestaAdminRequest(
            "Top Five - Fevereiro",
            [
                new CestaItemRequest("PETR4", 30),
                new CestaItemRequest("VALE3", 25),
                new CestaItemRequest("ITUB4", 20),
                new CestaItemRequest("BBDC4", 15),
                new CestaItemRequest("WEGE3", 10)
            ]));

        await service.AderirAsync(new AdesaoClienteRequest("Cliente A", "66666666666", "a@teste.com", 3000));
        await service.ExecutarCompraAsync(new DateOnly(2026, 2, 5));

        clock.UtcNow = new DateTime(2026, 2, 25, 10, 0, 0, DateTimeKind.Utc);
        var rebalanceamento = await service.RebalancearPorDesvioAsync(new DateOnly(2026, 2, 25), 1m);

        rebalanceamento.Tipo.Should().Be("DESVIO_PROPORCAO");
        rebalanceamento.ClientesProcessados.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task DeveLancarErroQuandoCpfDuplicado()
    {
        var clock = new FakeClock(new DateTime(2026, 2, 5, 10, 0, 0, DateTimeKind.Utc));
        var quoteProvider = new FakeQuoteProvider()
            .WithQuotes(new DateOnly(2026, 2, 5), new Dictionary<string, decimal>
            {
                ["PETR4"] = 35m,
                ["VALE3"] = 62m,
                ["ITUB4"] = 30m,
                ["BBDC4"] = 15m,
                ["WEGE3"] = 40m
            });
        var publisher = new FakeFiscalPublisher();

        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext, quoteProvider, publisher, clock);

        await service.AderirAsync(new AdesaoClienteRequest("Cliente A", "77777777777", "a@teste.com", 3000));

        var action = async () => await service.AderirAsync(new AdesaoClienteRequest("Cliente B", "77777777777", "b@teste.com", 3000));

        var exception = await action.Should().ThrowAsync<BusinessException>();
        exception.Which.Codigo.Should().Be("CLIENTE_CPF_DUPLICADO");
    }

    private static TradingDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TradingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new TradingDbContext(options);
    }

    private static TradingPlatformService CreateService(
        TradingDbContext dbContext,
        IQuoteProvider quoteProvider,
        IFiscalEventPublisher fiscalEventPublisher,
        FakeClock clock)
    {
        return new TradingPlatformService(
            dbContext,
            quoteProvider,
            fiscalEventPublisher,
            clock,
            Options.Create(new EngineOptions { LimiarDesvioPontosPercentuais = 5 }),
            NullLogger<TradingPlatformService>.Instance);
    }

    private sealed class FakeClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow { get; set; } = utcNow;
    }

    private sealed class FakeQuoteProvider : IQuoteProvider
    {
        private readonly SortedDictionary<DateOnly, Dictionary<string, decimal>> _quotesByDate = new();

        public FakeQuoteProvider WithQuotes(DateOnly date, Dictionary<string, decimal> quotes)
        {
            _quotesByDate[date] = new Dictionary<string, decimal>(quotes, StringComparer.OrdinalIgnoreCase);
            return this;
        }

        public Task<decimal> ObterCotacaoFechamentoAsync(string ticker, DateOnly dataReferencia, CancellationToken cancellationToken = default)
        {
            var map = GetMap(dataReferencia);
            if (!map.TryGetValue(ticker, out var value))
            {
                throw new InvalidOperationException($"Ticker nao encontrado: {ticker}");
            }

            return Task.FromResult(value);
        }

        public Task<IDictionary<string, decimal>> ObterCotacoesFechamentoAsync(IEnumerable<string> tickers, DateOnly dataReferencia, CancellationToken cancellationToken = default)
        {
            var map = GetMap(dataReferencia);
            var result = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

            foreach (var ticker in tickers)
            {
                if (!map.TryGetValue(ticker, out var value))
                {
                    throw new InvalidOperationException($"Ticker nao encontrado: {ticker}");
                }

                result[ticker] = value;
            }

            return Task.FromResult<IDictionary<string, decimal>>(result);
        }

        private Dictionary<string, decimal> GetMap(DateOnly dataReferencia)
        {
            var found = _quotesByDate
                .Where(item => item.Key <= dataReferencia)
                .OrderByDescending(item => item.Key)
                .Select(item => item.Value)
                .FirstOrDefault();

            if (found is null)
            {
                throw new InvalidOperationException("Sem cotacoes para a data.");
            }

            return found;
        }
    }

    private sealed class FakeFiscalPublisher : IFiscalEventPublisher
    {
        public List<FiscalEventMessage> Events { get; } = [];

        public Task PublicarAsync(FiscalEventMessage evento, CancellationToken cancellationToken = default)
        {
            Events.Add(evento);
            return Task.CompletedTask;
        }
    }
}
