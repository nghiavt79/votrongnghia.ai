using VoTrongNghia.Models;
using VoTrongNghia.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace VoTrongNghia.Pages.Admin.KhoaHoc;

/// <summary>Sửa thông tin một khóa và sắp xếp / xoá bài. Nội dung từng bài soạn ở trang Bai.</summary>
public class SuaModel : PageModel
{
    private readonly SiteContent _content;
    private readonly ILogger<SuaModel> _logger;

    public SuaModel(SiteContent content, ILogger<SuaModel> logger)
    {
        _content = content;
        _logger = logger;
    }

    public Course Course { get; private set; } = new();

    /// <summary>Khóa này đang là bài đầu vào của form đăng ký.</summary>
    public bool IsRequired { get; private set; }

    [BindProperty]
    public CourseInput Input { get; set; } = new();

    [TempData]
    public string? Message { get; set; }

    public sealed class CourseInput
    {
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public int Level { get; set; } = 1;
        public string Summary { get; set; } = string.Empty;
        public bool Published { get; set; }
    }

    // [FromRoute]: tránh bẫy Dokma đã dính — form có ô "Slug" thì tham số route trùng tên nhận nhầm giá trị form.
    public async Task<IActionResult> OnGetAsync([FromRoute] string slug, CancellationToken cancellationToken)
    {
        if (!await LoadAsync(slug, cancellationToken))
        {
            return NotFound();
        }

        Input = new CourseInput
        {
            Title = Course.Title,
            Slug = Course.Slug,
            Level = Course.Level,
            Summary = Course.Summary,
            Published = Course.Published
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync([FromRoute] string slug, CancellationToken cancellationToken)
    {
        if (!await LoadAsync(slug, cancellationToken))
        {
            return NotFound();
        }

        var title = Input.Title.Trim();
        // Khóa đã đăng thì khoá đường dẫn: link bài học có thể đã được chia sẻ.
        var targetSlug = Course.Published ? Course.Slug : Input.Slug.Trim().ToLowerInvariant();
        var courses = await _content.Courses.ReadAsync(cancellationToken);

        if (title.Length == 0)
        {
            ModelState.AddModelError("Input.Title", "Nhập tên khóa.");
        }

        if (!Slugs.Pattern().IsMatch(targetSlug))
        {
            ModelState.AddModelError("Input.Slug", "Đường dẫn chỉ gồm chữ thường không dấu, số và dấu gạch ngang.");
        }
        else if (courses.Any(item => item.Slug == targetSlug && item.Slug != Course.Slug))
        {
            ModelState.AddModelError("Input.Slug", "Đường dẫn này đã có khóa khác dùng.");
        }

        if (Input.Level is < 1 or > 20)
        {
            ModelState.AddModelError("Input.Level", "Cấp độ từ 1 đến 20.");
        }

        if (Input.Published)
        {
            if (Course.Lessons.Count == 0)
            {
                ModelState.AddModelError("Input.Published", "Khóa chưa có bài nào — thêm bài trước rồi mới đăng.");
            }
            else if (Course.Lessons.FirstOrDefault(lesson => lesson.Body.Trim().Length < 100) is { } thin)
            {
                ModelState.AddModelError("Input.Published", $"Bài \"{thin.Title}\" còn quá ngắn (dưới 100 ký tự). Viết xong rồi mới đăng khóa.");
            }

            if (Input.Summary.Trim().Length == 0)
            {
                ModelState.AddModelError("Input.Summary", "Viết một câu giới thiệu khóa trước khi đăng.");
            }
        }

        if (!Input.Published && Course.Published && IsRequired)
        {
            ModelState.AddModelError("Input.Published",
                "Khóa này đang là bài đầu vào của form đăng ký. Gỡ khóa thì form mở cho mọi người — đổi bài đầu vào ở Lịch & điều kiện trước.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var saved = await _content.Courses.UpdateAsync(list =>
        {
            var course = list.FirstOrDefault(item => item.Slug == slug);

            if (course is null || list.Any(item => item.Slug == targetSlug && item != course))
            {
                return false;
            }

            course.Title = title;
            course.Slug = targetSlug;
            course.Level = Input.Level;
            course.Summary = Input.Summary.Trim();
            course.Published = Input.Published;
            course.UpdatedAt = SiteTime.Now;
            return true;
        }, cancellationToken);

        if (!saved)
        {
            ModelState.AddModelError(string.Empty, "Không lưu được — khóa vừa bị xoá hoặc đổi đường dẫn ở tab khác.");
            return Page();
        }

        Message = "Đã lưu khóa học.";
        return RedirectToPage(new { slug = targetSlug });
    }

    /// <summary>Đổi chỗ bài: <paramref name="huong"/> là -1 (lên) hoặc 1 (xuống).</summary>
    public async Task<IActionResult> OnPostDoiChoAsync([FromRoute] string slug, string bai, int huong, CancellationToken cancellationToken)
    {
        await _content.Courses.UpdateAsync(list =>
        {
            var course = list.FirstOrDefault(item => item.Slug == slug);
            var index = course?.Lessons.FindIndex(lesson => lesson.Slug == bai) ?? -1;
            var target = index + Math.Sign(huong);

            if (course is null || index < 0 || target < 0 || target >= course.Lessons.Count)
            {
                return false;
            }

            (course.Lessons[index], course.Lessons[target]) = (course.Lessons[target], course.Lessons[index]);
            course.UpdatedAt = SiteTime.Now;
            return true;
        }, cancellationToken);

        return RedirectToPage(new { slug });
    }

    public async Task<IActionResult> OnPostXoaKhoaAsync([FromRoute] string slug, string? confirm, CancellationToken cancellationToken)
    {
        if (!await LoadAsync(slug, cancellationToken))
        {
            return NotFound();
        }

        ModelState.Clear();

        if (IsRequired)
        {
            ModelState.AddModelError(string.Empty, "Khóa này đang là bài đầu vào của form đăng ký. Đổi bài đầu vào ở Lịch & điều kiện trước.");
        }
        else if (!string.Equals(confirm?.Trim(), slug, StringComparison.Ordinal))
        {
            ModelState.AddModelError(string.Empty, $"Muốn xoá thì gõ đúng \"{slug}\" vào ô xác nhận.");
        }

        if (!ModelState.IsValid)
        {
            Input = new CourseInput { Title = Course.Title, Slug = Course.Slug, Level = Course.Level, Summary = Course.Summary, Published = Course.Published };
            return Page();
        }

        await _content.Courses.UpdateAsync(list => list.RemoveAll(item => item.Slug == slug) > 0, cancellationToken);
        _logger.LogWarning("Đã xoá khóa {Slug}", slug);
        TempData["Message"] = $"Đã xoá khóa \"{Course.Title}\".";

        return RedirectToPage("/Admin/KhoaHoc/Index");
    }

    private async Task<bool> LoadAsync(string slug, CancellationToken cancellationToken)
    {
        var course = (await _content.Courses.ReadAsync(cancellationToken)).FirstOrDefault(item => item.Slug == slug);

        if (course is null)
        {
            return false;
        }

        Course = course;
        IsRequired = (await _content.Live.ReadAsync(cancellationToken)).RequireCourse == slug;
        return true;
    }
}
