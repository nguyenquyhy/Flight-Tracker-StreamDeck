using Microsoft.Extensions.DependencyInjection;
using System;

namespace FlightStreamDeck.AddOn;

public class ServiceProviderFactory : IServiceProviderFactory<IServiceCollection>
{
    private readonly IServiceCollection serviceCollection;

    private ServiceProvider? serviceProvider = null;

    public ServiceProviderFactory(IServiceCollection serviceCollection)
    {
        this.serviceCollection = serviceCollection;
    }

    public IServiceCollection CreateBuilder(IServiceCollection services)
    {
        foreach (var service in services)
        {
            serviceCollection.Add(service);
        }
        return serviceCollection;
    }

    public IServiceProvider CreateServiceProvider(IServiceCollection serviceCollection)
    {
        if (serviceProvider != null)
            return serviceProvider;

        serviceProvider = serviceCollection.BuildServiceProvider();
        return serviceProvider;
    }
}
