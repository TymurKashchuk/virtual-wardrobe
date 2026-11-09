using Microsoft.AspNetCore.Identity;
using Wardrobe.Application.Common.Interfaces;
using Wardrobe.Domain.Entities;

namespace Wardrobe.Infrastructure.Services;

public class PasswordHasherService : IPasswordHasherService
{
    private readonly IPasswordHasher<User> _passwordHasher;
    private static readonly User DummyUser = new();

    public PasswordHasherService(IPasswordHasher<User>? passwordHasher = null)
    {
        _passwordHasher = passwordHasher ?? new PasswordHasher<User>();
    }

    public string HashPassword(string password)
    {
        return _passwordHasher.HashPassword(DummyUser, password);
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        var result = _passwordHasher.VerifyHashedPassword(DummyUser, passwordHash, password);
        return result == PasswordVerificationResult.Success ||
               result == PasswordVerificationResult.SuccessRehashNeeded;
    }
}
