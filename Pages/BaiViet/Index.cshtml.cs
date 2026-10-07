using VoTrongNghia.Models;
using VoTrongNghia.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace VoTrongNghia.Pages.BaiViet;

public class IndexModel : PageModel
{
    private readonly SiteContent _content;

    public IndexModel(SiteContent content)
    {
        _content = content;
    }

    public IReadOnlyList<Post> Posts { get; private set; } = [];
    public IReadOnlyList<string> Tags { get; private set; } = [];
    public int Total { get; private set; }

    /// <summary>Chủ đề đang lọc, trống là tất cả. Sai tên chủ đề thì coi như không lọc.</summary>
    public string Tag { get; private set; } = string.Empty;

    public async Task OnGetAsync(string? chuDe, CancellationToken cancellationToken)
    {
        var posts = await _content.PublishedPostsAsync(cancellationToken);

        Total = posts.Count;
        Tags = posts.SelectMany(post => post.Tags).Distinct().ToList();
        Tag = Tags.FirstOrDefault(tag => tag == chuDe) ?? string.Empty;
        Posts = Tag.Length == 0 ? posts : posts.Where(post => post.Tags.Contains(Tag)).ToList();
    }
}
