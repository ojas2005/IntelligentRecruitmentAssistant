using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IRA.IntegrationTests;

/// <summary>
/// Spins up the real IRA.Api in-memory and swaps authentication for the header-driven
/// <see cref="TestAuthHandler"/> so authorization policies can be verified deterministically.
/// </summary>
public class RecruitmentApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // "Testing" (not "Development") so the developer's appsettings.Development.Local.json
        // secrets overlay is never loaded; combined with the override below this keeps tests
        // deterministic on the offline fallbacks.
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(OfflineConfig.Overrides));

        builder.ConfigureTestServices(services =>
        {
            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        });
    }
}
