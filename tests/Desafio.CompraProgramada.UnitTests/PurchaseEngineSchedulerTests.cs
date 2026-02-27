using Desafio.CompraProgramada.Application.DTOs;
using Desafio.CompraProgramada.Application.Interfaces;
using Desafio.CompraProgramada.Infrastructure.Options;
using Desafio.CompraProgramada.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Desafio.CompraProgramada.UnitTests;

public class PurchaseEngineSchedulerTests
{
    [Fact]
    public async Task DeveExecutarCompraAutomaticaQuandoAgendamentoHabilitado()
    {
        var counter = new ExecutionCounter();
        using var provider = BuildServiceProvider(
            new EngineOptions
            {
                HabilitarAgendamentoCompra = true,
                IntervaloMinutosAgendamento = 60
            },
            new FakeClock(new DateTime(2026, 2, 5, 10, 0, 0, DateTimeKind.Utc)),
            counter);

        var scheduler = ActivatorUtilities.CreateInstance<PurchaseEngineScheduler>(provider);

        await scheduler.StartAsync(CancellationToken.None);
        await Task.Delay(120);
        await scheduler.StopAsync(CancellationToken.None);

        counter.Executions.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task NaoDeveExecutarQuandoAgendamentoDesabilitado()
    {
        var counter = new ExecutionCounter();
        using var provider = BuildServiceProvider(
            new EngineOptions
            {
                HabilitarAgendamentoCompra = false,
                IntervaloMinutosAgendamento = 1
            },
            new FakeClock(new DateTime(2026, 2, 5, 10, 0, 0, DateTimeKind.Utc)),
            counter);

        var scheduler = ActivatorUtilities.CreateInstance<PurchaseEngineScheduler>(provider);

        await scheduler.StartAsync(CancellationToken.None);
        await Task.Delay(120);
        await scheduler.StopAsync(CancellationToken.None);

        counter.Executions.Should().Be(0);
    }

    private static ServiceProvider BuildServiceProvider(EngineOptions options, IClock clock, ExecutionCounter counter)
    {
        var services = new ServiceCollection();

        var engine = new FakeEngineService(counter);

        services.AddSingleton<IClock>(clock);
        services.AddSingleton(Options.Create(options));
        services.AddSingleton<ILogger<PurchaseEngineScheduler>>(NullLogger<PurchaseEngineScheduler>.Instance);
        services.AddSingleton<IEngineService>(engine);
        services.AddScoped<IEngineService>(_ => engine);

        return services.BuildServiceProvider();
    }

    private sealed class ExecutionCounter
    {
        public int Executions;
    }

    private sealed class FakeClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow { get; } = utcNow;
    }

    private sealed class FakeEngineService(ExecutionCounter counter) : IEngineService
    {
        public Task<ExecucaoCompraResponse> ExecutarCompraAsync(DateOnly dataReferencia, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref counter.Executions);
            return Task.FromResult(new ExecucaoCompraResponse(
                DateTime.UtcNow,
                0,
                0,
                [],
                [],
                [],
                0,
                "OK"));
        }

        public Task<ExecucaoRebalanceamentoResponse> RebalancearPorDesvioAsync(DateOnly dataReferencia, decimal limiarDesvioPontosPercentuais, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ExecucaoRebalanceamentoResponse(DateTime.UtcNow, "DESVIO_PROPORCAO", 0, 0, 0, 0, "OK"));
        }
    }
}
