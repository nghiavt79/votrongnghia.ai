using System.Text.Json;
using VoTrongNghia.Models;
using VoTrongNghia.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace VoTrongNghia.Pages.BaiViet;

public class ChiTietModel : PageModel
{
    private readonly SiteContent _content;
    private readonly IConfiguration _configuration;

    public ChiTietModel(SiteContent content, IConfiguration configuration)
    {
        _content = content;
        _configuration = configuration;
    }

    public Post Post { get; private set; } = new();
    public SiteInfo Site { get; private set; } = new();
    public MarkdownRenderer.Rendered Body { get; private set; } = new(string.Empty, []);
    public string Description { get; private set; } = string.Empty;

    /// <summary>Bài mới hơn / cũ hơn trong danh sách đã đăng.</summary>
    public Post? Newer { get; private set; }
    public Post? Older { get; private set; }

    public IReadOnlyList<Post> Related { get; private set; } = [];

    /// <summary>Bài chưa đăng, đang được người quản trị xem trước.</summary>
    public bool IsPreview { get; private set; }

    public string JsonLd { get; private set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync(string slug, CancellationToken cancellationToken)
    {
        var posts = await _content.Posts.ReadAsync(cancellationToken);
        var post = posts.FirstOrDefault(item => string.Equals(item.Slug, slug, StringComparison.OrdinalIgnoreCase));

        // Bài nháp: khách nhận 404 như bài không tồn tại. Người đã đăng nhập /cms thì xem
        // được, kèm băng "bản nháp" — đó là nút "Xem trước" của trang sửa bài.
        if (post is null || (!post.Published && User.Identity?.IsAuthenticated != true))
        {
            return NotFound();
        }

        // Đường dẫn viết hoa hay sai hoa thường thì đưa về đúng một bản.
        if (post.Slug != slug)
        {
            return RedirectPermanent(post.PublicUrl);
        }

        Post = post;
        Site = await _content.Site.ReadAsync(cancellationToken);
        IsPreview = !post.Published;
        Body = MarkdownRenderer.Render(post.Body);
        Description = post.Summary.Length > 0 ? post.Summary : MarkdownRenderer.ToPlainText(post.Body, 160);

        var published = await _content.PublishedPostsAsync(cancellationToken);
        var index = published.FindIndex(item => item.Slug == post.Slug);

        if (index >= 0)
        {
            Newer = index > 0 ? published[index - 1] : null;
            Older = index < published.Count - 1 ? published[index + 1] : null;
        }

        // Liên quan: cùng chủ đề trước, thiếu thì lấy bài mới nhất bù vào.
        Related = published
            .Where(item => item.Slug != post.Slug)
            .OrderByDescending(item => item.Tags.Intersect(post.Tags).Count())
            .ThenByDescending(item => item.PublishedAt)
            .Take(3)
            .ToList();

        var baseUrl = (_configuration["Site:BaseUrl"] ?? $"{Request.Scheme}://{Request.Host}").TrimEnd('/');

        // BlogPosting cho Google. Serialize bằng JsonSerializer để dấu ngoặc kép trong tiêu
        // đề không làm vỡ khối JSON; Dictionary chứ không kiểu vô danh để giữ được chữ "@".
        JsonLd = JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "BlogPosting",
            ["headline"] = post.Title,
            ["description"] = Description,
            ["datePublished"] = post.PublishedAt?.ToString("yyyy-MM-dd'T'HH:mm:sszzz"),
            ["dateModified"] = (post.UpdatedAt ?? post.PublishedAt)?.ToString("yyyy-MM-dd'T'HH:mm:sszzz"),
            ["mainEntityOfPage"] = $"{baseUrl}{post.PublicUrl}",
            ["keywords"] = post.Tags.Count > 0 ? string.Join(", ", post.Tags) : null,
            ["author"] = new Dictionary<string, object> { ["@type"] = "Person", ["name"] = Site.Name, ["url"] = baseUrl + "/" },
            ["publisher"] = new Dictionary<string, object> { ["@type"] = "Person", ["name"] = Site.Name }
        }, new JsonSerializerOptions { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull })
            // Chặn "</script>" trong tiêu đề đóng sớm khối script.
            .Replace("</", "<\\/");

        return Page();
    }
}
