using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Non.Application.Common;
using Non.Domain.Entities;
using Non.Domain.Enums;
using Non.Infrastructure.Persistence.Data;

namespace Non.Infrastructure.Persistence.Seeds;

public static class DbSeeder
{
    private const string AdminFirstName = "Admin";
    private const string AdminLastName = "Adminov";
    private const string AdminPhone = "+992987519495";

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (await context.Users.AnyAsync(u => u.Role == UserRole.Admin))
            return;

        var phone = PhoneNumberNormalizer.Normalize(AdminPhone)
                    ?? throw new InvalidOperationException("Неверный номер телефона админа в DbSeeder.");

        context.Users.Add(new User
        {
            FirstName = AdminFirstName,
            LastName = AdminLastName,
            Phone = phone,
            Role = UserRole.Admin,
            IsActive = true
        });

        await context.SaveChangesAsync();
    }
}