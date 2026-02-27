using Desafio.CompraProgramada.Domain.Constants;

namespace Desafio.CompraProgramada.Domain.Entities;

public class Client
{
    private Client()
    {
        Custodia = [];
        HistoricoValorMensal = [];
        HistoricoAportes = [];
        HistoricoEvolucao = [];
        Vendas = [];
    }

    private Client(string nome, string cpf, string email, decimal valorMensal, DateTime dataAdesaoUtc, string numeroContaGrafica)
    {
        Nome = nome.Trim();
        Cpf = cpf.Trim();
        Email = email.Trim();
        ValorMensal = valorMensal;
        Ativo = true;
        DataAdesaoUtc = dataAdesaoUtc;
        NumeroContaGrafica = numeroContaGrafica;
    }

    public int Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public string Cpf { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public decimal ValorMensal { get; private set; }
    public bool Ativo { get; private set; }
    public DateTime DataAdesaoUtc { get; private set; }
    public DateTime? DataSaidaUtc { get; private set; }
    public string NumeroContaGrafica { get; private set; } = string.Empty;

    public ICollection<ClientHolding> Custodia { get; private set; } = [];
    public ICollection<ContributionChange> HistoricoValorMensal { get; private set; } = [];
    public ICollection<ContributionExecution> HistoricoAportes { get; private set; } = [];
    public ICollection<PortfolioSnapshot> HistoricoEvolucao { get; private set; } = [];
    public ICollection<SaleOperation> Vendas { get; private set; } = [];

    public static Client Criar(string nome, string cpf, string email, decimal valorMensal, DateTime dataAdesaoUtc, string numeroContaGrafica)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new InvalidOperationException("Nome do cliente e obrigatorio.");
        }

        if (string.IsNullOrWhiteSpace(cpf))
        {
            throw new InvalidOperationException("CPF do cliente e obrigatorio.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidOperationException("Email do cliente e obrigatorio.");
        }

        if (valorMensal < DomainConstraints.MinimoValorMensal)
        {
            throw new InvalidOperationException($"O valor mensal minimo e de R$ {DomainConstraints.MinimoValorMensal:N2}.");
        }

        return new Client(nome, cpf, email, valorMensal, dataAdesaoUtc, numeroContaGrafica);
    }

    public void EncerrarAdesao(DateTime dataSaidaUtc)
    {
        if (!Ativo)
        {
            throw new InvalidOperationException("Cliente ja havia saido do produto.");
        }

        Ativo = false;
        DataSaidaUtc = dataSaidaUtc;
    }

    public void AlterarValorMensal(decimal novoValorMensal, DateTime dataAlteracaoUtc)
    {
        if (novoValorMensal < DomainConstraints.MinimoValorMensal)
        {
            throw new InvalidOperationException($"O valor mensal minimo e de R$ {DomainConstraints.MinimoValorMensal:N2}.");
        }

        var valorAnterior = ValorMensal;
        ValorMensal = novoValorMensal;

        HistoricoValorMensal.Add(new ContributionChange(Id, valorAnterior, novoValorMensal, dataAlteracaoUtc));
    }

    public ClientHolding ObterOuCriarHolding(string ticker)
    {
        var holding = Custodia.FirstOrDefault(item => item.Ticker == ticker);

        if (holding is not null)
        {
            return holding;
        }

        holding = new ClientHolding(Id, ticker);
        Custodia.Add(holding);

        return holding;
    }

    public void RegistrarAporte(DateOnly dataReferencia, decimal valor, string parcela)
    {
        HistoricoAportes.Add(new ContributionExecution(Id, dataReferencia, valor, parcela));
    }

    public void RegistrarSnapshot(DateOnly dataReferencia, decimal valorCarteira, decimal valorInvestido)
    {
        var rentabilidade = valorInvestido <= 0
            ? 0
            : ((valorCarteira - valorInvestido) / valorInvestido) * 100;

        HistoricoEvolucao.Add(new PortfolioSnapshot(Id, dataReferencia, valorCarteira, valorInvestido, rentabilidade));
    }

    public void RegistrarVenda(SaleOperation venda)
    {
        Vendas.Add(venda);
    }
}
