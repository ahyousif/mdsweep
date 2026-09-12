using System.Security.Cryptography;
using Mdsweep.Application.Common.Security;

namespace Mdsweep.Infrastructure.Identity;

public sealed class SecureTokenService : ITokenService
{
    public Token Generate(int lengthBytes = 32)
    {
        if (lengthBytes < 16)
        {
            lengthBytes = 16;
        }

        Span<byte> buffer = stackalloc byte[lengthBytes];
        RandomNumberGenerator.Fill(buffer);
        var token = Convert.ToHexString(buffer);
        var hash = Hash(token);
        return new Token(token, hash);
    }

    public string Hash(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }

    public bool Verify(string token, string expectedHash)
    {
        var computed = Hash(token);
        return string.Equals(computed, expectedHash, StringComparison.Ordinal);
    }
}
