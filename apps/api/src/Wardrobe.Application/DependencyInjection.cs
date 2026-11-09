using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Wardrobe.Application.Auth.Services;
using Wardrobe.Application.Common.Interfaces;

namespace Wardrobe.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}
