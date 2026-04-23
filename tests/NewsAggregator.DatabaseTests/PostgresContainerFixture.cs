using Testcontainers.PostgreSql;

namespace NewsAggregator.DatabaseTests;

public sealed class PostgresContainerFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _c;

    public string ConnectionString =>
        _c?.GetConnectionString() ?? throw new InvalidOperationException("No container.");

    public async Task InitializeAsync()
    {
        _c = new PostgreSqlBuilder().WithImage("postgres:16-alpine").Build();
        await _c.StartAsync();
    }

    public async Task DisposeAsync()
    {
        if (_c is not null) await _c.DisposeAsync();
    }
}

[CollectionDefinition("db")]
public class DbCol : ICollectionFixture<PostgresContainerFixture> { }
