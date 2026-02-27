using Desafio.CompraProgramada.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Desafio.CompraProgramada.Infrastructure.Data;

public class TradingDbContext(DbContextOptions<TradingDbContext> options) : DbContext(options)
{
    public DbSet<Client> Clientes => Set<Client>();
    public DbSet<ClientHolding> CustodiasFilhote => Set<ClientHolding>();
    public DbSet<ContributionChange> HistoricoValorMensal => Set<ContributionChange>();
    public DbSet<ContributionExecution> HistoricoAportes => Set<ContributionExecution>();
    public DbSet<PortfolioSnapshot> HistoricoEvolucao => Set<PortfolioSnapshot>();
    public DbSet<SaleOperation> Vendas => Set<SaleOperation>();
    public DbSet<MasterHolding> CustodiaMaster => Set<MasterHolding>();
    public DbSet<RecommendationBasket> Cestas => Set<RecommendationBasket>();
    public DbSet<RecommendationBasketItem> CestaItens => Set<RecommendationBasketItem>();
    public DbSet<PurchaseExecution> ExecucoesCompra => Set<PurchaseExecution>();
    public DbSet<MasterOrder> OrdensCompra => Set<MasterOrder>();
    public DbSet<ClientDistribution> Distribuicoes => Set<ClientDistribution>();
    public DbSet<ClientDistributionItem> DistribuicaoItens => Set<ClientDistributionItem>();
    public DbSet<RebalanceExecution> ExecucoesRebalanceamento => Set<RebalanceExecution>();
    public DbSet<FiscalEventLog> EventosFiscais => Set<FiscalEventLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var dateOnlyConverter = new ValueConverter<DateOnly, DateTime>(
            value => value.ToDateTime(TimeOnly.MinValue),
            value => DateOnly.FromDateTime(value));

        modelBuilder.Entity<Client>(entity =>
        {
            entity.ToTable("clientes");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Nome).HasMaxLength(200).IsRequired();
            entity.Property(item => item.Cpf).HasMaxLength(11).IsRequired();
            entity.Property(item => item.Email).HasMaxLength(200).IsRequired();
            entity.Property(item => item.NumeroContaGrafica).HasMaxLength(20).IsRequired();
            entity.Property(item => item.ValorMensal).HasPrecision(18, 2);
            entity.HasIndex(item => item.Cpf).IsUnique();

            entity.HasMany(item => item.Custodia)
                .WithOne()
                .HasForeignKey(item => item.ClientId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(item => item.HistoricoValorMensal)
                .WithOne()
                .HasForeignKey(item => item.ClientId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(item => item.HistoricoAportes)
                .WithOne()
                .HasForeignKey(item => item.ClientId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(item => item.HistoricoEvolucao)
                .WithOne()
                .HasForeignKey(item => item.ClientId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(item => item.Vendas)
                .WithOne()
                .HasForeignKey(item => item.ClientId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ClientHolding>(entity =>
        {
            entity.ToTable("custodias_filhote");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Ticker).HasMaxLength(12).IsRequired();
            entity.Property(item => item.PrecoMedio).HasPrecision(18, 6);
            entity.HasIndex(item => new { item.ClientId, item.Ticker }).IsUnique();
        });

        modelBuilder.Entity<ContributionChange>(entity =>
        {
            entity.ToTable("historico_valor_mensal");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.ValorAnterior).HasPrecision(18, 2);
            entity.Property(item => item.ValorNovo).HasPrecision(18, 2);
        });

        modelBuilder.Entity<ContributionExecution>(entity =>
        {
            entity.ToTable("historico_aportes");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.DataReferencia).HasConversion(dateOnlyConverter);
            entity.Property(item => item.Valor).HasPrecision(18, 2);
            entity.Property(item => item.Parcela).HasMaxLength(10).IsRequired();
        });

        modelBuilder.Entity<PortfolioSnapshot>(entity =>
        {
            entity.ToTable("historico_evolucao");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.DataReferencia).HasConversion(dateOnlyConverter);
            entity.Property(item => item.ValorCarteira).HasPrecision(18, 2);
            entity.Property(item => item.ValorInvestido).HasPrecision(18, 2);
            entity.Property(item => item.Rentabilidade).HasPrecision(18, 4);
            entity.HasIndex(item => new { item.ClientId, item.DataReferencia });
        });

        modelBuilder.Entity<SaleOperation>(entity =>
        {
            entity.ToTable("vendas_rebalanceamento");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Ticker).HasMaxLength(12).IsRequired();
            entity.Property(item => item.PrecoVendaUnitario).HasPrecision(18, 6);
            entity.Property(item => item.PrecoMedio).HasPrecision(18, 6);
            entity.Property(item => item.ValorTotal).HasPrecision(18, 2);
            entity.Property(item => item.Lucro).HasPrecision(18, 2);
            entity.Property(item => item.MesReferencia).HasMaxLength(7).IsRequired();
        });

        modelBuilder.Entity<MasterHolding>(entity =>
        {
            entity.ToTable("custodia_master");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Ticker).HasMaxLength(12).IsRequired();
            entity.Property(item => item.PrecoMedio).HasPrecision(18, 6);
            entity.Property(item => item.Origem).HasMaxLength(300).IsRequired();
            entity.HasIndex(item => item.Ticker).IsUnique();
        });

