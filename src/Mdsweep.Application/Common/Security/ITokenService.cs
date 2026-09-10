namespace Mdsweep.Application.Common.Security;

public interface ITokenService
{
    Token Generate(int lengthBytes = 32);
    string Hash(string token);
}
