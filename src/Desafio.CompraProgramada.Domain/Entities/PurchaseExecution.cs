namespace Desafio.CompraProgramada.Domain.Entities;

public class PurchaseExecution
{
    private PurchaseExecution()
    {
        OrdensCompra = [];
        Distribuicoes = [];
    }

    public PurchaseExecution(DateOnly dataReferencia, DateTime dataExecucaoUtc, int totalClientes, decimal totalConsolidado)
    {
        DataReferencia = dataReferencia;
        DataExecucaoUtc = dataExecucaoUtc;
        TotalClientes = totalClientes;
        TotalConsolidado = totalConsolidado;
    }

    public int Id { get; private set; }
    public DateOnly DataReferencia { get; private set; }
    public DateTime DataExecucaoUtc { get; private set; }
    public int TotalClientes { get; private set; }
    public decimal TotalConsolidado { get; private set; }
    public int EventosIrPublicados { get; private set; }

    public ICollection<MasterOrder> OrdensCompra { get; private set; } = [];
    public ICollection<ClientDistribution> Distribuicoes { get; private set; } = [];

    public void AdicionarOrdem(MasterOrder ordem)
    {
        OrdensCompra.Add(ordem);
    }

    public void AdicionarDistribuicao(ClientDistribution distribuicao)
    {
        Distribuicoes.Add(distribuicao);
    }

    public void AtualizarEventosIrPublicados(int quantidade)
    {
        EventosIrPublicados = quantidade;
    }
}
