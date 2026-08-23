using System.Net.Http.Headers;
using System.Net.Http.Json;
using TaskManager.Dto.ResponsesDto;
using Xunit;

namespace Hardliq.Api.Tests.Isolation;

internal sealed class IsolationSupport(HttpClient client)
{
    public void Authenticate(string token) =>
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

    public async Task<string> RegisterAsync(string prefix)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var response = await client.PostAsJsonAsync("/auth/register", new
        {
            username = $"{prefix}{suffix}",
            email = $"{prefix}{suffix}@test.com",
            password = "Password1!"
        });

        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<AuthRegisterResponse>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body.Token));

        return body.Token;
    }

    public async Task<(string TokenA, string TokenB, TopicResponse Folder)> CreateFolderOwnedByOtherUserAsync()
    {
        var tokenA = await RegisterAsync("userA");
        var tokenB = await RegisterAsync("userB");

        Authenticate(tokenA);

        var create = await client.PostAsJsonAsync(
            "/topics",
            new { name = $"IsoA-{Guid.NewGuid():N}" });
        create.EnsureSuccessStatusCode();

        var folder = await create.Content.ReadFromJsonAsync<TopicResponse>();
        Assert.NotNull(folder);

        return (tokenA, tokenB, folder);
    }

    public async Task<(string TokenA, string TokenB, TopicResponse Folder, CreateTaskResponse Task)>
        CreateTaskOwnedByOtherUserAsync()
    {
        var (tokenA, tokenB, folder) = await CreateFolderOwnedByOtherUserAsync();

        var createTask = await client.PostAsJsonAsync(
            $"/topics/{folder.Id}/tasks",
            new { name = "A task", description = "owned by A" });
        createTask.EnsureSuccessStatusCode();

        var task = await createTask.Content.ReadFromJsonAsync<CreateTaskResponse>();
        Assert.NotNull(task);

        return (tokenA, tokenB, folder, task);
    }
}
