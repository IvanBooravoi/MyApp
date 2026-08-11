using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyApp.Application.Abstractions;
using MyApp.Application.Security;
using MyApp.Infrastructure.Db;
using MyApp.Infrastructure.Documents;
using MyApp.Infrastructure.Repositories;
using MyApp.Infrastructure.Security;
using Npgsql;

namespace MyApp.Infrastructure;

public static class InfrastructureRegistration
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString,
        JwtOptions jwtOptions,
        string componentPdfTemplatePath)
    {
        services.AddDbContext<AppDbContext>(
            options => options.UseNpgsql(connectionString));
        services.AddSingleton(NpgsqlDataSource.Create(connectionString));
        services.AddSingleton(jwtOptions);
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IProfessionRepository, ProfessionRepository>();
        services.AddScoped<ITableViewRepository, TableViewRepository>();
        services.AddScoped<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddScoped<ITokenProvider, JwtTokenProvider>();
        services.AddScoped<IDatabaseInitializer, DatabaseInitializer>();
        services.AddSingleton<IComponentDocumentRenderer>(
            new PdfComponentDocumentRenderer(componentPdfTemplatePath));
        return services;
    }

    public static async Task InitializeDatabaseAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var initializer = scope.ServiceProvider
            .GetRequiredService<IDatabaseInitializer>();
        await initializer.InitializeAsync(cancellationToken);
    }
}
