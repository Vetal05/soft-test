using Microsoft.Extensions.DependencyInjection;
using NewsAggregator.Api.Data;
using Testcontainers.PostgreSql;

namespace NewsAggregator.IntegrationTests;

public sealed class PostgresWithLargeSeedFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _db;

    public NewsAggregatorAppFactory Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        _db = new PostgreSqlBuilder().WithImage("postgres:16-alpine").Build();
        await _db.StartAsync();
        var f = new NewsAggregatorAppFactory();
        f.SetConnectionString(_db.GetConnectionString());
        Factory = f;
        _ = Factory.CreateClient();
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var st = await LargeDataSeeder.SeedAsync(db, cancellationToken: default);
        if (st.TotalRows < LargeDataSeeder.MinTotalRowCount)
            throw new InvalidOperationException("Seed < 10k");
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        if (_db is not null) await _db.DisposeAsync();
    }
}

[CollectionDefinition("integration")]
public class IntCol : ICollectionFixture<PostgresWithLargeSeedFixture> { }
