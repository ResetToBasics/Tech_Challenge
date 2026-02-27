namespace Desafio.CompraProgramada.Infrastructure.Options;

public class EngineOptions
{
    public decimal LimiarDesvioPontosPercentuais { get; set; } = 5m;
    public bool HabilitarAgendamentoCompra { get; set; } = true;
    public int IntervaloMinutosAgendamento { get; set; } = 60;
}
