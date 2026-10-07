using VoTrongNghia.Models;
using VoTrongNghia.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace VoTrongNghia.Pages.KhoaHoc;

/// <summary>Một bài học: video (nếu có), nội dung, nút đánh dấu đã học, bài trước / sau.</summary>
public class BaiModel : PageModel
{
    private readonly SiteContent _content;

    public BaiModel(SiteContent content)
    {
        _content = content;
    }

    public Course Course { get; private set; } = new();
    public Lesson Lesson { get; private set; } = new();
    public int Index { get; private set; }
    public string BodyHtml { get; private set; } = string.Empty;
    public Lesson? Previous { get; private set; }
    public Lesson? Next { get; private set; }

    /// <summary>Khóa cấp độ kế tiếp, cho nút ở bài cuối.</summary>
    public Course? NextCourse { get; private set; }

    public bool IsPreview { get; private set; }

    public async Task<IActionResult> OnGetAsync(string khoa, string bai, CancellationToken cancellationToken)
    {
        var course = (await _content.Courses.ReadAsync(cancellationToken))
            .FirstOrDefault(item => string.Equals(item.Slug, khoa, StringComparison.OrdinalIgnoreCase));

        if (course is null || (!course.Published && User.Identity?.IsAuthenticated != true))
        {
            return NotFound();
        }

        var index = course.Lessons.FindIndex(item => string.Equals(item.Slug, bai, StringComparison.OrdinalIgnoreCase));

        if (index < 0)
        {
            return NotFound();
        }

        var lesson = course.Lessons[index];

        if (course.Slug != khoa || lesson.Slug != bai)
        {
            return RedirectPermanent(course.LessonUrl(lesson));
        }

        Course = course;
        Lesson = lesson;
        Index = index;
        IsPreview = !course.Published;
        BodyHtml = MarkdownRenderer.ToHtml(lesson.Body);
        Previous = index > 0 ? course.Lessons[index - 1] : null;
        Next = index < course.Lessons.Count - 1 ? course.Lessons[index + 1] : null;

        if (Next is null)
        {
            NextCourse = (await _content.PublishedCoursesAsync(cancellationToken))
                .FirstOrDefault(item => item.Level > course.Level);
        }

        return Page();
    }
}
