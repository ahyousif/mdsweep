using Mdsweep.Domain.Tenants;

namespace Mdsweep.Utility.Commands;

public abstract record UtilityCommand
{
    public sealed record TenantProvision(TenantProvisioningOptions Options) : UtilityCommand;

    public sealed record DatabaseMigrate : UtilityCommand;

    public sealed record DatabaseReset : UtilityCommand;
}

public sealed record TenantProvisioningOptions(
    string TenantId,
    string TenantName,
    string AdminEmail,
    string AdminFirstName,
    string AdminLastName,
    string? AdminDisplayName
);

internal static class UtilityArguments
{
    public const string Usage = """
        Usage:
          tenant provision --tenant-id <id> --tenant-name <name> --admin-email <email> --admin-first-name <name> --admin-last-name <name> [--admin-display-name <name>]
          database migrate
          database reset --confirm mdsweep
        """;

    public static bool TryParse(string[] args, out UtilityCommand? command, out string? error)
    {
        command = null;
        error = null;

        if (args is ["database", "migrate"])
        {
            command = new UtilityCommand.DatabaseMigrate();
            return true;
        }

        if (args is ["database", "reset", "--confirm", "mdsweep"])
        {
            command = new UtilityCommand.DatabaseReset();
            return true;
        }

        if (args.Length < 2 || args[0] != "tenant" || args[1] != "provision")
        {
            error = "Expected 'tenant provision', 'database migrate', or 'database reset'.";
            return false;
        }

        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var index = 2; index < args.Length; index += 2)
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
            [
                "--tenant-id",
                "--tenant-name",
                "--admin-email",
                "--admin-first-name",
                "--admin-last-name",
                "--admin-display-name",
            ],
            StringComparer.Ordinal
        );
        var unknown = values.Keys.FirstOrDefault(key => !supported.Contains(key));
        if (unknown is not null)
        {
            error = $"Unknown option '{unknown}'.";
            return false;
        }

        string[] required =
        [
            "--tenant-id",
            "--tenant-name",
            "--admin-email",
            "--admin-first-name",
            "--admin-last-name",
        ];
        foreach (var option in required)
        {
            if (!values.TryGetValue(option, out var value) || string.IsNullOrWhiteSpace(value))
            {
                error = $"Required option '{option}' is missing or empty.";
                return false;
            }
        }

        if (!TenantIdentifier.IsValid(values["--tenant-id"]))
        {
            error = "Tenant ID must use the xxxx-xxxx-xxxx lowercase unambiguous format.";
            return false;
        }

        if (values.TryGetValue("--admin-display-name", out var displayName) && string.IsNullOrWhiteSpace(displayName))
        {
            error = "Option '--admin-display-name' cannot be empty.";
            return false;
        }

        command = new UtilityCommand.TenantProvision(
            new TenantProvisioningOptions(
                values["--tenant-id"],
                values["--tenant-name"],
                values["--admin-email"],
                values["--admin-first-name"],
                values["--admin-last-name"],
                displayName
            )
        );
        return true;
    }
}
