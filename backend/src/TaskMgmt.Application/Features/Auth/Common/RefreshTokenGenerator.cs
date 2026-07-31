using System.Security.Cryptography;

namespace TaskMgmt.Application.Features.Auth.Common;

internal static class RefreshTokenGenerator
{
    public static string Generate() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
}
