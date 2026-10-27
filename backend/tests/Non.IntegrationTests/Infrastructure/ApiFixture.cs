using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Non.Application.Common;
using Non.Application.DTOs.Auth.Request;
using Non.Application.DTOs.Auth.Response;
using Non.Application.DTOs.Orders.Request;
using Non.Application.DTOs.Orders.Response;
using Non.Application.DTOs.Payments.Request;
using Non.Application.DTOs.Payments.Response;
using Non.Application.DTOs.Products.Request;
using Non.Application.DTOs.Products.Response;
using Non.Application.DTOs.Shops.Request;
using Non.Application.DTOs.Shops.Response;
using Non.Application.DTOs.Debts.Response;
using Non.Application.DTOs.Devices.Request;
using Non.Application.Interfaces.Services;
using Non.Domain.Entities;
using Non.Domain.Enums;
using Non.Infrastructure.Persistence.Data;
using Npgsql;
using Xunit;

namespace Non.IntegrationTests.Infrastructure;

public record ShopContext(Guid ShopId, ApiClient Owner);

public class ApiFixture : IAsyncLifetime
{
    public static readonly string ConnectionString = ResolveConnectionString();

    private NonApiFactory? _factory;

    public const string TestPassword = "Test12345";

    private string? _testPasswordHash;

    public CapturingEmailSender Email { get; } = new();    public CapturingPushSender Push { get; } = new();

    public IServiceProvider Services => _factory!.Services;
    

    private static string ResolveConnectionString()
    {
        var fromEnvironment = Environment.GetEnvironmentVariable("NON_TEST_CONNECTION");

        if (!string.IsNullOrWhiteSpace(fromEnvironment))
            return fromEnvironment;

        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var path = Path.Combine(directory.FullName, "src", "Non.API", "appsettings.Development.json");

            if (File.Exists(path))
            {
                using var document = JsonDocument.Parse(File.ReadAllText(path));

                var devConnection = document.RootElement
                    .GetProperty("ConnectionStrings")
                    .GetProperty("DefaultConnection")
                    .GetString()
                    ?? throw new InvalidOperationException("В appsettings.Development.json нет ConnectionStrings:DefaultConnection.");

                return new NpgsqlConnectionStringBuilder(devConnection) { Database = "non_tests" }.ConnectionString;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            "Не найден src/Non.API/appsettings.Development.json. Задай переменную окружения NON_TEST_CONNECTION.");
    }
    
    public async Task RegisterFcmTokenAsync(ApiClient client, string token) =>
        (await client.PutAsync<object>("/api/devices/fcm-token", new RegisterFcmTokenRequest { Token = token })).Require();

