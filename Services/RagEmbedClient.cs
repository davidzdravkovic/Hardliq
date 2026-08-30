using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using TaskManager.Configuration;
using TaskManager.Logging;

namespace TaskManager.Services;

public class RagEmbedClient(
    HttpClient httpClient,
    IOptions<RagOptions> ragOptions,
    ILogger<RagEmbedClient> logger)
{
    public void RequestEmbed(int topicId)
    {
        if (topicId < 1)
            return;

        _ = SendEmbedAsync(topicId);
    }

    public void RequestEmbedMany(IEnumerable<int> topicIds)
    {
        foreach (var topicId in topicIds.Distinct())
            RequestEmbed(topicId);
    }

    private async Task SendEmbedAsync(int topicId)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "internal/embed")
            {
                Content = JsonContent.Create(new RagEmbedRequestDto(topicId))
            };

            var internalKey = ragOptions.Value.InternalKey;
            if (!string.IsNullOrWhiteSpace(internalKey))
                request.Headers.Add("X-Internal-Key", internalKey);

            var response = await httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation(RagLogMessages.EmbedRequested, topicId, (int)response.StatusCode);
                return;
            }

            logger.LogWarning(
                RagLogMessages.EmbedFailure,
                topicId,
                (int)response.StatusCode);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, RagLogMessages.EmbedFailure, topicId, 0);
        }
    }

    private sealed record RagEmbedRequestDto(
        [property: JsonPropertyName("topicId")] int TopicId);
}
