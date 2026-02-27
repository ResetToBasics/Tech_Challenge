# Sistema de Compra Programada de Acoes - Desafio Tecnico Itau

Implementacao completa do desafio `teste_itau_v2`, com foco em regras de negocio, API REST, MySQL, Kafka, parser COTAHIST, motor de compra programada, rebalanceamento e testes automatizados.

## Stack
- Backend: .NET 10 (C#)
- API: ASP.NET Core Web API + Swagger
- Persistencia: Entity Framework Core + MySQL (Pomelo)
- Mensageria: Kafka (Confluent client)
- Testes: xUnit + FluentAssertions
- Infra local: Docker Compose (MySQL + Zookeeper + Kafka)

## O que foi implementado

### Cliente (API REST)
- `POST /api/clientes/adesao`
- `POST /api/clientes/{clienteId}/saida`
- `PUT /api/clientes/{clienteId}/valor-mensal`
- `GET /api/clientes/{clienteId}/carteira`
- `GET /api/clientes/{clienteId}/rentabilidade`

### Admin (API REST)
- `POST /api/admin/cesta`
- `GET /api/admin/cesta/atual`
- `GET /api/admin/cesta/historico`
- `GET /api/admin/conta-master/custodia`

### Motor
- `POST /api/motor/executar-compra`
- `POST /api/motor/rebalancear-desvio`
- Execucao automatica via `BackgroundService` configuravel (scheduler)

### Regras de negocio cobertas
- Adesao com validacoes (CPF unico, valor minimo)
- Saida do produto mantendo custodia
- Alteracao de valor mensal com historico
- Cesta Top Five com validacoes (5 ativos, 100%)
- Compra programada em datas validas (5/15/25 com ajuste para dia util)
- Consolidacao por cesta e cotacao de fechamento (COTAHIST)
- Uso de saldo da custodia master antes de comprar
- Separacao lote padrao/fracionario
- Distribuicao proporcional para custodias filhotes
- Residuos mantidos na master para proximo ciclo
- Atualizacao de preco medio por compra
- Publicacao de IR dedo-duro (0,005%)
- Rebalanceamento por mudanca de cesta
- Rebalanceamento por desvio de proporcao (limiar configuravel)
- Regra fiscal de venda (isencao ate R$ 20.000 e 20% sobre lucro liquido acima do limite)

## Estrutura do projeto

```
.
|-- cotacoes/
|-- src/
|   |-- Desafio.CompraProgramada.Domain/
|   |-- Desafio.CompraProgramada.Application/
|   |-- Desafio.CompraProgramada.Infrastructure/
|   `-- Desafio.CompraProgramada.Api/
|-- tests/
|   |-- Desafio.CompraProgramada.UnitTests/
|   `-- Desafio.CompraProgramada.IntegrationTests/
`-- docker-compose.yml
```

## Como rodar

### 1) Subir MySQL e Kafka
```bash
docker compose up -d
```

Se a porta `3306` ja estiver ocupada no host:
```bash
MYSQL_HOST_PORT=3307 docker compose up -d
```

Nesse caso, rode a API com:
```bash
ConnectionStrings__MySql="Server=localhost;Port=3307;Database=desafio_compra_programada;Uid=root;Pwd=root123;" dotnet run --project src/Desafio.CompraProgramada.Api/Desafio.CompraProgramada.Api.csproj --no-launch-profile
```

### 2) Rodar API
```bash
dotnet run --project src/Desafio.CompraProgramada.Api/Desafio.CompraProgramada.Api.csproj
```

A API sobe com Swagger em:
- `http://localhost:5000/swagger` (ou porta exibida no console)

### 3) Painel visual basico
A aplicacao inclui um frontend basico para validar os fluxos:
- `http://localhost:5000/`

Ele permite:
- cadastrar/alterar cesta
- aderir cliente
- alterar valor mensal
- sair do produto
- executar compra
- rebalancear por desvio
- consultar carteira/rentabilidade
- consultar endpoints administrativos

## Configuracao

Arquivo: `src/Desafio.CompraProgramada.Api/appsettings.json`

Principais chaves:
- `ConnectionStrings:MySql`
- `Kafka:Habilitado`
- `Kafka:BootstrapServers`
- `Kafka:TopicoFiscal`
- `Cotacoes:PastaCotacoes`
- `Motor:LimiarDesvioPontosPercentuais`
- `Motor:HabilitarAgendamentoCompra`
- `Motor:IntervaloMinutosAgendamento`

No `appsettings.Development.json`, Kafka e agendamento automatico ficam desabilitados e o banco pode rodar em memoria (quando sem connection string).

## COTAHIST

O parser utiliza arquivos em `cotacoes/` com layout posicional da B3.
Foram adicionados arquivos de exemplo:
- `COTAHIST_D20260205.TXT`
- `COTAHIST_D20260225.TXT`
- `COTAHIST_D20260301.TXT`

## Testes e cobertura

Rodar todos os testes com cobertura:
```bash
dotnet test DesafioCompraProgramada.slnx --collect:"XPlat Code Coverage" --results-directory ./TestResults
```

Status atual:
- Unit tests: 13/13
- Integration tests: 7/7
- Cobertura (XPlat, ultimo run): `70.66%` e `76.22%` de linhas (relatorios gerados em `TestResults/*/coverage.cobertura.xml`)

## Observacoes tecnicas

- Eventos fiscais sao persistidos em banco (`eventos_fiscais`) e publicados no Kafka quando habilitado.
- Se Kafka estiver desabilitado/indisponivel no ambiente de desenvolvimento, o sistema continua funcional para testes locais de fluxo.
- Em `appsettings.json` (padrao), o scheduler automatico do motor fica habilitado para tentar execucao nas datas validas de compra.
- O motor de compra usa truncamento para quantidade de ativos, conforme regra do desafio.
