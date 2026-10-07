using VoTrongNghia.Models;

namespace VoTrongNghia.Services;

/// <summary>
/// Các file nội dung của site, mỗi file một <see cref="JsonFileStore{T}"/>.
/// Đăng ký singleton: nhớ đệm và khoá ghi phải dùng chung cho mọi request.
/// </summary>
public sealed class SiteContent
{
    public SiteContent(ContentPaths paths, ILoggerFactory loggers)
    {
        var logger = loggers.CreateLogger<SiteContent>();

        JsonFileStore<TItem> Store<TItem>(string file) where TItem : class, new() =>
            new(file, Path.Combine(paths.SeedDirectory, Path.GetFileName(file)), paths.BackupsDirectory, logger);

        Site = Store<SiteInfo>(paths.SiteFile);
        Posts = Store<List<Post>>(paths.PostsFile);
        Courses = Store<List<Course>>(paths.CoursesFile);
        Live = Store<LiveClass>(paths.LiveFile);
    }

    public JsonFileStore<SiteInfo> Site { get; }
    public JsonFileStore<List<Post>> Posts { get; }
    public JsonFileStore<List<Course>> Courses { get; }
    public JsonFileStore<LiveClass> Live { get; }

    /// <summary>Bài đã đăng, mới nhất trước.</summary>
    public async Task<List<Post>> PublishedPostsAsync(CancellationToken cancellationToken = default) =>
        (await Posts.ReadAsync(cancellationToken))
            .Where(post => post.Published)
            .OrderByDescending(post => post.PublishedAt)
            .ToList();

    /// <summary>Khóa đã đăng, theo cấp độ.</summary>
    public async Task<List<Course>> PublishedCoursesAsync(CancellationToken cancellationToken = default) =>
        (await Courses.ReadAsync(cancellationToken))
            .Where(course => course.Published)
            .OrderBy(course => course.Level)
            .ThenBy(course => course.Title, StringComparer.CurrentCulture)
            .ToList();
}
