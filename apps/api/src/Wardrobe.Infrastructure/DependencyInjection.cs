using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wardrobe.Application.Common.Interfaces;
using Wardrobe.Infrastructure.Persistence;
using Wardrobe.Infrastructure.Services;

namespace Wardrobe.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<WardrobeDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IWardrobeDbContext>(provider =>
            provider.GetRequiredService<WardrobeDbContext>());

        services.AddSingleton<IPasswordHasherService, PasswordHasherService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IEmailSenderService, EmailSenderService>();

        return services;
    }
}
