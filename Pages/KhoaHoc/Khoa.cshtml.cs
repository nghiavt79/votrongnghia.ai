using VoTrongNghia.Models;
using VoTrongNghia.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace VoTrongNghia.Pages.KhoaHoc;

/// <summary>Trang tổng quan một khóa: mô tả, danh sách bài, nút bắt đầu / học tiếp.</summary>
public class KhoaModel : PageModel
{
    private readonly SiteContent _content;

    public KhoaModel(SiteContent content)
    {
        _content = content;
    }

    public Course Course { get; private set; } = new();
    public IReadOnlyList<Course> Others { get; private set; } = [];
    public bool IsPreview { get; private set; }

    public async Task<IActionResult> OnGetAsync(string khoa, CancellationToken cancellationToken)
    {
        var course = (await _content.Courses.ReadAsync(cancellationToken))
            .FirstOrDefault(item => string.Equals(item.Slug, khoa, StringComparison.OrdinalIgnoreCase));

        // Khóa nháp: như bài nháp, chỉ người đăng nhập /cms xem được.
        if (course is null || (!course.Published && User.Identity?.IsAuthenticated != true))
        {
            return NotFound();
        }

        if (course.Slug != khoa)
        {
            return RedirectPermanent(course.PublicUrl);
        }

        Course = course;
        IsPreview = !course.Published;
        Others = (await _content.PublishedCoursesAsync(cancellationToken)).Where(item => item.Slug != course.Slug).ToList();

        return Page();
    }
}
