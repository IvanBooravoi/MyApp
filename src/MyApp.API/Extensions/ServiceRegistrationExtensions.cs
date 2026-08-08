using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace MyApp.API.Extensions
{
    public static class ServiceRegistrationExtensions
    {
        public static IServiceCollection AddBusinessServices(this IServiceCollection services)
        {
            var asm = Assembly.Load("MyApp.Application");
            var servicesTypes = asm.GetTypes().Where(t => t.IsClass && t.Name.EndsWith("Service"));

            foreach (var impl in servicesTypes)
            {
                foreach (var iface in impl.GetInterfaces())
                {
                    services.AddScoped(iface, impl);
                }
            }

            return services;
        }
    }
}
