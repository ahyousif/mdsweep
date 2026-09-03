namespace Mdsweep.Domain.Tenants;

public static class TenantIdentifier
{
    private const string Alphabet = "abcdefghjkmnpqrstuvwxyz23456789";

    public static bool IsValid(string value)
    {
        if (value.Length != 14 || value[4] != '-' || value[9] != '-')
        {
            return false;
        }

        return value.Where(character => character != '-').All(character => Alphabet.Contains(character));
    }
}
