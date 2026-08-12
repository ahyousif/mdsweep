using Microsoft.AspNetCore.Identity;

namespace Mdsweep.Api.Infrastructure;

public static class BootstrapDispatcher
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
    {
        var email = configuration["BootstrapDispatcher:Email"];
        var password = configuration["BootstrapDispatcher:Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)) return;

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        if (!await roleManager.RoleExistsAsync("Dispatcher"))
            await roleManager.CreateAsync(new IdentityRole("Dispatcher"));

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByNameAsync(email);
        if (user is null)
        {
            user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
            var created = await userManager.CreateAsync(user, password);
            if (!created.Succeeded)
                throw new InvalidOperationException(string.Join("; ", created.Errors.Select(x => x.Description)));
        }
        if (!await userManager.IsInRoleAsync(user, "Dispatcher"))
            await userManager.AddToRoleAsync(user, "Dispatcher");
    }
}
