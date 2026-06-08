using Testcontainers.PostgreSql;
using Xunit;

namespace FuelFlow.IntegrationTests;

public sealed class TestDatabaseFixture : IAsyncLifetime
{
    public PostgreSqlContainer DbContainer { get; } = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("fuelflow_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public async Task InitializeAsync()
    {
        await DbContainer.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await DbContainer.DisposeAsync();
    }
}
