using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Desafio.CompraProgramada.Application.DTOs;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Desafio.CompraProgramada.IntegrationTests;

public class ApiFlowIntegrationTests
{
    [Fact]
    public async Task DeveExecutarFluxoPrincipalViaApi()
    {
        using var test = CreateClient();
        var client = test.Client;

        var cestaRequest = CriarCestaPadrao("Top Five - API");

        var cestaResponse = await client.PostAsJsonAsync("/api/admin/cesta", cestaRequest);
        cestaResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var adesaoResponse = await client.PostAsJsonAsync(
            "/api/clientes/adesao",
            new AdesaoClienteRequest("Joao da Silva", "12345678901", "joao@email.com", 3000));

        adesaoResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var execucaoResponse = await client.PostAsJsonAsync(
            "/api/motor/executar-compra",
            new ExecucaoCompraRequest(new DateOnly(2026, 2, 5)));

        var execucaoBodyRaw = await execucaoResponse.Content.ReadAsStringAsync();
        execucaoResponse.StatusCode.Should().Be(HttpStatusCode.OK, execucaoBodyRaw);

        var execucaoBody = JsonSerializer.Deserialize<ExecucaoCompraResponse>(
            execucaoBodyRaw,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        execucaoBody.Should().NotBeNull();
        execucaoBody!.TotalClientes.Should().Be(1);
        execucaoBody.OrdensCompra.Should().HaveCount(5);

        var carteiraResponse = await client.GetAsync("/api/clientes/1/carteira");
        carteiraResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var carteira = await carteiraResponse.Content.ReadFromJsonAsync<CarteiraClienteResponse>();
        carteira.Should().NotBeNull();
        carteira!.Ativos.Should().NotBeEmpty();

        carteiraResponse.Headers.Should().Contain(header => header.Key.Equals("X-Request-Id", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task DeveRetornarErroDeValidacaoQuandoCestaInvalida()
    {
        using var test = CreateClient();
        using var client = test.Client;

        var cestaRequest = new CestaAdminRequest(
            "Cesta Invalida",
            [
                new CestaItemRequest("PETR4", 50),
                new CestaItemRequest("VALE3", 50),
                new CestaItemRequest("ITUB4", 10),
                new CestaItemRequest("BBDC4", 10),
                new CestaItemRequest("WEGE3", 10)
            ]);

        var response = await client.PostAsJsonAsync("/api/admin/cesta", cestaRequest);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        payload.Should().NotBeNull();
        payload!.Codigo.Should().Be("PERCENTUAIS_INVALIDOS");
    }

    [Fact]
    public async Task DevePermitirAlterarValorMensalESairDoProduto()
    {
        using var test = CreateClient();
        var client = test.Client;

        await client.PostAsJsonAsync("/api/admin/cesta", CriarCestaPadrao("Top Five - Gestao Cliente"));

        var adesaoResponse = await client.PostAsJsonAsync(
            "/api/clientes/adesao",
            new AdesaoClienteRequest("Maria Souza", "22345678901", "maria@email.com", 3000));

        adesaoResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var alteracaoResponse = await client.PutAsJsonAsync(
            "/api/clientes/1/valor-mensal",
            new { novoValorMensal = 6000m });

        alteracaoResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var saidaResponse = await client.PostAsync("/api/clientes/1/saida", null);
        saidaResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var rentabilidadeResponse = await client.GetAsync("/api/clientes/1/rentabilidade");
        rentabilidadeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeveConsultarEndpointsAdministrativosERebalanceamentoPorDesvio()
    {
        using var test = CreateClient();
        var client = test.Client;

        await client.PostAsJsonAsync("/api/admin/cesta", CriarCestaPadrao("Top Five - Completo"));
        await client.PostAsJsonAsync(
            "/api/clientes/adesao",
            new AdesaoClienteRequest("Pedro Santos", "32345678901", "pedro@email.com", 9000));
        await client.PostAsJsonAsync(
            "/api/motor/executar-compra",
            new ExecucaoCompraRequest(new DateOnly(2026, 2, 5)));

        var cestaAtual = await client.GetAsync("/api/admin/cesta/atual");
        cestaAtual.StatusCode.Should().Be(HttpStatusCode.OK);

        var historico = await client.GetAsync("/api/admin/cesta/historico");
        historico.StatusCode.Should().Be(HttpStatusCode.OK);

        var custodiaMaster = await client.GetAsync("/api/admin/conta-master/custodia");
        custodiaMaster.StatusCode.Should().Be(HttpStatusCode.OK);

        var rebalanceamento = await client.PostAsJsonAsync(
            "/api/motor/rebalancear-desvio",
            new
            {
                dataReferencia = new DateOnly(2026, 2, 25),
                limiarDesvioPontosPercentuais = 0.5m
            });

        rebalanceamento.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeveRetornarConflitoQuandoCompraJaFoiExecutadaNaMesmaData()
    {
        using var test = CreateClient();
        var client = test.Client;

        await client.PostAsJsonAsync("/api/admin/cesta", CriarCestaPadrao("Top Five - Conflito"));
        await client.PostAsJsonAsync(
            "/api/clientes/adesao",
            new AdesaoClienteRequest("Cliente Conflito", "42345678901", "conf@email.com", 3000));

        var primeiraExecucao = await client.PostAsJsonAsync(
            "/api/motor/executar-compra",
            new ExecucaoCompraRequest(new DateOnly(2026, 3, 5)));
        primeiraExecucao.StatusCode.Should().Be(HttpStatusCode.OK);

        var segundaExecucao = await client.PostAsJsonAsync(
            "/api/motor/executar-compra",
            new ExecucaoCompraRequest(new DateOnly(2026, 3, 5)));

        segundaExecucao.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task DeveRetornarNaoEncontradoParaClienteInexistente()
    {
        using var test = CreateClient();
        using var client = test.Client;

        var response = await client.GetAsync("/api/clientes/999/carteira");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        error.Should().NotBeNull();
        error!.Codigo.Should().Be("CLIENTE_NAO_ENCONTRADO");
    }

    [Fact]
    public async Task DeveDispararRebalanceamentoQuandoCestaMuda()
    {
        using var test = CreateClient();
        var client = test.Client;

        await client.PostAsJsonAsync("/api/admin/cesta", CriarCestaPadrao("Top Five - Inicial"));
        await client.PostAsJsonAsync(
            "/api/clientes/adesao",
            new AdesaoClienteRequest("Cliente Rebalanceamento", "52345678901", "reb@email.com", 9000));
        await client.PostAsJsonAsync(
            "/api/motor/executar-compra",
            new ExecucaoCompraRequest(new DateOnly(2026, 2, 5)));

        var novaCesta = new CestaAdminRequest(
            "Top Five - Nova Composicao",
            [
                new CestaItemRequest("PETR4", 25),
                new CestaItemRequest("VALE3", 20),
                new CestaItemRequest("ITUB4", 20),
                new CestaItemRequest("ABEV3", 20),
                new CestaItemRequest("RENT3", 15)
            ]);

        var response = await client.PostAsJsonAsync("/api/admin/cesta", novaCesta);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var payload = await response.Content.ReadFromJsonAsync<CestaAdminResponse>();
        payload.Should().NotBeNull();
        payload!.RebalanceamentoDisparado.Should().BeTrue();
        payload.AtivosRemovidos.Should().Contain(["BBDC4", "WEGE3"]);
        payload.AtivosAdicionados.Should().Contain(["ABEV3", "RENT3"]);
    }

    private static TestClientScope CreateClient()
    {
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("https_port", "");
            builder.ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:MySql"] = "",
                    ["Database:InMemoryName"] = $"desafio_compra_programada_{Guid.NewGuid():N}",
                    ["Kafka:Habilitado"] = "false",
                    ["Kafka:BootstrapServers"] = "",
                    ["Cotacoes:PastaCotacoes"] = ObterPastaCotacoes()
                });
            });
        });

        return new TestClientScope(factory, factory.CreateClient());
    }

    private static CestaAdminRequest CriarCestaPadrao(string nome)
    {
        return new CestaAdminRequest(
            nome,
            [
                new CestaItemRequest("PETR4", 30),
                new CestaItemRequest("VALE3", 25),
                new CestaItemRequest("ITUB4", 20),
                new CestaItemRequest("BBDC4", 15),
                new CestaItemRequest("WEGE3", 10)
            ]);
    }

    private static string ObterPastaCotacoes()
    {
        var diretorioAtual = new DirectoryInfo(AppContext.BaseDirectory);

        while (diretorioAtual is not null)
        {
            var candidate = Path.Combine(diretorioAtual.FullName, "cotacoes");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            diretorioAtual = diretorioAtual.Parent;
        }

        throw new DirectoryNotFoundException("Pasta cotacoes nao encontrada para os testes de integracao.");
    }

    private sealed class TestClientScope(WebApplicationFactory<Program> factory, HttpClient client) : IDisposable
    {
        public WebApplicationFactory<Program> Factory { get; } = factory;
        public HttpClient Client { get; } = client;

        public void Dispose()
        {
            Client.Dispose();
            Factory.Dispose();
        }
    }
}
