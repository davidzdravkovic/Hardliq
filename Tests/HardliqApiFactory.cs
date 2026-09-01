using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.PostgreSql;
using Xunit;

namespace Hardliq.Api.Tests;

[CollectionDefinition(nameof(HardliqApiCollection))]
public sealed class HardliqApiCollection : ICollectionFixture<HardliqApiFactory>;

public sealed class HardliqApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting(
            "ConnectionStrings:DefaultConnection",
            _postgres.GetConnectionString());
        builder.UseSetting("Jwt:Key", "integration-test-jwt-key-32chars-min!");
        builder.UseSetting("Jwt:Issuer", "Hardliq");
        builder.UseSetting("Jwt:Audience", "Hardliq");
        builder.UseSetting("Database:AutoMigrate", "true");
        builder.UseSetting("Rag:BaseUrl", "http://127.0.0.1:8000");
        builder.UseSetting("Rag:InternalKey", "integration-test-internal-key");
    }
}
