namespace Mdsweep.Infrastructure.Email;

public class EmailOptions
{
    public const string SectionName = "Email";

    public required string ConnectionStringName { get; set; }
    public required string Host { get; set; }
    public int Port { get; set; }
    public required string From { get; set; }
    public bool UseStartTls { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
}
