using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using PizzaShop.Application.Common.Messaging;

namespace PizzaShop.Application;

/// <summary>
/// Wires the Application layer into DI: command/query handlers, FluentValidation
/// validators, and the <see cref="IDispatcher"/> (ADR-0012). Called once from Api's
/// composition root.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        RegisterImplementations(services, assembly, typeof(ICommandHandler<,>));
        RegisterImplementations(services, assembly, typeof(IQueryHandler<,>));
        RegisterImplementations(services, assembly, typeof(FluentValidation.IValidator<>));

        services.AddScoped<IDispatcher, Dispatcher>();

        return services;
    }

    private static void RegisterImplementations(IServiceCollection services, Assembly assembly, Type openGenericInterface)
    {
        var matches =
            from type in assembly.GetTypes()
            where !type.IsAbstract && !type.IsInterface
            from @interface in type.GetInterfaces()
            where @interface.IsGenericType && @interface.GetGenericTypeDefinition() == openGenericInterface
            select (Service: @interface, Implementation: type);

        foreach (var (service, implementation) in matches)
            services.AddScoped(service, implementation);
    }
}
