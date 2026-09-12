using Mdsweep.Domain.Users;
using Mdsweep.Infrastructure.Persistence;
using Npgsql;

namespace Mdsweep.Api.IntegrationTests;

public sealed class UserEmailTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void User_creation_requires_email(string? email)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            UserAggregate.Create("Synthetic", "User", "synthetic-subject", email!)
        );
    }
}

public sealed class UserEmailPersistenceTests : MdsweepIntegrationTest
{
    [Fact]
    public async Task Database_rejects_null_User_email()
    {
        await using var scope = Application.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var error = await Assert.ThrowsAsync<PostgresException>(() =>
            db.Database.ExecuteSqlRawAsync("UPDATE users SET email = NULL WHERE keycloak_user_id = 'dispatcher-test'")
        );
        Assert.Equal(PostgresErrorCodes.NotNullViolation, error.SqlState);
        Assert.Equal("dispatcher@example.test", (await db.Users.SingleAsync()).Email);
    }
}
