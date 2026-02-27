using System.Text.Json;
using Confluent.Kafka;
using Desafio.CompraProgramada.Application.DTOs;
using Desafio.CompraProgramada.Application.Interfaces;
using Desafio.CompraProgramada.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Desafio.CompraProgramada.Infrastructure.Messaging;

public class KafkaFiscalEventPublisher : IFiscalEventPublisher, IDisposable
{
    private readonly KafkaOptions _options;
    private readonly ILogger<KafkaFiscalEventPublisher> _logger;
    private readonly JsonSerializerOptions _serializerOptions;
    private IProducer<Null, string>? _producer;

    public KafkaFiscalEventPublisher(IOptions<KafkaOptions> options, ILogger<KafkaFiscalEventPublisher> logger)
    {
        _options = options.Value;
        _logger = logger;
        _serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task PublicarAsync(FiscalEventMessage evento, CancellationToken cancellationToken = default)
    {
        if (!_options.Habilitado || string.IsNullOrWhiteSpace(_options.BootstrapServers) || string.IsNullOrWhiteSpace(_options.TopicoFiscal))
        {
            _logger.LogInformation(
                "Publicacao Kafka desabilitada. Evento fiscal mantido somente em banco. Tipo: {Tipo}, ClienteId: {ClienteId}",
                evento.Tipo,
                evento.ClienteId);
            return;
        }

        _producer ??= CriarProducer();

        var payload = JsonSerializer.Serialize(evento, _serializerOptions);

        await _producer.ProduceAsync(
            _options.TopicoFiscal,
            new Message<Null, string> { Value = payload },
            cancellationToken);
    }

    private IProducer<Null, string> CriarProducer()
    {
        var config = new ProducerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            Acks = Acks.All,
            MessageTimeoutMs = 5000
        };

        return new ProducerBuilder<Null, string>(config).Build();
    }

    public void Dispose()
    {
        _producer?.Dispose();
    }
}
