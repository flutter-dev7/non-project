using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Non.Application.Common;
using Non.Application.Interfaces.Services;
using Non.Domain.Entities;
using Non.Domain.Enums;
using Non.Infrastructure.Persistence.Data;

namespace Non.Infrastructure.Persistence.Seeds;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var passwords = scope.ServiceProvider.GetRequiredService<IPasswordService>();

        var email = EmailNormalizer.Normalize(configuration["Seed:AdminEmail"]);
        if (email is null)
            return;

        if (await context.Users.AnyAsync(u => u.Email == email))
            return;

        var password = configuration["Seed:AdminPassword"];
        if (!string.IsNullOrEmpty(password))
        {
            var passwordError = PasswordRules.Validate(password);
            if (passwordError is not null)
                throw new InvalidOperationException($"Seed:AdminPassword: {passwordError}");
        }

        var admin = await context.Users
            .OrderBy(u => u.CreatedAt)
            .FirstOrDefaultAsync(u => u.Role == UserRole.Admin && u.Email == null);

        if (admin is null)
        {
            if (await context.Users.AnyAsync(u => u.Role == UserRole.Admin))
                return;

            var phone = PhoneNumberNormalizer.Normalize(configuration["Seed:AdminPhone"])
                ?? throw new InvalidOperationException("Для создания админа задайте Seed:AdminPhone.");

            admin = new User
            {
                FirstName = configuration["Seed:AdminFirstName"] ?? "Admin",
                LastName = configuration["Seed:AdminLastName"] ?? "Non",
                Phone = phone,
                Role = UserRole.Admin,
                IsActive = true
            };

            context.Users.Add(admin);
        }

        admin.Email = email;
        admin.EmailConfirmed = true;

        if (!string.IsNullOrEmpty(password))
            admin.PasswordHash = passwords.Hash(password);

        await context.SaveChangesAsync();
    }
}