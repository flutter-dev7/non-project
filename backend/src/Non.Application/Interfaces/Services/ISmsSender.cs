namespace Non.Application.Interfaces.Services;

public interface ISmsSender
{
    Task SendAsync(string phone, string message, CancellationToken ct = default);
}