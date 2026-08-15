using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace MyApp.API.Extensions
{
    public static class RepositoryReflectionExtensions
    {
        public static IServiceCollection AddRepositoriesByReflection(this IServiceCollection services)
        {
            var asm = Assembly.Load("MyApp.Infrastructure");
            var repos = asm.GetTypes().Where(t => t.IsClass && t.Name.EndsWith("Repository"));

            foreach (var impl in repos)
            {
                // Filter to get only repository interfaces (I*Repository)
                var iface = impl.GetInterfaces()
                    .FirstOrDefault(i => i.Name.StartsWith("I") && i.Name.EndsWith("Repository"));
                if (iface != null)
                {
                    services.AddScoped(iface, impl);
                }
            }

            services.AddScoped<MyApp.Infrastructure.UnitOfWork.IUnitOfWork, MyApp.Infrastructure.UnitOfWork.UnitOfWork>();
            return services;
        }
    }
}