        modelBuilder.Entity<RecommendationBasket>(entity =>
        {
            entity.ToTable("cestas_recomendacao");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Nome).HasMaxLength(200).IsRequired();
            entity.HasMany(item => item.Itens)
                .WithOne()
                .HasForeignKey(item => item.RecommendationBasketId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RecommendationBasketItem>(entity =>
        {
            entity.ToTable("cesta_itens");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Ticker).HasMaxLength(12).IsRequired();
            entity.Property(item => item.Percentual).HasPrecision(9, 4);
        });

        modelBuilder.Entity<PurchaseExecution>(entity =>
        {
            entity.ToTable("execucoes_compra");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.DataReferencia).HasConversion(dateOnlyConverter);
            entity.Property(item => item.TotalConsolidado).HasPrecision(18, 2);
            entity.HasIndex(item => item.DataReferencia).IsUnique();

            entity.HasMany(item => item.OrdensCompra)
                .WithOne()
                .HasForeignKey(item => item.PurchaseExecutionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(item => item.Distribuicoes)
                .WithOne()
                .HasForeignKey(item => item.PurchaseExecutionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MasterOrder>(entity =>
        {
            entity.ToTable("ordens_compra");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Ticker).HasMaxLength(12).IsRequired();
            entity.Property(item => item.PrecoUnitario).HasPrecision(18, 6);
            entity.Property(item => item.ValorTotal).HasPrecision(18, 2);
        });

        modelBuilder.Entity<ClientDistribution>(entity =>
        {
            entity.ToTable("distribuicoes");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.NomeCliente).HasMaxLength(200).IsRequired();
            entity.Property(item => item.ValorAporte).HasPrecision(18, 2);

            entity.HasMany(item => item.Ativos)
                .WithOne()
                .HasForeignKey(item => item.ClientDistributionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ClientDistributionItem>(entity =>
        {
            entity.ToTable("distribuicao_itens");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Ticker).HasMaxLength(12).IsRequired();
        });

        modelBuilder.Entity<RebalanceExecution>(entity =>
        {
            entity.ToTable("execucoes_rebalanceamento");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.DataReferencia).HasConversion(dateOnlyConverter);
            entity.Property(item => item.Tipo).HasMaxLength(60).IsRequired();
            entity.Property(item => item.ValorTotalVendas).HasPrecision(18, 2);
            entity.Property(item => item.ValorTotalCompras).HasPrecision(18, 2);
            entity.Property(item => item.ValorTotalIrVenda).HasPrecision(18, 2);
        });

        modelBuilder.Entity<FiscalEventLog>(entity =>
        {
            entity.ToTable("eventos_fiscais");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Tipo).HasMaxLength(60).IsRequired();
            entity.Property(item => item.Cpf).HasMaxLength(11).IsRequired();
            entity.Property(item => item.Ticker).HasMaxLength(12);
            entity.Property(item => item.MesReferencia).HasMaxLength(7);
            entity.Property(item => item.ValorOperacao).HasPrecision(18, 2);
            entity.Property(item => item.ValorIr).HasPrecision(18, 2);
            entity.Property(item => item.PayloadJson).HasColumnType("longtext");
        });

        base.OnModelCreating(modelBuilder);
    }
}
