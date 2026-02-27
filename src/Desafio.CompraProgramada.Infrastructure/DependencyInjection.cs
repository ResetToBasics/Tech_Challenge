using Desafio.CompraProgramada.Application.Interfaces;
using Desafio.CompraProgramada.Infrastructure.Cotacoes;
using Desafio.CompraProgramada.Infrastructure.Data;
using Desafio.CompraProgramada.Infrastructure.Messaging;
using Desafio.CompraProgramada.Infrastructure.Options;
using Desafio.CompraProgramada.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Desafio.CompraProgramada.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<KafkaOptions>(configuration.GetSection("Kafka"));
        services.Configure<CotacaoOptions>(configuration.GetSection("Cotacoes"));
        services.Configure<EngineOptions>(configuration.GetSection("Motor"));

        var connectionString = configuration.GetConnectionString("MySql");
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddDbContext<TradingDbContext>(options =>
                options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
        }
        else
        {
            var inMemoryDatabaseName = configuration["Database:InMemoryName"] ?? "desafio_compra_programada";
            services.AddDbContext<TradingDbContext>(options =>
                options.UseInMemoryDatabase(inMemoryDatabaseName));
        }

        services.AddScoped<IClientService, TradingPlatformService>();
        services.AddScoped<IAdminService, TradingPlatformService>();
        services.AddScoped<IEngineService, TradingPlatformService>();

        services.AddSingleton<IQuoteProvider, CotahistQuoteProvider>();
        services.AddSingleton<IFiscalEventPublisher, KafkaFiscalEventPublisher>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddHostedService<PurchaseEngineScheduler>();

        return services;
    }
}
