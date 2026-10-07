using VoTrongNghia.Models;
using VoTrongNghia.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace VoTrongNghia.Pages.Admin.KhoaHoc;

public class IndexModel : PageModel
{
    private readonly SiteContent _content;

    public IndexModel(SiteContent content)
    {
        _content = content;
    }

    public IReadOnlyList<Course> Courses { get; private set; } = [];

    /// <summary>Khóa đang làm bài đầu vào cho form đăng ký, để đánh dấu trong bảng.</summary>
    public string RequiredCourse { get; private set; } = string.Empty;

    [BindProperty]
    public string NewTitle { get; set; } = string.Empty;

    [TempData]
    public string? Message { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken) => await LoadAsync(cancellationToken);

    /// <summary>Tạo khóa mới (nháp, chưa có bài) rồi sang trang sửa để thêm bài.</summary>
    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var title = NewTitle.Trim();
        var slug = Slugs.From(title);

        if (title.Length == 0 || slug.Length == 0)
        {
            ModelState.AddModelError(nameof(NewTitle), "Nhập tên khóa học.");
            await LoadAsync(cancellationToken);
            return Page();
        }

        var saved = await _content.Courses.UpdateAsync(list =>
        {
            if (list.Any(item => item.Slug == slug))
            {
                return false;
            }

            list.Add(new Course
            {
                Slug = slug,
                Title = title,
                Level = list.Count == 0 ? 1 : list.Max(item => item.Level) + 1,
                UpdatedAt = SiteTime.Now
            });
            return true;
        }, cancellationToken);

        if (!saved)
        {
            ModelState.AddModelError(nameof(NewTitle), $"Đã có khóa dùng đường dẫn \"{slug}\". Đặt tên khác.");
            await LoadAsync(cancellationToken);
            return Page();
        }

        Message = "Đã tạo khóa (bản nháp). Thêm bài học rồi bật \"Đăng lên site\".";
        return RedirectToPage("/Admin/KhoaHoc/Sua", new { slug });
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        Courses = (await _content.Courses.ReadAsync(cancellationToken)).OrderBy(course => course.Level).ToList();
        RequiredCourse = (await _content.Live.ReadAsync(cancellationToken)).RequireCourse;
    }
}
