using System.Net;
using System.Net.Http.Json;
using TaskManager.Dto.ResponsesDto;
using TaskManager.Services;
using Xunit;
using Hardliq.Api.Tests;

namespace Hardliq.Api.Tests.Isolation;

[Collection(nameof(HardliqApiCollection))]
public sealed class TopicIsolationTests
{
    private readonly HttpClient _client;
    private readonly IsolationSupport _support;

    public TopicIsolationTests(HardliqApiFactory factory)
    {
        _client = factory.CreateClient();
        _support = new IsolationSupport(_client);
    }

    [Theory]
    [InlineData("GET", "/topics/{id}/delete-summary")]
    [InlineData("GET", "/topics/{id}/tasks")]
    [InlineData("GET", "/topics/stats?topicId={id}")]
    [InlineData("GET", "/topics?parentId={id}")]
    [InlineData("PATCH", "/topics/{id}")]
    [InlineData("DELETE", "/topics/{id}/children")]
    [InlineData("DELETE", "/topics/{id}")]
    [InlineData("POST", "/topics")]
    public async Task User_cannot_access_another_users_topic(string method, string path)
    {
        var (_, tokenB, folder) = await _support.CreateFolderOwnedByOtherUserAsync();

        _support.Authenticate(tokenB);

        var url = path.Replace("{id}", folder.Id.ToString());
        using var request = new HttpRequestMessage(new HttpMethod(method), url);
        request.Content = BodyFor(method, path, folder.Id);

        var asB = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, asB.StatusCode);
    }

    [Theory]
    [InlineData("/topics")]
    [InlineData("/topics/search?q={name}")]
    public async Task User_cannot_see_another_users_topic_in_collection(string path)
    {
        var (_, tokenB, folder) = await _support.CreateFolderOwnedByOtherUserAsync();

        _support.Authenticate(tokenB);

        var url = path.Replace("{name}", Uri.EscapeDataString(folder.Name));
        var asB = await _client.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, asB.StatusCode);
        Assert.DoesNotContain(await ReadCollectionIdsAsync(asB, path), id => id == folder.Id);
    }

    [Fact]
    public async Task User_cannot_see_another_users_tasks_in_workspace_stats()
    {
        var (tokenA, tokenB, folder) = await _support.CreateFolderOwnedByOtherUserAsync();

        _support.Authenticate(tokenA);

        var createTask = await _client.PostAsJsonAsync(
            $"/topics/{folder.Id}/tasks",
            new { name = "A task", description = "owned by A" });
        createTask.EnsureSuccessStatusCode();

        var asA = await _client.GetAsync("/topics/stats");
        asA.EnsureSuccessStatusCode();
        var statsA = await asA.Content.ReadFromJsonAsync<TaskStatsResult>();
        Assert.NotNull(statsA);
        Assert.True(statsA.TotalTasks >= 1);

        _support.Authenticate(tokenB);

        var asB = await _client.GetAsync("/topics/stats");
        Assert.Equal(HttpStatusCode.OK, asB.StatusCode);

        var statsB = await asB.Content.ReadFromJsonAsync<TaskStatsResult>();
        Assert.NotNull(statsB);
        Assert.Equal(0, statsB.TotalTasks);
        Assert.Equal(0, statsB.Pending);
        Assert.Equal(0, statsB.Completed);
        Assert.Equal(0, statsB.Canceled);
    }

    private static HttpContent? BodyFor(string method, string path, int topicId) => (method, path) switch
    {
        ("PATCH", _) => JsonContent.Create(new { name = "x" }),
        ("POST", "/topics") => JsonContent.Create(new { name = "x", parentId = topicId }),
        _ => null
    };

    private static async Task<IReadOnlyList<int>> ReadCollectionIdsAsync(
        HttpResponseMessage response,
        string path)
    {
        if (path.StartsWith("/topics/search", StringComparison.Ordinal))
        {
            var search = await response.Content.ReadFromJsonAsync<TopicSearchResult>();
            Assert.NotNull(search);
            return search.Items.Select(i => i.Id).ToList();
        }

        var list = await response.Content.ReadFromJsonAsync<TopicListResponse>();
        Assert.NotNull(list);
        return list.Items.Select(i => i.Id).ToList();
    }
}
