using Microsoft.AspNetCore.Identity;
using Non.Application.Interfaces.Services;
using Non.Domain.Entities;

namespace Non.Infrastructure.Services;

public class PasswordService : IPasswordService
{
    private static readonly User Placeholder = new();

    private readonly PasswordHasher<User> _hasher = new();
    private readonly string _dummyHash;

    public PasswordService()
    {
        _dummyHash = _hasher.HashPassword(Placeholder, Guid.NewGuid().ToString("N"));
    }

    public string Hash(string password) => _hasher.HashPassword(Placeholder, password);

    public bool Verify(string? hash, string password)
    {
        if (string.IsNullOrEmpty(hash))
        {
            _hasher.VerifyHashedPassword(Placeholder, _dummyHash, password);

            return false;
        }

        var result = _hasher.VerifyHashedPassword(Placeholder, hash, password);

        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}