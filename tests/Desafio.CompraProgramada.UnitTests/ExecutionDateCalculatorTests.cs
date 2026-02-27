using Desafio.CompraProgramada.Application.Abstractions;
using FluentAssertions;

namespace Desafio.CompraProgramada.UnitTests;

public class ExecutionDateCalculatorTests
{
    [Fact]
    public void DeveCalcularDatasExecucaoComAjusteFimDeSemana()
    {
        var mapa = ExecutionDateCalculator.ObterMapaDatasExecucao(2026, 2);

        mapa[5].Should().Be(new DateOnly(2026, 2, 5));
        mapa[15].Should().Be(new DateOnly(2026, 2, 16));
        mapa[25].Should().Be(new DateOnly(2026, 2, 25));
    }

    [Fact]
    public void DeveRetornarParcelaCorreta()
    {
        ExecutionDateCalculator.ObterParcela(new DateOnly(2026, 2, 5)).Should().Be("1/3");
        ExecutionDateCalculator.ObterParcela(new DateOnly(2026, 2, 16)).Should().Be("2/3");
        ExecutionDateCalculator.ObterParcela(new DateOnly(2026, 2, 25)).Should().Be("3/3");
    }

    [Fact]
    public void DeveValidarDataCorretaDeExecucao()
    {
        ExecutionDateCalculator.DataEhValidaParaExecucao(new DateOnly(2026, 2, 5)).Should().BeTrue();
        ExecutionDateCalculator.DataEhValidaParaExecucao(new DateOnly(2026, 2, 16)).Should().BeTrue();
        ExecutionDateCalculator.DataEhValidaParaExecucao(new DateOnly(2026, 2, 25)).Should().BeTrue();

        ExecutionDateCalculator.DataEhValidaParaExecucao(new DateOnly(2026, 2, 6)).Should().BeFalse();
    }
}
