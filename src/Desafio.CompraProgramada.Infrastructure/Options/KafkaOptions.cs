namespace Desafio.CompraProgramada.Infrastructure.Options;

public class KafkaOptions
{
    public bool Habilitado { get; set; } = true;
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string TopicoFiscal { get; set; } = "itau.fiscal";
}
