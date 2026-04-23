using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace NewsAggregator.IntegrationTests;

public sealed class NewsAggregatorAppFactory : WebApplicationFactory<Program>
{
    private string _cs = "";

    public void SetConnectionString(string s) => _cs = s;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        if (string.IsNullOrEmpty(_cs))
            throw new InvalidOperationException("Set connection string first.");
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Default", _cs);
    }
}
