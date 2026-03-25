using Application.Core;
using Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Application.Extensions
{
    /// <summary>
    /// Extension methods for registering MediatR-like services.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers the mediator and all request handlers from a specified assembly.
        /// </summary>
        public static IServiceCollection AddCustomMediator(this IServiceCollection services, Assembly assembly)
        {
            services.AddScoped<IMediator, Mediator>();

            var handlers = assembly
                .GetTypes()
                .Where(t => t.GetInterfaces()
                    .Any(i => i.IsGenericType &&
                             i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)))
                .ToList();

            foreach (var handler in handlers)
            {
                var handlerInterfaces = handler
                    .GetInterfaces()
                    .Where(i => i.IsGenericType &&
                               i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>));

                foreach (var handlerInterface in handlerInterfaces)
                {
                    services.AddScoped(handlerInterface, handler);
                }
            }

            return services;
        }
    }
}