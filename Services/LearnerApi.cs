using VoTrongNghia.Models;

namespace VoTrongNghia.Services;

/// <summary>
/// API ghi tiến độ của học viên đã đăng nhập (khoa-hoc.js và site.js gọi). Người học tự do không
/// dùng tới — tiến độ của họ vẫn chỉ nằm trên trình duyệt.
///
/// <para>Đi bằng cookie học viên nên mỗi request phải có: JSON (form của site khác không gửi được
/// kiểu này mà không qua CORS), mã chống giả ở header (<see cref="LearnerSession.RequestToken"/>), và
/// chỉ ghi bài có thật trong khóa đã đăng. Phản hồi lỗi luôn kèm thân JSON — phản hồi lỗi không thân
/// bị UseStatusCodePages thay bằng trang 404.</para>
/// </summary>
public static class LearnerApi
{
    private sealed record ProgressRequest(string? Khoa, string? Bai, bool Xong);

    private sealed record MergeRequest(List<string>? Bai);

    public static void MapLearnerApi(this WebApplication app)
    {
        var api = app.MapGroup("/api/hoc-vien")
            .RequireRateLimiting("hoc-vien-api")
            .WithMetadata(new Microsoft.AspNetCore.Mvc.RequestSizeLimitAttribute(16 * 1024));

        // { "khoa": "...", "bai": "...", "xong": true | false }
        api.MapPost("/tien-do", async (HttpContext context, LearnerSession session, LearnerStore learners, SiteContent content) =>
        {
            var (error, learner) = await CheckAsync(context, session);

            if (error is not null)
            {
                return error;
            }

            var request = await ReadAsync<ProgressRequest>(context);
            var lessons = await PublishedLessonKeysAsync(content, context.RequestAborted);
            var key = $"{request?.Khoa}/{request?.Bai}";

            if (request is null || !lessons.Contains(key))
            {
                return Results.BadRequest(new { loi = "Không có bài này." });
            }

            await learners.SetProgressAsync(learner!.Id, key, request.Xong, context.RequestAborted);
            return Results.NoContent();
        });

        // Gộp tiến độ trên máy lên tài khoản: { "bai": ["khoa/bai", ...] } → { "tienDo": [...] }.
        api.MapPost("/gop-tien-do", async (HttpContext context, LearnerSession session, LearnerStore learners, SiteContent content) =>
        {
            var (error, learner) = await CheckAsync(context, session);

            if (error is not null)
            {
                return error;
            }

            var request = await ReadAsync<MergeRequest>(context);
            var lessons = await PublishedLessonKeysAsync(content, context.RequestAborted);
            var keys = (request?.Bai ?? []).Where(lessons.Contains).Distinct().Take(500);

            var progress = await learners.MergeProgressAsync(learner!.Id, keys, context.RequestAborted);
            return Results.Json(new { tienDo = progress });
        });
    }

    /// <summary>Kiểm chung: JSON, đã đăng nhập, đúng mã chống giả. Có lỗi thì trả về phản hồi lỗi, qua thì trả về học viên.</summary>
    private static async Task<(IResult? Error, Learner? Learner)> CheckAsync(HttpContext context, LearnerSession session)
    {
        if (!context.Request.HasJsonContentType())
        {
            return (Results.Json(new { loi = "Chỉ nhận JSON." }, statusCode: StatusCodes.Status415UnsupportedMediaType), null);
        }

        var learner = await session.CurrentAsync(context);

        if (learner is null)
        {
            return (Results.Json(new { loi = "Phiên học viên đã hết, đăng nhập lại nhé." }, statusCode: StatusCodes.Status401Unauthorized), null);
        }

        if (!session.IsValidRequest(context, learner))
        {
            return (Results.Json(new { loi = "Mã xác nhận không đúng, tải lại trang rồi thử lại." }, statusCode: StatusCodes.Status403Forbidden), null);
        }

        return (null, learner);
    }

    private static async Task<T?> ReadAsync<T>(HttpContext context) where T : class
    {
        try
        {
            return await context.Request.ReadFromJsonAsync<T>(context.RequestAborted);
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
    }

    private static async Task<HashSet<string>> PublishedLessonKeysAsync(SiteContent content, CancellationToken cancellationToken) =>
        (await content.PublishedCoursesAsync(cancellationToken))
            .SelectMany(course => course.Lessons.Select(lesson => $"{course.Slug}/{lesson.Slug}"))
            .ToHashSet(StringComparer.Ordinal);
}
