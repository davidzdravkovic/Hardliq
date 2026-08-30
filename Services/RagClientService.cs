using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using TaskManager.Configuration;
using TaskManager.Errors;
using TaskManager.Logging;

namespace TaskManager.Services;

public class RagClientService(
    HttpClient httpClient,
    IOptions<RagOptions> ragOptions,
    ILogger<RagClientService> logger)
{
    public async Task<Result<RagAskResult>> AskAsync(
        int userId,
        string question,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var request = new RagAskRequestDto(userId, question);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "internal/ask")
        {
            Content = JsonContent.Create(request)
        };

        var internalKey = ragOptions.Value.InternalKey;
        if (!string.IsNullOrWhiteSpace(internalKey))
            httpRequest.Headers.Add("X-Internal-Key", internalKey);

        httpRequest.Headers.Add("X-Correlation-Id", correlationId);

        var stopwatch = Stopwatch.StartNew();

        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(httpRequest, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogError(
                ex,
                RagLogMessages.PythonFailure,
                userId,
                correlationId,
                0);
            return Result<RagAskResult>.Fail(ErrorCodes.RagServiceUnavailable);
        }

        stopwatch.Stop();

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError(
                RagLogMessages.PythonFailure,
                userId,
                correlationId,
                (int)response.StatusCode);
            return Result<RagAskResult>.Fail(ErrorCodes.RagServiceUnavailable);
        }

        var body = await response.Content.ReadFromJsonAsync<RagAskResponseDto>(cancellationToken);
        if (body is null || string.IsNullOrWhiteSpace(body.Answer))
        {
            logger.LogError(
                RagLogMessages.PythonFailure,
                userId,
                correlationId,
                (int)response.StatusCode);
            return Result<RagAskResult>.Fail(ErrorCodes.RagServiceUnavailable);
        }

        var sources = body.Sources?
            .Select(s => new RagAskSource(s.TopicId, s.Name))
            .ToList() ?? [];

        logger.LogInformation(
            RagLogMessages.PythonResponse,
            userId,
            stopwatch.ElapsedMilliseconds,
            sources.Count,
            correlationId);

        return Result<RagAskResult>.Success(new RagAskResult(body.Answer, sources));
    }

    private sealed record RagAskRequestDto(int UserId, string Question);

    private sealed class RagAskResponseDto
    {
        public string Answer { get; init; } = string.Empty;

        public List<RagAskSourceDto>? Sources { get; init; }
    }

    private sealed class RagAskSourceDto
    {
        [JsonPropertyName("topicId")]
        public int TopicId { get; init; }

        public string Name { get; init; } = string.Empty;
    }
}

public sealed record RagAskResult(
    string Answer,
    IReadOnlyList<RagAskSource> Sources);

public sealed record RagAskSource(int TopicId, string Name);
