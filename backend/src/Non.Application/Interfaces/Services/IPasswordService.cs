namespace Non.Application.Interfaces.Services;

public interface IPasswordService
{
    string Hash(string password);

    bool Verify(string? hash, string password);
}