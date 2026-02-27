using Desafio.CompraProgramada.Application.Abstractions;
using Desafio.CompraProgramada.Application.Exceptions;
using Desafio.CompraProgramada.Application.Interfaces;
using Desafio.CompraProgramada.Infrastructure.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Desafio.CompraProgramada.Infrastructure.Services;

public class PurchaseEngineScheduler : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IClock _clock;
    private readonly EngineOptions _options;
    private readonly ILogger<PurchaseEngineScheduler> _logger;

    public PurchaseEngineScheduler(
        IServiceScopeFactory scopeFactory,
        IClock clock,
        IOptions<EngineOptions> options,
        ILogger<PurchaseEngineScheduler> logger)
    {
        _scopeFactory = scopeFactory;
        _clock = clock;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.HabilitarAgendamentoCompra)
        {
            _logger.LogInformation("Agendamento automatico do motor de compra desabilitado.");
            return;
        }

        var intervalo = TimeSpan.FromMinutes(Math.Max(1, _options.IntervaloMinutosAgendamento));

        _logger.LogInformation(
            "Agendamento automatico do motor de compra habilitado. Intervalo: {IntervaloMinutos} minuto(s).",
            intervalo.TotalMinutes);

        await TentarExecutarCompraAsync(stoppingToken);

        using var timer = new PeriodicTimer(intervalo);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await TentarExecutarCompraAsync(stoppingToken);
        }
    }

    private async Task TentarExecutarCompraAsync(CancellationToken cancellationToken)
    {
        var dataAtual = DateOnly.FromDateTime(_clock.UtcNow);
        if (!ExecutionDateCalculator.DataEhValidaParaExecucao(dataAtual))
        {
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var engineService = scope.ServiceProvider.GetRequiredService<IEngineService>();

        try
        {
            var resultado = await engineService.ExecutarCompraAsync(dataAtual, cancellationToken);
            _logger.LogInformation(
                "Compra automatica executada. Data: {Data}. Clientes: {TotalClientes}. Consolidado: {TotalConsolidado}.",
                dataAtual,
                resultado.TotalClientes,
                resultado.TotalConsolidado);
        }
        catch (BusinessException exception) when (exception.Codigo == "COMPRA_JA_EXECUTADA")
        {
            _logger.LogDebug("Compra automatica ja executada para a data {Data}.", dataAtual);
        }
        catch (BusinessException exception)
        {
            _logger.LogWarning(
                "Agendador nao conseguiu executar compra automatica para {Data}. Codigo: {Codigo}. Mensagem: {Mensagem}",
                dataAtual,
                exception.Codigo,
                exception.Message);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Erro inesperado ao executar compra automatica para {Data}.", dataAtual);
        }
    }
}
