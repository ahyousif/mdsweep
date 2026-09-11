using Mdsweep.Domain.Tenants;

namespace Mdsweep.Utility.BootstrapTenant;

internal static class BootstrapTenantArguments
{
    public const string Usage =
        "Usage: bootstrap-tenant --tenant-id <id> --tenant-name <name> "
        + "--keycloak-user-id <sub> --email <email> --first-name <name> --last-name <name> "
        + "[--display-name <name>]";

    public static bool TryParse(string[] args, out BootstrapTenantOptions? options, out string? error)
    {
        options = null;
        error = null;

        if (args.Length == 0 || !string.Equals(args[0], "bootstrap-tenant", StringComparison.Ordinal))
        {
            error = "The only supported command is 'bootstrap-tenant'.";
            return false;
        }

        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var index = 1; index < args.Length; index += 2)
        {
            if (index + 1 >= args.Length || !args[index].StartsWith("--", StringComparison.Ordinal))
            {
                error = $"Option '{args[index]}' requires a value.";
                return false;
            }

            if (!values.TryAdd(args[index], args[index + 1]))
            {
                error = $"Option '{args[index]}' was supplied more than once.";
                return false;
            }
        }

        var supported = new HashSet<string>(
            ["--tenant-id", "--tenant-name", "--keycloak-user-id", "--email", "--first-name", "--last-name", "--display-name"],
            StringComparer.Ordinal
        );
        var unknown = values.Keys.FirstOrDefault(key => !supported.Contains(key));
        if (unknown is not null)
        {
            error = $"Unknown option '{unknown}'.";
            return false;
        }

        string[] requiredOptions =
        {
            "--tenant-id",
            "--tenant-name",
            "--keycloak-user-id",
            "--email",
            "--first-name",
            "--last-name",
        };
        foreach (var requiredOption in requiredOptions)
        {
            if (!values.TryGetValue(requiredOption, out var value) || string.IsNullOrWhiteSpace(value))
            {
                error = $"Required option '{requiredOption}' is missing or empty.";
                return false;
            }
        }

        var tenantId = values["--tenant-id"];
        var tenantName = values["--tenant-name"];
        var keycloakUserId = values["--keycloak-user-id"];
        var email = values["--email"];
        var firstName = values["--first-name"];
        var lastName = values["--last-name"];

        if (!TenantIdentifier.IsValid(tenantId))
        {
            error = "Tenant ID must use the xxxx-xxxx-xxxx lowercase unambiguous format.";
            return false;
        }

        var displayName = values.TryGetValue("--display-name", out var suppliedDisplayName)
            ? suppliedDisplayName
            : $"{firstName} {lastName}";
        if (string.IsNullOrWhiteSpace(displayName))
        {
            error = "Option '--display-name' cannot be empty.";
            return false;
        }

        options = new BootstrapTenantOptions(
            tenantId,
            tenantName,
            keycloakUserId,
            email,
            firstName,
            lastName,
            displayName
        );
        return true;
    }
}
