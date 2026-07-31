using Microsoft.AspNetCore.Identity;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Domain.Entities;

namespace TaskMgmt.Infrastructure.Identity;

public class PasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<User> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(default!, password);

    public bool Verify(string hash, string password) =>
        _hasher.VerifyHashedPassword(default!, hash, password) != PasswordVerificationResult.Failed;
}
