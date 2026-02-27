namespace Desafio.CompraProgramada.Application.Abstractions;

public static class ExecutionDateCalculator
{
    private static readonly int[] DatasBase = [5, 15, 25];

    public static bool DataEhValidaParaExecucao(DateOnly data)
    {
        return ObterMapaDatasExecucao(data.Year, data.Month).Values.Contains(data);
    }

    public static string ObterParcela(DateOnly data)
    {
        var mapa = ObterMapaDatasExecucao(data.Year, data.Month);

        foreach (var item in mapa)
        {
            if (item.Value == data)
            {
                return item.Key switch
                {
                    5 => "1/3",
                    15 => "2/3",
                    25 => "3/3",
                    _ => "1/3"
                };
            }
        }

        throw new InvalidOperationException("Data de execucao invalida para parcela.");
    }

    public static IReadOnlyDictionary<int, DateOnly> ObterMapaDatasExecucao(int ano, int mes)
    {
        var mapa = new Dictionary<int, DateOnly>();

        foreach (var dia in DatasBase)
        {
            var dataBase = new DateOnly(ano, mes, dia);
            while (dataBase.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
            {
                dataBase = dataBase.AddDays(1);
            }

            mapa[dia] = dataBase;
        }

        return mapa;
    }
}
