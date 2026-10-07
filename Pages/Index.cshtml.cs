using VoTrongNghia.Models;
using VoTrongNghia.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace VoTrongNghia.Pages;

public class IndexModel : PageModel
{
    private readonly SiteContent _content;

    public IndexModel(SiteContent content)
    {
        _content = content;
    }

    public SiteInfo Site { get; private set; } = new();
    public IReadOnlyList<Post> Posts { get; private set; } = [];
    public int PostCount { get; private set; }
    public IReadOnlyList<Course> Courses { get; private set; } = [];
    public IReadOnlyList<LiveSession> Sessions { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Site = await _content.Site.ReadAsync(cancellationToken);

        var posts = await _content.PublishedPostsAsync(cancellationToken);
        PostCount = posts.Count;
        Posts = posts.Take(6).ToList();

        Courses = await _content.PublishedCoursesAsync(cancellationToken);
        Sessions = (await _content.Live.ReadAsync(cancellationToken)).Upcoming(SiteTime.Today).ToList();
    }
}
