using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Non.Application.Common;
using Non.Application.Interfaces.Repositories;
using Non.Application.Interfaces.Services;
using Non.Application.Services;
using Non.Infrastructure.Persistence.Data;
using Non.Infrastructure.Repositories;
using Non.Infrastructure.Services;

namespace Non.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
                               ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        services.AddMemoryCache();
        services.AddSingleton<ICacheService, CacheService>();
        services.AddSingleton<ISmsSender, LogSmsSender>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IShopRepository, ShopRepository>();

        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IShopService, ShopService>();
        
        services.AddSingleton(TimeProvider.System);

        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<ISettingRepository, SettingRepository>();

        services.AddScoped<IOrderService, OrderService>();
        
        services.AddScoped<IPaymentRepository, PaymentRepository>();

        services.AddScoped<IDebtService, DebtService>();
        services.AddScoped<IPaymentService, PaymentService>();
        
        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddScoped<IReportService, ReportService>();
        
        services.AddScoped<IVisitRepository, VisitRepository>();
        services.AddScoped<IVisitService, VisitService>();
        
        services.AddScoped<ISettingService, SettingService>();
        
        return services;
    }
}