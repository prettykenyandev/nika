using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NikaFitness.Application.Common.Interfaces;
using NikaFitness.Infrastructure.Carts;
using NikaFitness.Infrastructure.Files;
using NikaFitness.Infrastructure.Identity;
using NikaFitness.Infrastructure.Payments.Mpesa;
using NikaFitness.Infrastructure.Persistence;
using NikaFitness.Infrastructure.Sequences;

namespace NikaFitness.Infrastructure;

public static class DependencyInjection
{
    public const string DatabaseConnectionName = "nikadb";
    public const string RedisConnectionName = "redis";

    public static IHostApplicationBuilder AddInfrastructure(this IHostApplicationBuilder builder)
    {
        // Aspire-managed integrations: connection strings are injected by the AppHost.
        builder.AddNpgsqlDbContext<ApplicationDbContext>(DatabaseConnectionName);
        builder.AddRedisClient(RedisConnectionName);

        var services = builder.Services;

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<ICartStore, RedisCartStore>();

        services.AddScoped<IDocumentNumberGenerator, DocumentNumberGenerator>();
        services.AddSingleton<IFileStorage, LocalFileStorage>();

        AddMpesa(services, builder.Configuration);

        services.AddScoped<ApplicationDbContextSeeder>();

        return builder;
    }

    private static void AddMpesa(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MpesaOptions>(configuration.GetSection(MpesaOptions.SectionName));

        services.AddHttpClient<MpesaClient>((sp, http) =>
        {
            var options = configuration.GetSection(MpesaOptions.SectionName).Get<MpesaOptions>()
                          ?? new MpesaOptions();
            http.BaseAddress = new Uri(options.BaseUrl);
            http.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddScoped<IPaymentGateway, MpesaPaymentGateway>();
    }
}
