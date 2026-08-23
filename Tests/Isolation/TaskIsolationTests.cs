using System.Net;
using System.Net.Http.Json;
using Xunit;
using Hardliq.Api.Tests;

namespace Hardliq.Api.Tests.Isolation;

[Collection(nameof(HardliqApiCollection))]
public sealed class TaskIsolationTests
{
    private readonly HttpClient _client;
    private readonly IsolationSupport _support;

    public TaskIsolationTests(HardliqApiFactory factory)
    {
        _client = factory.CreateClient();
        _support = new IsolationSupport(_client);
    }

    [Theory]
    [InlineData("POST", "/topics/{folderId}/tasks")]
    [InlineData("PUT", "/topics/{taskId}/task")]
    [InlineData("PATCH", "/topics/{taskId}/task")]
    [InlineData("DELETE", "/topics/{taskId}/task")]
    public async Task User_cannot_access_another_users_task(string method, string path)
    {
        var (_, tokenB, folder, task) = await _support.CreateTaskOwnedByOtherUserAsync();

        _support.Authenticate(tokenB);

        var url = path
            .Replace("{folderId}", folder.Id.ToString())
            .Replace("{taskId}", task.Id.ToString());
        using var request = new HttpRequestMessage(new HttpMethod(method), url);
        request.Content = BodyFor(method);

        var asB = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, asB.StatusCode);
    }

    private static HttpContent? BodyFor(string method) => method switch
    {
        "POST" => JsonContent.Create(new { name = "x", description = "x" }),
        "PUT" => JsonContent.Create(new { description = "x", status = "Completed" }),
        "PATCH" => JsonContent.Create(new { description = "x" }),
        _ => null
    };
}
