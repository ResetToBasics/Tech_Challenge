namespace Desafio.CompraProgramada.Domain.Entities;

public class RebalanceExecution
{
    private RebalanceExecution()
    {
    }

    public RebalanceExecution(DateOnly dataReferencia, DateTime dataExecucaoUtc, string tipo, int clientesImpactados)
    {
        DataReferencia = dataReferencia;
        DataExecucaoUtc = dataExecucaoUtc;
        Tipo = tipo;
        ClientesImpactados = clientesImpactados;
    }

    public int Id { get; private set; }
    public DateOnly DataReferencia { get; private set; }
    public DateTime DataExecucaoUtc { get; private set; }
    public string Tipo { get; private set; } = string.Empty;
    public int ClientesImpactados { get; private set; }
    public decimal ValorTotalVendas { get; private set; }
    public decimal ValorTotalCompras { get; private set; }
    public decimal ValorTotalIrVenda { get; private set; }

    public void Acumular(decimal valorVendas, decimal valorCompras, decimal valorIrVenda)
    {
        ValorTotalVendas += valorVendas;
        ValorTotalCompras += valorCompras;
        ValorTotalIrVenda += valorIrVenda;
    }
}