    public async Task<string?> GetFcmTokenAsync(Guid userId)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.FcmToken)
            .SingleAsync();
    }

    public async Task InitializeAsync()
    {
        var database = new NpgsqlConnectionStringBuilder(ConnectionString).Database ?? string.Empty;

        if (!database.Contains("test", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"Тестовая база должна содержать 'test' в имени, сейчас '{database}'. Тесты остановлены, чтобы не стереть рабочие данные.");

        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", ConnectionString);
        Environment.SetEnvironmentVariable("Jwt__Secret", "integration-tests-secret-key-0123456789-abcdefghij");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "Non.API");
        Environment.SetEnvironmentVariable("Jwt__Audience", "Non.Clients");
        Environment.SetEnvironmentVariable("Jwt__ExpirationMinutes", "60");
        Environment.SetEnvironmentVariable("RateLimiting__AuthPermitPerMinute", "100000");

        var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(ConnectionString).Options;

        await using (var db = new AppDbContext(options))
        {
            await db.Database.EnsureDeletedAsync();
            await db.Database.MigrateAsync();
        }

        _factory = new NonApiFactory(Email, Push);
        _ = _factory.Services;
    }
    
    public async Task<string> GetEmailAsync(Guid userId)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.Email!)
            .SingleAsync();
    }

    public async Task ClearAuthThrottlesAsync(string email)
    {
        var cache = Services.GetRequiredService<ICacheService>();
        var normalized = email.ToLowerInvariant();

        await cache.RemoveAsync(AuthCacheKeys.Cooldown(EmailCodePurpose.EmailVerification, normalized));
        await cache.RemoveAsync(AuthCacheKeys.Cooldown(EmailCodePurpose.PasswordReset, normalized));
        await cache.RemoveAsync(AuthCacheKeys.LoginFailures(normalized));
    }

    public async Task<AuthResponse> LoginAsync(string phone)
    {
        string email;

        using (var scope = Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            email = await db.Users
                .AsNoTracking()
                .Where(u => u.Phone == phone)
                .Select(u => u.Email!)
                .SingleAsync();
        }

        await ClearAuthThrottlesAsync(email);

        return (await CreateAnonymousClient().PostAsync<AuthResponse>("/api/auth/login",
            new LoginRequest { Email = email, Password = TestPassword })).Require();
    }

    public async Task DisposeAsync()
    {
        if (_factory is not null)
            await _factory.DisposeAsync();
    }

    public ApiClient CreateAnonymousClient() => new(_factory!.CreateClient());

    public ApiClient CreateClientWithToken(string token)
    {
        var client = CreateAnonymousClient();
        client.SetToken(token);

        return client;
    }

    public async Task<ApiClient> CreateUserAsync(UserRole role)
    {
        var phone = "+9929" + Random.Shared.Next(0, 100_000_000).ToString("D8");
        var email = $"user-{Guid.NewGuid():N}@test.local";

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        _testPasswordHash ??= scope.ServiceProvider.GetRequiredService<IPasswordService>().Hash(TestPassword);

        var user = new User
        {
            FirstName = "Тест",
            LastName = role.ToString(),
            Phone = phone,
            Email = email,
            EmailConfirmed = true,
            PasswordHash = _testPasswordHash,
            Role = role,
            IsActive = true
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        var token = scope.ServiceProvider.GetRequiredService<IJwtTokenGenerator>().Generate(user);

        var http = _factory!.CreateClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);

        return new ApiClient(http, user.Id, phone);
    }

    public Task ClearUserAccessCacheAsync(Guid userId) =>
        Services.GetRequiredService<ICacheService>().RemoveAsync(CacheKeys.UserAccess(userId));

    public async Task<Guid> CreateProductAsync(ApiClient admin, decimal price = 5m)
    {
        var response = await admin.PostAsync<ProductResponse>("/api/products", new CreateProductRequest
        {
            Name = "Нон " + Guid.NewGuid().ToString("N")[..10],
            WeightInGrams = 300,
            Price = price
        });

        return response.Require().Id;
    }

    public async Task<ShopContext> CreateShopAsync(ApiClient admin, decimal? debtLimit = null)
    {
        var owner = await CreateUserAsync(UserRole.Shop);

        var created = await admin.PostAsync<ShopResponse>("/api/shops", new CreateShopRequest
        {
            Name = "Магазин " + Guid.NewGuid().ToString("N")[..8],
            Address = "ул. Рудаки 10",
            Phone = owner.Phone
        });

        var shopId = created.Require().Id;

        (await admin.PutAsync<ShopResponse>($"/api/shops/{shopId}/owner",
            new SetOwnerRequest { OwnerPhone = owner.Phone })).Require();

        if (debtLimit.HasValue)
        {
            (await admin.PutAsync<ShopResponse>($"/api/shops/{shopId}/debt-limit",
                new SetDebtLimitRequest { DebtLimit = debtLimit })).Require();
        }

        return new ShopContext(shopId, owner);
    }

    public Task<ApiResponse<OrderDetailsResponse>> PlaceOrderAsync(
        ApiClient shopClient, Guid productId, int quantity, PaymentType paymentType,
        int daysAhead = 5, Guid? id = null) =>
        shopClient.PostAsync<OrderDetailsResponse>("/api/orders", new CreateOrderRequest
        {
            Id = id,
            DeliveryDate = Dates.Ahead(daysAhead),
            PaymentType = paymentType,
            Items = new List<CreateOrderItemRequest>
            {
                new() { ProductId = productId, Quantity = quantity }
            }
        });

    public async Task AdvanceToOnTheWayAsync(ApiClient admin, ApiClient courier, Guid orderId)
    {
        (await admin.PutAsync<OrderDetailsResponse>($"/api/orders/{orderId}/courier",
            new AssignCourierRequest { CourierId = courier.UserId })).Require();

        foreach (var status in new[] { OrderStatus.Accepted, OrderStatus.Baking })
        {
            (await admin.PutAsync<OrderDetailsResponse>($"/api/orders/{orderId}/status",
                new ChangeOrderStatusRequest { Status = status })).Require();
        }

        (await courier.PutAsync<OrderDetailsResponse>($"/api/orders/{orderId}/status",
            new ChangeOrderStatusRequest { Status = OrderStatus.OnTheWay })).Require();
    }

    public async Task<OrderDetailsResponse> DeliverAsync(
        ApiClient admin, Guid orderId, decimal? paymentAmount = null)
    {
        var courier = await CreateUserAsync(UserRole.Courier);

        await AdvanceToOnTheWayAsync(admin, courier, orderId);

        var response = await courier.PutAsync<OrderDetailsResponse>($"/api/orders/{orderId}/status",
            new ChangeOrderStatusRequest
            {
                Status = OrderStatus.Delivered,
                PaymentAmount = paymentAmount,
                PaymentMethod = paymentAmount.HasValue ? PaymentMethod.Cash : (PaymentMethod?)null
            });

        return response.Require();
    }

    public async Task<ShopBalanceResponse> GetBalanceAsync(ApiClient admin, Guid shopId) =>
        (await admin.GetAsync<ShopBalanceResponse>($"/api/shops/{shopId}/balance")).Require();

    public Task<ApiResponse<PaymentResponse>> PayAsync(ApiClient admin, Guid shopId, decimal amount) =>
        admin.PostAsync<PaymentResponse>("/api/payments", new CreatePaymentRequest
        {
            ShopId = shopId,
            Amount = amount,
            Method = PaymentMethod.Cash
        });

    private sealed class NonApiFactory : WebApplicationFactory<Program>
    {
        private readonly IEmailSender _email;
        private readonly IPushSender _push;

        public NonApiFactory(IEmailSender email, IPushSender push)
        {
            _email = email;
            _push = push;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureLogging(logging => logging.AddProvider(new CapturingLoggerProvider()));

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IEmailSender>();
                services.AddSingleton(_email);

                services.RemoveAll<IPushSender>();
                services.AddSingleton(_push);
            });
        }
    }
}

[CollectionDefinition(Name)]
public class ApiCollection : ICollectionFixture<ApiFixture>
{
    public const string Name = "api";
}