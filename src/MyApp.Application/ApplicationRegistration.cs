using Microsoft.Extensions.DependencyInjection;
using MyApp.Application.Services;

namespace MyApp.Application;

public static class ApplicationRegistration
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IProfessionService, ProfessionService>();
        services.AddScoped<ITableViewService, TableViewService>();
        return services;
    }
}
