using IRA.Application;
using IRA.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace IRA.UnitTests;

/// <summary>
/// Builds a fully-wired service provider using the real Application + Infrastructure
/// composition with empty Azure configuration, so every port resolves to its deterministic
/// offline fallback. This lets tests exercise the genuine DI graph and agent workflow.
/// </summary>
public static class TestFactory
{
    public static IServiceProvider CreateProvider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<Microsoft.Extensions.Logging.ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddLogging();
        services.AddApplication();
        services.AddInfrastructure(configuration);

        return services.BuildServiceProvider();
    }
}
