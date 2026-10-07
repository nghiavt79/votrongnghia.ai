namespace VoTrongNghia.Models;

/// <summary>Một bài viết, <c>Data/posts.json</c>. Trang công khai: <c>/bai-viet/{slug}</c>.</summary>
public sealed class Post
{
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;

    /// <summary>Hiện ở thẻ bài, dưới tiêu đề và làm thẻ meta description, 120–200 ký tự là vừa.</summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>Thân bài, viết bằng Markdown. Máy chủ dựng ra HTML đã lọc.</summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// Ảnh bìa 1200×630, đường dẫn tương đối không gạch chéo đầu (<c>uploads/bai-viet/…</c>).
    /// Hiện ở thẻ bài, đầu bài, và là ảnh khi chia sẻ link lên Facebook / Zalo.
    /// </summary>
    public string Cover { get; set; } = string.Empty;
    public string CoverAlt { get; set; } = string.Empty;

    /// <summary>Ảnh đã tải lên để chèn vào thân bài, giữ lại để lấy lại đoạn mã chèn và để dọn khi xoá bài.</summary>
    public List<string> BodyImages { get; set; } = [];

    /// <summary>Chủ đề, dùng để lọc ở trang tất cả bài viết.</summary>
    public List<string> Tags { get; set; } = [];

    /// <summary>Chưa đăng thì khách không thấy, không vào trang chủ, không vào sitemap.</summary>
    public bool Published { get; set; }

    /// <summary>Ngày hiện trên bài và dùng để xếp mới trước.</summary>
    public DateTimeOffset? PublishedAt { get; set; }

    /// <summary>
    /// Lần đầu bài lên site. Có mốc này rồi thì đường dẫn khoá lại, kể cả khi bài bị gỡ
    /// về nháp: link có thể đã được chia sẻ hoặc đã vào Google.
    /// </summary>
    public DateTimeOffset? FirstPublishedAt { get; set; }

    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public string PublicUrl => "/bai-viet/" + Slug;
}
