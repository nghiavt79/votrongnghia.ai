using System.Text.RegularExpressions;
using VoTrongNghia.Models;
using VoTrongNghia.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace VoTrongNghia.Pages.Admin.KhoaHoc;

/// <summary>Soạn một bài học trong khóa: tên, thời lượng, video, nội dung Markdown.</summary>
public partial class BaiModel : PageModel
{
    private readonly SiteContent _content;
    private readonly ILogger<BaiModel> _logger;

    public BaiModel(SiteContent content, ILogger<BaiModel> logger)
    {
        _content = content;
        _logger = logger;
    }

    public Course Course { get; private set; } = new();

    /// <summary>Bài đang sửa; null khi thêm bài mới.</summary>
    public Lesson? Lesson { get; private set; }

    public bool IsNew => Lesson is null;

    /// <summary>Khóa đã đăng thì đường dẫn bài cũ khoá lại, như bài viết.</summary>
    public bool SlugLocked => !IsNew && Course.Published;

    public string? PreviewHtml { get; private set; }

    [BindProperty]
    public LessonInput Input { get; set; } = new();

    [TempData]
    public string? Message { get; set; }

    public sealed class LessonInput
    {
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public int Minutes { get; set; } = 5;

        /// <summary>Mã video hoặc nguyên link YouTube — máy tự lấy mã.</summary>
        public string Video { get; set; } = string.Empty;

        public string Body { get; set; } = string.Empty;
    }

    [GeneratedRegex(@"(?:v=|youtu\.be/|embed/|shorts/)([A-Za-z0-9_-]{11})")]
    private static partial Regex YoutubeLink();

    [GeneratedRegex(@"^[A-Za-z0-9_-]{11}$")]
    private static partial Regex YoutubeId();

    public async Task<IActionResult> OnGetAsync(string khoa, string? bai, CancellationToken cancellationToken)
    {
        if (!await LoadAsync(khoa, bai, cancellationToken))
        {
            return NotFound();
        }

        if (Lesson is not null)
        {
            Input = new LessonInput { Title = Lesson.Title, Slug = Lesson.Slug, Minutes = Lesson.Minutes, Video = Lesson.Video, Body = Lesson.Body };
        }

        return Page();
    }

    public async Task<IActionResult> OnPostXemTruocAsync(string khoa, string? bai, CancellationToken cancellationToken)
    {
        if (!await LoadAsync(khoa, bai, cancellationToken))
        {
            return NotFound();
        }

        ModelState.Clear();
        PreviewHtml = MarkdownRenderer.ToHtml(Input.Body);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string khoa, string? bai, CancellationToken cancellationToken)
    {
        if (!await LoadAsync(khoa, bai, cancellationToken))
        {
            return NotFound();
        }

        var title = Input.Title.Trim();
        var targetSlug = SlugLocked ? Lesson!.Slug : Input.Slug.Trim().ToLowerInvariant();

        if (targetSlug.Length == 0)
        {
            targetSlug = Slugs.From(title);
        }

        var video = Input.Video.Trim();

        if (YoutubeLink().Match(video) is { Success: true } match)
        {
            video = match.Groups[1].Value;
        }

        if (title.Length == 0)
        {
            ModelState.AddModelError("Input.Title", "Nhập tên bài.");
        }

        if (!Slugs.Pattern().IsMatch(targetSlug))
        {
            ModelState.AddModelError("Input.Slug", "Đường dẫn chỉ gồm chữ thường không dấu, số và dấu gạch ngang.");
        }
        else if (Course.Lessons.Any(item => item.Slug == targetSlug && item != Lesson))
        {
            ModelState.AddModelError("Input.Slug", "Trong khóa đã có bài dùng đường dẫn này.");
        }

        if (Input.Minutes is < 1 or > 300)
        {
            ModelState.AddModelError("Input.Minutes", "Thời lượng từ 1 đến 300 phút.");
        }

        if (video.Length > 0 && !YoutubeId().IsMatch(video))
        {
            ModelState.AddModelError("Input.Video", "Không đọc được mã video. Dán link YouTube (youtube.com/watch?v=… hoặc youtu.be/…) hoặc mã 11 ký tự.");
        }

        // Khóa đang hiện trên site: bài thêm vào hay sửa đi là hiện ngay, nên giữ luật như lúc đăng khóa.
        if (Course.Published && Input.Body.Trim().Length < 100)
        {
            ModelState.AddModelError("Input.Body", "Khóa đang hiện trên site, bài cần ít nhất 100 ký tự. Gỡ khóa về nháp nếu muốn soạn dần.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var originalSlug = Lesson?.Slug;
        var saved = await _content.Courses.UpdateAsync(list =>
        {
            var course = list.FirstOrDefault(item => item.Slug == khoa);

            if (course is null || course.Lessons.Any(item => item.Slug == targetSlug && item.Slug != originalSlug))
            {
                return false;
            }

            var lesson = originalSlug is null ? new Lesson() : course.Lessons.FirstOrDefault(item => item.Slug == originalSlug);

            if (lesson is null)
            {
                return false;
            }

            lesson.Title = title;
            lesson.Slug = targetSlug;
            lesson.Minutes = Input.Minutes;
            lesson.Video = video;
            lesson.Body = Input.Body.Replace("\r\n", "\n").Trim();

            if (originalSlug is null)
            {
                course.Lessons.Add(lesson);
            }

            course.UpdatedAt = SiteTime.Now;
            return true;
        }, cancellationToken);

        if (!saved)
        {
            ModelState.AddModelError(string.Empty, "Không lưu được — bài hoặc khóa vừa bị đổi ở tab khác.");
            return Page();
        }

        Message = IsNew ? "Đã thêm bài vào cuối khóa." : "Đã lưu bài.";
        return RedirectToPage(new { khoa, bai = targetSlug });
    }

    public async Task<IActionResult> OnPostXoaAsync(string khoa, string bai, CancellationToken cancellationToken)
    {
        if (!await LoadAsync(khoa, bai, cancellationToken) || Lesson is null)
        {
            return NotFound();
        }

        if (Course.Published && Course.Lessons.Count == 1)
        {
            ModelState.AddModelError(string.Empty, "Đây là bài duy nhất của một khóa đang hiện trên site. Gỡ khóa về nháp trước.");
            Input = new LessonInput { Title = Lesson.Title, Slug = Lesson.Slug, Minutes = Lesson.Minutes, Video = Lesson.Video, Body = Lesson.Body };
            return Page();
        }

        await _content.Courses.UpdateAsync(list =>
        {
            var course = list.FirstOrDefault(item => item.Slug == khoa);
            return course is not null && course.Lessons.RemoveAll(item => item.Slug == bai) > 0;
        }, cancellationToken);

        _logger.LogWarning("Đã xoá bài {Khoa}/{Bai}", khoa, bai);
        TempData["Message"] = $"Đã xoá bài \"{Lesson.Title}\".";

        return RedirectToPage("/Admin/KhoaHoc/Sua", new { slug = khoa });
    }

    private async Task<bool> LoadAsync(string khoa, string? bai, CancellationToken cancellationToken)
    {
        var course = (await _content.Courses.ReadAsync(cancellationToken)).FirstOrDefault(item => item.Slug == khoa);

        if (course is null)
        {
            return false;
        }

        Course = course;

        if (bai is null)
        {
            return true;
        }

        Lesson = course.Lessons.FirstOrDefault(item => item.Slug == bai);
        return Lesson is not null;
    }
}
