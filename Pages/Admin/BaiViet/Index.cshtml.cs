using VoTrongNghia.Models;
using VoTrongNghia.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace VoTrongNghia.Pages.Admin.BaiViet;

public class IndexModel : PageModel
{
    private readonly SiteContent _content;

    public IndexModel(SiteContent content)
    {
        _content = content;
    }

    public IReadOnlyList<Post> Posts { get; private set; } = [];

    [TempData]
    public string? Message { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        // Nháp lên đầu (đang viết dở), rồi bài mới nhất.
        Posts = (await _content.Posts.ReadAsync(cancellationToken))
            .OrderBy(post => post.Published)
            .ThenByDescending(post => post.PublishedAt)
            .ToList();
    }
}
