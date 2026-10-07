using VoTrongNghia.Models;
using VoTrongNghia.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace VoTrongNghia.Pages.KhoaHoc;

public class IndexModel : PageModel
{
    private readonly SiteContent _content;

    public IndexModel(SiteContent content)
    {
        _content = content;
    }

    public IReadOnlyList<Course> Courses { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Courses = await _content.PublishedCoursesAsync(cancellationToken);
    }
}
