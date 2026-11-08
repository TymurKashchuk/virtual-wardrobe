using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wardrobe.Application.Common.Interfaces;
using Wardrobe.Infrastructure.Persistence;

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

        return services;
    }
}
