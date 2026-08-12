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
        services.AddScoped<IComponentDocumentService, ComponentDocumentService>();
        services.AddScoped<IResponsibleEmployeeService, ResponsibleEmployeeService>();
        services.AddScoped<IRequirementJournalService, RequirementJournalService>();
        return services;
    }
}
