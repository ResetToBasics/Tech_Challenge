using Desafio.CompraProgramada.Application.Interfaces;

namespace Desafio.CompraProgramada.Infrastructure.Services;

public class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
