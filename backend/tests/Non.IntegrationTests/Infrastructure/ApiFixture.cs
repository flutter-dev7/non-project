using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Non.Application.Common;
using Non.Application.DTOs.Orders.Request;
using Non.Application.DTOs.Orders.Response;
using Non.Application.DTOs.Payments.Request;
using Non.Application.DTOs.Payments.Response;
using Non.Application.DTOs.Products.Request;
using Non.Application.DTOs.Products.Response;
using Non.Application.DTOs.Shops.Request;
using Non.Application.DTOs.Shops.Response;
using Non.Application.DTOs.Debts.Response;
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
    public static readonly string ConnectionString =
        Environment.GetEnvironmentVariable("NON_TEST_CONNECTION")
        ?? "Host=localhost;Port=5432;Database=non_tests;Username=postgres;Password=postgres";

    private NonApiFactory? _factory;

    public CapturingSmsSender Sms { get; } = new();

    public IServiceProvider Services => _factory!.Services;

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

        var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(ConnectionString).Options;

        await using (var db = new AppDbContext(options))
        {
            await db.Database.EnsureDeletedAsync();
            await db.Database.MigrateAsync();
        }

        _factory = new NonApiFactory(Sms);
        _ = _factory.Services;
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

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = new User
        {
            FirstName = "Тест",
            LastName = role.ToString(),
            Phone = phone,
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
        private readonly ISmsSender _sms;

        public NonApiFactory(ISmsSender sms)
        {
            _sms = sms;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ISmsSender>();
                services.AddSingleton(_sms);
            });
        }
    }
}

[CollectionDefinition(Name)]
public class ApiCollection : ICollectionFixture<ApiFixture>
{
    public const string Name = "api";
}