namespace Desafio.CompraProgramada.Application.Exceptions;

public class BusinessException : Exception
{
    public BusinessException(string codigo, string mensagem, int statusCode = 400)
        : base(mensagem)
    {
        Codigo = codigo;
        StatusCode = statusCode;
    }

    public string Codigo { get; }
    public int StatusCode { get; }
}
