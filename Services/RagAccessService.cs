using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TaskManager.Auth;
using TaskManager.Configuration;
using TaskManager.Data;
using TaskManager.Errors;
using TaskManager.Logging;

namespace TaskManager.Services;

public class RagAccessService(
    AppDbContext db,
    IOptions<RagOptions> ragOptions,
    ILogger<RagAccessService> logger)
{
    public async Task<Result<int>> TryConsumeAsync(
        AuthenticatedUser user,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var options = ragOptions.Value;
        var dailyLimit = options.DailyLimitPerUser;

        if (dailyLimit < 1)
        {
            logger.LogWarning(
                RagLogMessages.QuotaMisconfigured,
                user.Id,
                dailyLimit,
                correlationId);
            return Result<int>.Fail(ErrorCodes.RagDailyLimitReached);
        }

        if (!IsPremiumUser(user.Username, options.PremiumUsernames))
        {
            logger.LogWarning(
                RagLogMessages.PremiumDenied,
                user.Id,
                user.Username,
                correlationId);
            return Result<int>.Fail(ErrorCodes.PremiumRequired);
        }

        if (IsUnlimitedUser(user.Username, options.UnlimitedUsernames))
        {
            logger.LogInformation(
                RagLogMessages.QuotaUnlimited,
                user.Id,
                user.Username,
                correlationId);
            return Result<int>.Success(-1);
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var newCounts = await db.Database
            .SqlQuery<int>($"""
                INSERT INTO "RagDailyUsages" ("UserId", "UsageDate", "RequestCount")
                VALUES ({user.Id}, {today}, 1)
                ON CONFLICT ("UserId", "UsageDate")
                DO UPDATE SET "RequestCount" = "RagDailyUsages"."RequestCount" + 1
                WHERE "RagDailyUsages"."RequestCount" < {dailyLimit}
                RETURNING "RequestCount"
                """)
            .ToListAsync(cancellationToken);

        if (newCounts.Count == 0)
        {
            logger.LogWarning(
                RagLogMessages.QuotaDenied,
                user.Id,
                dailyLimit,
                dailyLimit,
                correlationId);
            return Result<int>.Fail(ErrorCodes.RagDailyLimitReached);
        }

        var newCount = newCounts[0];
        var remaining = dailyLimit - newCount;

        logger.LogInformation(
            RagLogMessages.QuotaConsumed,
            user.Id,
            remaining,
            correlationId);

        return Result<int>.Success(remaining);
    }

    private static bool IsPremiumUser(string username, string[] premiumUsernames) =>
        premiumUsernames.Contains(username, StringComparer.OrdinalIgnoreCase);

    private static bool IsUnlimitedUser(string username, string[] unlimitedUsernames) =>
        unlimitedUsernames.Contains(username, StringComparer.OrdinalIgnoreCase);
}
