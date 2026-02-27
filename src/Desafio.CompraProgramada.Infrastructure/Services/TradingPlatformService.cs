using System.Text.Json;
using Desafio.CompraProgramada.Application.Abstractions;
using Desafio.CompraProgramada.Application.DTOs;
using Desafio.CompraProgramada.Application.Exceptions;
using Desafio.CompraProgramada.Application.Interfaces;
using Desafio.CompraProgramada.Domain.Constants;
using Desafio.CompraProgramada.Domain.Entities;
using Desafio.CompraProgramada.Domain.Enums;
using Desafio.CompraProgramada.Infrastructure.Data;
using Desafio.CompraProgramada.Infrastructure.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Desafio.CompraProgramada.Infrastructure.Services;

public class TradingPlatformService : IClientService, IAdminService, IEngineService
{
    private readonly TradingDbContext _dbContext;
    private readonly IQuoteProvider _quoteProvider;
    private readonly IFiscalEventPublisher _fiscalEventPublisher;
    private readonly IClock _clock;
    private readonly EngineOptions _engineOptions;
    private readonly ILogger<TradingPlatformService> _logger;
    private readonly JsonSerializerOptions _serializerOptions;

    public TradingPlatformService(
        TradingDbContext dbContext,
        IQuoteProvider quoteProvider,
        IFiscalEventPublisher fiscalEventPublisher,
        IClock clock,
        IOptions<EngineOptions> engineOptions,
        ILogger<TradingPlatformService> logger)
    {
        _dbContext = dbContext;
        _quoteProvider = quoteProvider;
        _fiscalEventPublisher = fiscalEventPublisher;
        _clock = clock;
        _engineOptions = engineOptions.Value;
        _logger = logger;

        _serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public async Task<AdesaoClienteResponse> AderirAsync(AdesaoClienteRequest request, CancellationToken cancellationToken = default)
    {
        var cpfNormalizado = NormalizarCpf(request.Cpf);

        var cpfJaExiste = await _dbContext.Clientes.AnyAsync(item => item.Cpf == cpfNormalizado, cancellationToken);
        if (cpfJaExiste)
        {
            throw new BusinessException("CLIENTE_CPF_DUPLICADO", "CPF ja cadastrado no sistema.");
        }

        var proximoNumeroConta = (await _dbContext.Clientes.MaxAsync(item => (int?)item.Id, cancellationToken) ?? 0) + 1;
        var numeroConta = $"FLH-{proximoNumeroConta:D6}";

        Client cliente;
        try
        {
            cliente = Client.Criar(request.Nome, cpfNormalizado, request.Email, request.ValorMensal, _clock.UtcNow, numeroConta);
        }
        catch (InvalidOperationException exception) when (exception.Message.Contains("valor mensal minimo", StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessException("VALOR_MENSAL_INVALIDO", "O valor mensal minimo e de R$ 100,00.");
        }

        _dbContext.Clientes.Add(cliente);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AdesaoClienteResponse(
            cliente.Id,
            cliente.Nome,
            cliente.Cpf,
            cliente.Email,
            cliente.ValorMensal,
            cliente.Ativo,
            cliente.DataAdesaoUtc,
            new ContaGraficaResponse(cliente.Id, cliente.NumeroContaGrafica, "FILHOTE", cliente.DataAdesaoUtc));
    }

    public async Task<SaidaClienteResponse> SairAsync(int clienteId, CancellationToken cancellationToken = default)
    {
        var cliente = await _dbContext.Clientes.FirstOrDefaultAsync(item => item.Id == clienteId, cancellationToken);
        if (cliente is null)
        {
            throw new BusinessException("CLIENTE_NAO_ENCONTRADO", "Cliente nao encontrado.", 404);
        }

        if (!cliente.Ativo)
        {
            throw new BusinessException("CLIENTE_JA_INATIVO", "Cliente ja havia saido do produto.");
        }

        cliente.EncerrarAdesao(_clock.UtcNow);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new SaidaClienteResponse(
            cliente.Id,
            cliente.Nome,
            cliente.Ativo,
            cliente.DataSaidaUtc ?? _clock.UtcNow,
            "Adesao encerrada. Sua posicao em custodia foi mantida.");
    }

    public async Task<AlteracaoValorMensalResponse> AlterarValorMensalAsync(int clienteId, decimal novoValorMensal, CancellationToken cancellationToken = default)
    {
        var cliente = await _dbContext.Clientes.FirstOrDefaultAsync(item => item.Id == clienteId, cancellationToken);
        if (cliente is null)
        {
            throw new BusinessException("CLIENTE_NAO_ENCONTRADO", "Cliente nao encontrado.", 404);
        }

        var valorAnterior = cliente.ValorMensal;

        try
        {
            cliente.AlterarValorMensal(novoValorMensal, _clock.UtcNow);
        }
        catch (InvalidOperationException exception) when (exception.Message.Contains("valor mensal minimo", StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessException("VALOR_MENSAL_INVALIDO", "O valor mensal minimo e de R$ 100,00.");
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AlteracaoValorMensalResponse(
            cliente.Id,
            valorAnterior,
            cliente.ValorMensal,
            _clock.UtcNow,
            "Valor mensal atualizado. O novo valor sera considerado a partir da proxima data de compra.");
    }

    public async Task<CarteiraClienteResponse> ObterCarteiraAsync(int clienteId, CancellationToken cancellationToken = default)
    {
        var cliente = await ObterClienteDetalhadoOuErroAsync(clienteId, cancellationToken);

        var dataReferencia = DateOnly.FromDateTime(_clock.UtcNow);

        var holdings = cliente.Custodia
            .Where(item => item.Quantidade > 0)
            .OrderBy(item => item.Ticker)
            .ToList();

        var cotacoes = holdings.Count == 0
            ? new Dictionary<string, decimal>()
            : await ObterCotacoesOuErroAsync(holdings.Select(item => item.Ticker), dataReferencia, cancellationToken);

        var valorTotalInvestido = ArredondarMoeda(cliente.HistoricoAportes.Sum(item => item.Valor));

        var ativos = holdings
            .Select(item =>
            {
                var cotacaoAtual = cotacoes[item.Ticker];
                var valorAtual = item.Quantidade * cotacaoAtual;
                var pl = (cotacaoAtual - item.PrecoMedio) * item.Quantidade;
                var plPercentual = item.PrecoMedio <= 0
                    ? 0
                    : ((cotacaoAtual - item.PrecoMedio) / item.PrecoMedio) * 100;

                return new
                {
                    item,
                    cotacaoAtual,
                    valorAtual,
                    pl,
                    plPercentual
                };
            })
            .ToList();

        var valorAtualCarteira = ArredondarMoeda(ativos.Sum(item => item.valorAtual));
        var plTotal = ArredondarMoeda(ativos.Sum(item => item.pl));
        var rentabilidadePercentual = valorTotalInvestido <= 0
            ? 0
            : ArredondarPercentual(((valorAtualCarteira - valorTotalInvestido) / valorTotalInvestido) * 100);

        var ativosResponse = ativos
            .Select(item => new AtivoCarteiraResponse(
                item.item.Ticker,
                item.item.Quantidade,
                ArredondarPreco(item.item.PrecoMedio),
                ArredondarPreco(item.cotacaoAtual),
                ArredondarMoeda(item.valorAtual),
                ArredondarMoeda(item.pl),
                ArredondarPercentual(item.plPercentual),
                valorAtualCarteira <= 0 ? 0 : ArredondarPercentual((item.valorAtual / valorAtualCarteira) * 100)))
            .ToList();

        var resumo = new ResumoCarteiraResponse(
            valorTotalInvestido,
            valorAtualCarteira,
            plTotal,
            rentabilidadePercentual);

        return new CarteiraClienteResponse(
            cliente.Id,
            cliente.Nome,
            cliente.NumeroContaGrafica,
            _clock.UtcNow,
            resumo,
            ativosResponse);
    }

    public async Task<RentabilidadeClienteResponse> ObterRentabilidadeAsync(int clienteId, CancellationToken cancellationToken = default)
    {
        var cliente = await ObterClienteDetalhadoOuErroAsync(clienteId, cancellationToken);
        var carteira = await ObterCarteiraAsync(clienteId, cancellationToken);

        var historicoAportes = cliente.HistoricoAportes
            .OrderBy(item => item.DataReferencia)
            .Select(item => new HistoricoAporteResponse(item.DataReferencia, ArredondarMoeda(item.Valor), item.Parcela))
            .ToList();

        var evolucao = cliente.HistoricoEvolucao
            .OrderBy(item => item.DataReferencia)
            .Select(item => new EvolucaoCarteiraResponse(
                item.DataReferencia,
                ArredondarMoeda(item.ValorCarteira),
                ArredondarMoeda(item.ValorInvestido),
                ArredondarPercentual(item.Rentabilidade)))
            .ToList();

        var rentabilidade = new RentabilidadeResumoResponse(
            carteira.Resumo.ValorTotalInvestido,
            carteira.Resumo.ValorAtualCarteira,
            carteira.Resumo.PlTotal,
            carteira.Resumo.RentabilidadePercentual);

        return new RentabilidadeClienteResponse(
            cliente.Id,
            cliente.Nome,
            _clock.UtcNow,
            rentabilidade,
            historicoAportes,
            evolucao);
    }

    public async Task<CestaAdminResponse> CriarOuAtualizarCestaAsync(CestaAdminRequest request, CancellationToken cancellationToken = default)
    {
        RecommendationBasket novaCesta;

        try
        {
            novaCesta = RecommendationBasket.Criar(
                request.Nome,
                request.Itens.Select(item => (item.Ticker, item.Percentual)),
                _clock.UtcNow);
        }
        catch (InvalidOperationException exception)
        {
            if (exception.Message.Contains("exatamente 5 ativos", StringComparison.OrdinalIgnoreCase))
            {
                throw new BusinessException("QUANTIDADE_ATIVOS_INVALIDA", exception.Message);
            }

            if (exception.Message.Contains("soma dos percentuais", StringComparison.OrdinalIgnoreCase))
            {
                throw new BusinessException("PERCENTUAIS_INVALIDOS", exception.Message);
            }

            throw;
        }

        var cestaAtual = await _dbContext.Cestas
            .Include(item => item.Itens)
            .FirstOrDefaultAsync(item => item.Ativa, cancellationToken);

        CestaAnteriorResponse? cestaAnteriorResponse = null;
        var rebalanceamentoDisparado = false;
        var clientesImpactados = 0;
        decimal valorTotalVendas = 0;
        decimal valorTotalCompras = 0;
        decimal valorTotalIrVenda = 0;

        IReadOnlyList<string> ativosRemovidos = [];
        IReadOnlyList<string> ativosAdicionados = [];

        if (cestaAtual is not null)
        {
            ativosRemovidos = cestaAtual.Itens
                .Select(item => item.Ticker)
                .Except(novaCesta.Itens.Select(item => item.Ticker))
                .OrderBy(item => item)
                .ToList();

            ativosAdicionados = novaCesta.Itens
                .Select(item => item.Ticker)
                .Except(cestaAtual.Itens.Select(item => item.Ticker))
                .OrderBy(item => item)
                .ToList();

            cestaAtual.Desativar(_clock.UtcNow);
            cestaAnteriorResponse = new CestaAnteriorResponse(cestaAtual.Id, cestaAtual.Nome, cestaAtual.DataDesativacaoUtc ?? _clock.UtcNow);
            rebalanceamentoDisparado = true;
        }

        _dbContext.Cestas.Add(novaCesta);
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (cestaAtual is not null)
        {
            var resultadoRebalanceamento = await RebalancearMudancaCestaInternoAsync(novaCesta, DateOnly.FromDateTime(_clock.UtcNow), cancellationToken);
            clientesImpactados = resultadoRebalanceamento.ClientesProcessados;
            valorTotalVendas = resultadoRebalanceamento.ValorTotalVendas;
            valorTotalCompras = resultadoRebalanceamento.ValorTotalCompras;
            valorTotalIrVenda = resultadoRebalanceamento.ValorTotalIrVenda;

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Rebalanceamento por mudanca de cesta executado. Clientes: {Clientes}. Vendas: {Vendas}. Compras: {Compras}. IR venda: {IrVenda}",
                clientesImpactados,
                valorTotalVendas,
                valorTotalCompras,
                valorTotalIrVenda);
        }

        var mensagem = !rebalanceamentoDisparado
            ? "Primeira cesta cadastrada com sucesso."
            : $"Cesta atualizada. Rebalanceamento disparado para {clientesImpactados} clientes ativos.";

        return new CestaAdminResponse(
            novaCesta.Id,
            novaCesta.Nome,
            novaCesta.Ativa,
            novaCesta.DataCriacaoUtc,
            novaCesta.Itens.Select(item => new CestaItemResponse(item.Ticker, ArredondarPercentual(item.Percentual))).ToList(),
            rebalanceamentoDisparado,
            cestaAnteriorResponse,
            ativosRemovidos,
            ativosAdicionados,
            mensagem);
    }

    public async Task<CestaAtualResponse> ObterCestaAtualAsync(CancellationToken cancellationToken = default)
    {
        var cesta = await ObterCestaAtivaOuErroAsync(cancellationToken);
        var cotacoes = await ObterCotacoesOuErroAsync(
            cesta.Itens.Select(item => item.Ticker),
            DateOnly.FromDateTime(_clock.UtcNow),
            cancellationToken);

        var itens = cesta.Itens
            .OrderBy(item => item.Ticker)
            .Select(item => new CestaItemCotacaoResponse(item.Ticker, ArredondarPercentual(item.Percentual), ArredondarPreco(cotacoes[item.Ticker])))
            .ToList();

        return new CestaAtualResponse(
            cesta.Id,
            cesta.Nome,
            cesta.Ativa,
            cesta.DataCriacaoUtc,
            itens);
    }

    public async Task<HistoricoCestasResponse> ObterHistoricoCestasAsync(CancellationToken cancellationToken = default)
    {
        var cestas = await _dbContext.Cestas
            .AsNoTracking()
            .Include(item => item.Itens)
            .OrderByDescending(item => item.DataCriacaoUtc)
            .ToListAsync(cancellationToken);

        var response = cestas
            .Select(item => new CestaHistoricoItemResponse(
                item.Id,
                item.Nome,
                item.Ativa,
                item.DataCriacaoUtc,
                item.DataDesativacaoUtc,
                item.Itens
                    .OrderBy(cestaItem => cestaItem.Ticker)
                    .Select(cestaItem => new CestaItemResponse(cestaItem.Ticker, ArredondarPercentual(cestaItem.Percentual)))
                    .ToList()))
            .ToList();

        return new HistoricoCestasResponse(response);
    }

    public async Task<CustodiaMasterResponse> ObterCustodiaMasterAsync(CancellationToken cancellationToken = default)
    {
        var custodiaMaster = await _dbContext.CustodiaMaster
            .AsNoTracking()
            .Where(item => item.Quantidade > 0)
            .OrderBy(item => item.Ticker)
            .ToListAsync(cancellationToken);

        var cotacoes = custodiaMaster.Count == 0
            ? new Dictionary<string, decimal>()
            : await ObterCotacoesOuErroAsync(custodiaMaster.Select(item => item.Ticker), DateOnly.FromDateTime(_clock.UtcNow), cancellationToken);

        var itens = custodiaMaster
            .Select(item =>
            {
                var cotacao = cotacoes.TryGetValue(item.Ticker, out var valorCotacao)
                    ? valorCotacao
                    : item.PrecoMedio;
                var valorAtual = item.Quantidade * cotacao;

                return new CustodiaMasterItemResponse(
                    item.Ticker,
                    item.Quantidade,
                    ArredondarPreco(item.PrecoMedio),
                    ArredondarMoeda(valorAtual),
                    item.Origem);
            })
            .ToList();

        var valorTotal = ArredondarMoeda(itens.Sum(item => item.ValorAtual));

        return new CustodiaMasterResponse(
            new ContaMasterResponse(1, "MST-000001", "MASTER"),
            itens,
            valorTotal);
    }

    public async Task<ExecucaoCompraResponse> ExecutarCompraAsync(DateOnly dataReferencia, CancellationToken cancellationToken = default)
    {
        if (!ExecutionDateCalculator.DataEhValidaParaExecucao(dataReferencia))
        {
            throw new BusinessException(
                "DATA_EXECUCAO_INVALIDA",
                "A compra programada so pode ser executada na data util correspondente aos dias 5, 15 e 25.");
        }

        var compraJaExecutada = await _dbContext.ExecucoesCompra.AnyAsync(item => item.DataReferencia == dataReferencia, cancellationToken);
        if (compraJaExecutada)
        {
            throw new BusinessException("COMPRA_JA_EXECUTADA", "Compra ja foi executada para esta data.", 409);
        }

        var cestaAtiva = await ObterCestaAtivaOuErroAsync(cancellationToken);

        var clientesAtivos = await _dbContext.Clientes
            .Include(item => item.Custodia)
            .Include(item => item.HistoricoAportes)
            .Include(item => item.HistoricoEvolucao)
            .Where(item => item.Ativo)
            .OrderBy(item => item.Id)
            .ToListAsync(cancellationToken);

        if (clientesAtivos.Count == 0)
        {
            return new ExecucaoCompraResponse(
                _clock.UtcNow,
                0,
                0,
                [],
                [],
                [],
                0,
                "Nao ha clientes ativos para processar a compra programada.");
        }

        var aportesCliente = clientesAtivos.ToDictionary(
            item => item.Id,
            item => item.ValorMensal / 3m);

        var totalConsolidado = aportesCliente.Sum(item => item.Value);

        var cotacoes = await ObterCotacoesOuErroAsync(
            cestaAtiva.Itens.Select(item => item.Ticker),
            dataReferencia,
            cancellationToken);

        var execucao = new PurchaseExecution(
            dataReferencia,
            _clock.UtcNow,
            clientesAtivos.Count,
            ArredondarMoeda(totalConsolidado));

        _dbContext.ExecucoesCompra.Add(execucao);

        var holdingsMaster = await _dbContext.CustodiaMaster
            .Where(item => cestaAtiva.Itens.Select(cestaItem => cestaItem.Ticker).Contains(item.Ticker))
            .ToDictionaryAsync(item => item.Ticker, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var disponivelParaDistribuicao = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var itemCesta in cestaAtiva.Itens.OrderBy(item => item.Ticker))
        {
            var ticker = itemCesta.Ticker;
            var cotacao = cotacoes[ticker];
            var valorParaAtivo = totalConsolidado * (itemCesta.Percentual / 100m);
            var quantidadeCalculada = TruncarInteiro(valorParaAtivo / cotacao);

            if (!holdingsMaster.TryGetValue(ticker, out var holdingMaster))
            {
                holdingMaster = new MasterHolding(ticker);
                holdingsMaster[ticker] = holdingMaster;
                _dbContext.CustodiaMaster.Add(holdingMaster);
            }

            var saldoMasterAnterior = holdingMaster.Quantidade;
            var quantidadeComprar = Math.Max(0, quantidadeCalculada - saldoMasterAnterior);

            if (quantidadeComprar > 0)
            {
                var quantidadeLotePadrao = (quantidadeComprar / 100) * 100;
                var quantidadeFracionaria = quantidadeComprar % 100;

                var ordem = new MasterOrder(ticker, quantidadeComprar, quantidadeLotePadrao, quantidadeFracionaria, cotacao);
                execucao.AdicionarOrdem(ordem);

                holdingMaster.Adicionar(
                    quantidadeComprar,
                    cotacao,
                    $"Compra consolidada {dataReferencia:yyyy-MM-dd}",
                    _clock.UtcNow);
            }

            disponivelParaDistribuicao[ticker] = saldoMasterAnterior + quantidadeComprar;
        }

        var distribuidoPorTicker = cestaAtiva.Itens.ToDictionary(item => item.Ticker, _ => 0, StringComparer.OrdinalIgnoreCase);
        var totalEventosIr = 0;
        var parcela = ExecutionDateCalculator.ObterParcela(dataReferencia);

        foreach (var cliente in clientesAtivos)
        {
            var valorAporte = aportesCliente[cliente.Id];
            var proporcao = totalConsolidado <= 0 ? 0 : valorAporte / totalConsolidado;

            var distribuicao = new ClientDistribution(cliente.Id, cliente.Nome, ArredondarMoeda(valorAporte));

            foreach (var itemCesta in cestaAtiva.Itens.OrderBy(item => item.Ticker))
            {
                var ticker = itemCesta.Ticker;
                var quantidadeDisponivel = disponivelParaDistribuicao[ticker];
                var quantidadeCliente = TruncarInteiro(quantidadeDisponivel * proporcao);

                if (quantidadeCliente <= 0)
                {
                    continue;
                }

                var cotacao = cotacoes[ticker];
                var holdingCliente = cliente.ObterOuCriarHolding(ticker);
                holdingCliente.AdicionarCompra(quantidadeCliente, cotacao);

                distribuicao.AdicionarAtivo(ticker, quantidadeCliente);
                distribuidoPorTicker[ticker] += quantidadeCliente;

                await PublicarIrDedoDuroAsync(cliente, ticker, quantidadeCliente, cotacao, cancellationToken);
                totalEventosIr++;
            }

            cliente.RegistrarAporte(dataReferencia, ArredondarMoeda(valorAporte), parcela);
            execucao.AdicionarDistribuicao(distribuicao);
        }

        foreach (var itemCesta in cestaAtiva.Itens.OrderBy(item => item.Ticker))
        {
            var ticker = itemCesta.Ticker;
            var quantidadeDistribuida = distribuidoPorTicker[ticker];
            var holdingMaster = holdingsMaster[ticker];

            if (quantidadeDistribuida > 0)
            {
                holdingMaster.Remover(
                    quantidadeDistribuida,
                    $"Distribuicao {dataReferencia:yyyy-MM-dd}",
                    _clock.UtcNow);
            }
        }

        execucao.AtualizarEventosIrPublicados(totalEventosIr);

        await AtualizarSnapshotsClientesAsync(clientesAtivos, dataReferencia, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var ordensResponse = execucao.OrdensCompra
            .OrderBy(item => item.Ticker)
            .Select(item =>
            {
                var detalhes = new List<OrdemDetalheResponse>();

                if (item.QuantidadeLotePadrao > 0)
                {
                    detalhes.Add(new OrdemDetalheResponse("LOTE_PADRAO", item.Ticker, item.QuantidadeLotePadrao));
                }

                if (item.QuantidadeFracionaria > 0)
                {
                    detalhes.Add(new OrdemDetalheResponse("FRACIONARIO", $"{item.Ticker}F", item.QuantidadeFracionaria));
                }

                return new OrdemCompraResponse(
                    item.Ticker,
                    item.QuantidadeTotal,
                    detalhes,
                    ArredondarPreco(item.PrecoUnitario),
                    ArredondarMoeda(item.ValorTotal));
            })
            .ToList();

        var distribuicoesResponse = execucao.Distribuicoes
            .OrderBy(item => item.ClientId)
            .Select(item => new DistribuicaoClienteResponse(
                item.ClientId,
                item.NomeCliente,
                ArredondarMoeda(item.ValorAporte),
                item.Ativos
                    .OrderBy(ativo => ativo.Ticker)
                    .Select(ativo => new DistribuicaoAtivoResponse(ativo.Ticker, ativo.Quantidade))
                    .ToList()))
            .ToList();

        var residuosResponse = holdingsMaster.Values
            .Where(item => item.Quantidade > 0)
            .OrderBy(item => item.Ticker)
            .Select(item => new ResiduoMasterResponse(item.Ticker, item.Quantidade))
            .ToList();

        return new ExecucaoCompraResponse(
            execucao.DataExecucaoUtc,
            execucao.TotalClientes,
            ArredondarMoeda(execucao.TotalConsolidado),
            ordensResponse,
            distribuicoesResponse,
            residuosResponse,
            execucao.EventosIrPublicados,
            $"Compra programada executada com sucesso para {execucao.TotalClientes} clientes.");
    }

    public async Task<ExecucaoRebalanceamentoResponse> RebalancearPorDesvioAsync(
        DateOnly dataReferencia,
        decimal limiarDesvioPontosPercentuais,
        CancellationToken cancellationToken = default)
    {
        var cestaAtiva = await ObterCestaAtivaOuErroAsync(cancellationToken);

        var clientesAtivos = await _dbContext.Clientes
            .Include(item => item.Custodia)
            .Include(item => item.Vendas)
            .Include(item => item.HistoricoAportes)
            .Include(item => item.HistoricoEvolucao)
            .Where(item => item.Ativo)
            .ToListAsync(cancellationToken);

        var limiar = limiarDesvioPontosPercentuais <= 0
            ? _engineOptions.LimiarDesvioPontosPercentuais
            : limiarDesvioPontosPercentuais;

        var resultado = new RebalanceResult();

        foreach (var cliente in clientesAtivos)
        {
            var rebalanceamentoCliente = await RebalancearClienteParaCestaAsync(
                cliente,
                cestaAtiva,
                dataReferencia,
                RebalanceTriggerType.DesvioProporcao,
                limiar,
                aplicarFiltroDesvio: true,
                cancellationToken);

            resultado.Acumular(rebalanceamentoCliente);
        }

        if (resultado.ClientesProcessados > 0)
        {
            var execucao = new RebalanceExecution(dataReferencia, _clock.UtcNow, "DESVIO_PROPORCAO", resultado.ClientesProcessados);
            execucao.Acumular(resultado.ValorTotalVendas, resultado.ValorTotalCompras, resultado.ValorTotalIrVenda);

            _dbContext.ExecucoesRebalanceamento.Add(execucao);
            await AtualizarSnapshotsClientesAsync(clientesAtivos, dataReferencia, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return new ExecucaoRebalanceamentoResponse(
            _clock.UtcNow,
            "DESVIO_PROPORCAO",
            resultado.ClientesProcessados,
            ArredondarMoeda(resultado.ValorTotalVendas),
            ArredondarMoeda(resultado.ValorTotalCompras),
            ArredondarMoeda(resultado.ValorTotalIrVenda),
            resultado.ClientesProcessados == 0
                ? "Nenhum cliente excedeu o limiar de desvio para rebalanceamento."
                : "Rebalanceamento por desvio executado com sucesso.");
    }

    private async Task<RebalanceResult> RebalancearMudancaCestaInternoAsync(
        RecommendationBasket cestaNova,
        DateOnly dataReferencia,
        CancellationToken cancellationToken)
    {
        var clientesAtivos = await _dbContext.Clientes
            .Include(item => item.Custodia)
            .Include(item => item.Vendas)
            .Include(item => item.HistoricoAportes)
            .Include(item => item.HistoricoEvolucao)
            .Where(item => item.Ativo)
            .ToListAsync(cancellationToken);

        var resultado = new RebalanceResult();

        foreach (var cliente in clientesAtivos)
        {
            var rebalanceamentoCliente = await RebalancearClienteParaCestaAsync(
                cliente,
                cestaNova,
                dataReferencia,
                RebalanceTriggerType.MudancaCesta,
                limiarDesvioPontosPercentuais: 0,
                aplicarFiltroDesvio: false,
                cancellationToken);

            resultado.Acumular(rebalanceamentoCliente);
        }

        if (resultado.ClientesProcessados > 0)
        {
            var execucao = new RebalanceExecution(dataReferencia, _clock.UtcNow, "MUDANCA_CESTA", resultado.ClientesProcessados);
            execucao.Acumular(resultado.ValorTotalVendas, resultado.ValorTotalCompras, resultado.ValorTotalIrVenda);
            _dbContext.ExecucoesRebalanceamento.Add(execucao);

            await AtualizarSnapshotsClientesAsync(clientesAtivos, dataReferencia, cancellationToken);
        }

        return resultado;
    }

    private async Task<RebalanceClientResult> RebalancearClienteParaCestaAsync(
        Client cliente,
        RecommendationBasket cestaAlvo,
        DateOnly dataReferencia,
        RebalanceTriggerType gatilho,
        decimal limiarDesvioPontosPercentuais,
        bool aplicarFiltroDesvio,
        CancellationToken cancellationToken)
    {
        var holdings = cliente.Custodia.Where(item => item.Quantidade > 0).ToList();
        if (holdings.Count == 0)
        {
            return RebalanceClientResult.SemAlteracao;
        }

        var tickersCarteira = holdings.Select(item => item.Ticker);
        var tickersCesta = cestaAlvo.Itens.Select(item => item.Ticker);

        var cotacoes = await ObterCotacoesOuErroAsync(
            tickersCarteira.Concat(tickersCesta).Distinct(StringComparer.OrdinalIgnoreCase),
            dataReferencia,
            cancellationToken);

        var valorTotalCarteira = holdings.Sum(item => item.Quantidade * cotacoes[item.Ticker]);
        if (valorTotalCarteira <= 0)
        {
            return RebalanceClientResult.SemAlteracao;
        }

        if (aplicarFiltroDesvio)
        {
            var existeDesvio = cestaAlvo.Itens.Any(item =>
            {
                var quantidadeAtual = holdings.FirstOrDefault(holding => holding.Ticker == item.Ticker)?.Quantidade ?? 0;
                var valorAtual = quantidadeAtual * cotacoes[item.Ticker];
                var percentualAtual = valorTotalCarteira <= 0 ? 0 : (valorAtual / valorTotalCarteira) * 100;
                var desvio = Math.Abs(percentualAtual - item.Percentual);
                return desvio > limiarDesvioPontosPercentuais;
            });

            if (!existeDesvio)
            {
                return RebalanceClientResult.SemAlteracao;
            }
        }

        var tickersCestaSet = cestaAlvo.Itens.Select(item => item.Ticker).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var caixaDisponivel = 0m;
        var totalVendas = 0m;
        var totalCompras = 0m;

        // 1) Vende ativos fora da cesta.
        foreach (var holding in holdings.Where(item => !tickersCestaSet.Contains(item.Ticker)).ToList())
        {
            var quantidadeVenda = holding.Quantidade;
            if (quantidadeVenda <= 0)
            {
                continue;
            }

            var precoVenda = cotacoes[holding.Ticker];
            RegistrarVenda(cliente, holding, quantidadeVenda, precoVenda, gatilho, ref caixaDisponivel, ref totalVendas);
        }

        // 2) Vende excesso dos ativos sobre-alocados.
        foreach (var itemCesta in cestaAlvo.Itens)
        {
            var holding = cliente.Custodia.FirstOrDefault(item => item.Ticker == itemCesta.Ticker);
            if (holding is null || holding.Quantidade <= 0)
            {
                continue;
            }

            var preco = cotacoes[itemCesta.Ticker];
            var valorAtual = holding.Quantidade * preco;
            var valorAlvo = valorTotalCarteira * (itemCesta.Percentual / 100m);

            if (valorAtual <= valorAlvo)
            {
                continue;
            }

            var quantidadeVenda = TruncarInteiro((valorAtual - valorAlvo) / preco);
            if (quantidadeVenda <= 0)
            {
                continue;
            }

            RegistrarVenda(cliente, holding, quantidadeVenda, preco, gatilho, ref caixaDisponivel, ref totalVendas);
        }

        // 3) Compra ativos sub-alocados com caixa das vendas.
        var deficits = new List<(string ticker, decimal deficit, decimal preco)>();
        foreach (var itemCesta in cestaAlvo.Itens)
        {
            var holding = cliente.Custodia.FirstOrDefault(item => item.Ticker == itemCesta.Ticker);
            var quantidadeAtual = holding?.Quantidade ?? 0;
            var preco = cotacoes[itemCesta.Ticker];

            var valorAtual = quantidadeAtual * preco;
            var valorAlvo = valorTotalCarteira * (itemCesta.Percentual / 100m);
            var deficit = valorAlvo - valorAtual;

            if (deficit > 0)
            {
                deficits.Add((itemCesta.Ticker, deficit, preco));
            }
        }

        var totalDeficit = deficits.Sum(item => item.deficit);
        if (caixaDisponivel > 0 && totalDeficit > 0)
        {
            foreach (var (ticker, deficit, preco) in deficits)
            {
                var alocacao = caixaDisponivel * (deficit / totalDeficit);
                var quantidadeCompra = TruncarInteiro(alocacao / preco);

                if (quantidadeCompra <= 0)
                {
                    continue;
                }

                var holding = cliente.ObterOuCriarHolding(ticker);
                holding.AdicionarCompra(quantidadeCompra, preco);

                var valorCompra = quantidadeCompra * preco;
                caixaDisponivel -= valorCompra;
                totalCompras += valorCompra;
            }
        }

        var totalIrVenda = 0m;
        if (totalVendas > 0)
        {
            totalIrVenda = await CalcularIrVendaMensalAsync(cliente, dataReferencia, cancellationToken);
        }

        var houveAlteracao = totalVendas > 0 || totalCompras > 0;

        return new RebalanceClientResult(
            houveAlteracao,
            totalVendas,
            totalCompras,
            totalIrVenda);
    }

    private void RegistrarVenda(
        Client cliente,
        ClientHolding holding,
        int quantidade,
        decimal precoVenda,
        RebalanceTriggerType gatilho,
        ref decimal caixaDisponivel,
        ref decimal totalVendas)
    {
        var venda = new SaleOperation(
            cliente.Id,
            holding.Ticker,
            quantidade,
            precoVenda,
            holding.PrecoMedio,
            _clock.UtcNow,
            gatilho);

        holding.Vender(quantidade);
        cliente.RegistrarVenda(venda);

        caixaDisponivel += venda.ValorTotal;
        totalVendas += venda.ValorTotal;
    }

    private async Task<decimal> CalcularIrVendaMensalAsync(Client cliente, DateOnly dataReferencia, CancellationToken cancellationToken)
    {
        var mesReferencia = dataReferencia.ToString("yyyy-MM");

        var vendasMes = cliente.Vendas
            .Where(item => item.MesReferencia == mesReferencia)
            .ToList();

        var totalVendasMes = vendasMes.Sum(item => item.ValorTotal);
        if (totalVendasMes <= TaxConstants.LimiteIsencaoVendaMensal)
        {
            return 0;
        }

        var lucroLiquido = vendasMes.Sum(item => item.Lucro);
        if (lucroLiquido <= 0)
        {
            return 0;
        }

        var valorIr = ArredondarMoeda(lucroLiquido * TaxConstants.AliquotaIrVenda);

        var detalhes = vendasMes
            .Select(item => new FiscalSaleDetail(
                item.Ticker,
                item.Quantidade,
                ArredondarPreco(item.PrecoVendaUnitario),
                ArredondarPreco(item.PrecoMedio),
                ArredondarMoeda(item.Lucro)))
            .ToList();

        var mensagem = new FiscalEventMessage(
            "IR_VENDA",
            cliente.Id,
            cliente.Cpf,
            null,
            "VENDA",
            0,
            0,
            ArredondarMoeda(totalVendasMes),
            TaxConstants.AliquotaIrVenda,
            valorIr,
            _clock.UtcNow,
            mesReferencia,
            ArredondarMoeda(totalVendasMes),
            ArredondarMoeda(lucroLiquido),
            detalhes);

        var payload = JsonSerializer.Serialize(mensagem, _serializerOptions);

        _dbContext.EventosFiscais.Add(new FiscalEventLog(
            "IR_VENDA",
            cliente.Id,
            cliente.Cpf,
            null,
            ArredondarMoeda(totalVendasMes),
            valorIr,
            _clock.UtcNow,
            payload,
            mesReferencia));

        try
        {
            await _fiscalEventPublisher.PublicarAsync(mensagem, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Erro ao publicar evento de IR venda no Kafka.");
            throw new BusinessException("KAFKA_INDISPONIVEL", "Erro ao publicar no topico Kafka.", 500);
        }

        return valorIr;
    }

    private async Task PublicarIrDedoDuroAsync(
        Client cliente,
        string ticker,
        int quantidade,
        decimal precoUnitario,
        CancellationToken cancellationToken)
    {
        var valorOperacao = quantidade * precoUnitario;
        var valorIr = ArredondarMoeda(valorOperacao * TaxConstants.AliquotaIrDedoDuro);

        var mensagem = new FiscalEventMessage(
            "IR_DEDO_DURO",
            cliente.Id,
            cliente.Cpf,
            ticker,
            "COMPRA",
            quantidade,
            ArredondarPreco(precoUnitario),
            ArredondarMoeda(valorOperacao),
            TaxConstants.AliquotaIrDedoDuro,
            valorIr,
            _clock.UtcNow);

        var payload = JsonSerializer.Serialize(mensagem, _serializerOptions);

        _dbContext.EventosFiscais.Add(new FiscalEventLog(
            "IR_DEDO_DURO",
            cliente.Id,
            cliente.Cpf,
            ticker,
            ArredondarMoeda(valorOperacao),
            valorIr,
            _clock.UtcNow,
            payload));

        try
        {
            await _fiscalEventPublisher.PublicarAsync(mensagem, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Erro ao publicar evento de IR dedo-duro no Kafka.");
            throw new BusinessException("KAFKA_INDISPONIVEL", "Erro ao publicar no topico Kafka.", 500);
        }
    }

    private async Task AtualizarSnapshotsClientesAsync(
        IReadOnlyCollection<Client> clientes,
        DateOnly dataReferencia,
        CancellationToken cancellationToken)
    {
        var holdings = clientes
            .SelectMany(item => item.Custodia)
            .Where(item => item.Quantidade > 0)
            .ToList();

        if (holdings.Count == 0)
        {
            return;
        }

        var cotacoes = await ObterCotacoesOuErroAsync(
            holdings.Select(item => item.Ticker).Distinct(StringComparer.OrdinalIgnoreCase),
            dataReferencia,
            cancellationToken);

        foreach (var cliente in clientes)
        {
            var valorCarteira = cliente.Custodia
                .Where(item => item.Quantidade > 0)
                .Sum(item => item.Quantidade * cotacoes[item.Ticker]);

            var valorInvestido = cliente.HistoricoAportes.Sum(item => item.Valor);

            var snapshotExistente = cliente.HistoricoEvolucao.FirstOrDefault(item => item.DataReferencia == dataReferencia);
            if (snapshotExistente is not null)
            {
                _dbContext.HistoricoEvolucao.Remove(snapshotExistente);
            }

            cliente.RegistrarSnapshot(dataReferencia, ArredondarMoeda(valorCarteira), ArredondarMoeda(valorInvestido));
        }
    }

    private async Task<Client> ObterClienteDetalhadoOuErroAsync(int clienteId, CancellationToken cancellationToken)
    {
        var cliente = await _dbContext.Clientes
            .Include(item => item.Custodia)
            .Include(item => item.HistoricoAportes)
            .Include(item => item.HistoricoEvolucao)
            .Include(item => item.Vendas)
            .FirstOrDefaultAsync(item => item.Id == clienteId, cancellationToken);

        if (cliente is null)
        {
            throw new BusinessException("CLIENTE_NAO_ENCONTRADO", "Cliente nao encontrado.", 404);
        }

        return cliente;
    }

    private async Task<RecommendationBasket> ObterCestaAtivaOuErroAsync(CancellationToken cancellationToken)
    {
        var cesta = await _dbContext.Cestas
            .Include(item => item.Itens)
            .FirstOrDefaultAsync(item => item.Ativa, cancellationToken);

        if (cesta is null)
        {
            throw new BusinessException("CESTA_NAO_ENCONTRADA", "Nenhuma cesta ativa encontrada.", 404);
        }

        return cesta;
    }

    private async Task<IDictionary<string, decimal>> ObterCotacoesOuErroAsync(
        IEnumerable<string> tickers,
        DateOnly dataReferencia,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _quoteProvider.ObterCotacoesFechamentoAsync(tickers, dataReferencia, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Erro ao obter cotacoes para data {DataReferencia}", dataReferencia);
            throw new BusinessException("COTACAO_NAO_ENCONTRADA", "Arquivo COTAHIST nao encontrado para a data solicitada.", 404);
        }
    }

    private static string NormalizarCpf(string cpf)
    {
        return new string(cpf.Where(char.IsDigit).ToArray());
    }

    private static int TruncarInteiro(decimal valor)
    {
        return (int)decimal.Truncate(valor);
    }

    private static decimal ArredondarMoeda(decimal valor)
    {
        return Math.Round(valor, 2, MidpointRounding.AwayFromZero);
    }

    private static decimal ArredondarPreco(decimal valor)
    {
        return Math.Round(valor, 6, MidpointRounding.AwayFromZero);
    }

    private static decimal ArredondarPercentual(decimal valor)
    {
        return Math.Round(valor, 4, MidpointRounding.AwayFromZero);
    }

    private sealed class RebalanceResult
    {
        public int ClientesProcessados { get; private set; }
        public decimal ValorTotalVendas { get; private set; }
        public decimal ValorTotalCompras { get; private set; }
        public decimal ValorTotalIrVenda { get; private set; }

        public void Acumular(RebalanceClientResult resultadoCliente)
        {
            if (!resultadoCliente.HouveAlteracao)
            {
                return;
            }

            ClientesProcessados++;
            ValorTotalVendas += resultadoCliente.ValorTotalVendas;
            ValorTotalCompras += resultadoCliente.ValorTotalCompras;
            ValorTotalIrVenda += resultadoCliente.ValorTotalIrVenda;
        }
    }

    private sealed record RebalanceClientResult(bool HouveAlteracao, decimal ValorTotalVendas, decimal ValorTotalCompras, decimal ValorTotalIrVenda)
    {
        public static readonly RebalanceClientResult SemAlteracao = new(false, 0, 0, 0);
    }
}
