using System.Text.RegularExpressions;
using VoTrongNghia.Models;
using VoTrongNghia.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace VoTrongNghia.Pages.Admin.KhoaHoc;

/// <summary>Soạn một bài học trong khóa: tên, thời lượng, video, nội dung Markdown, ảnh.</summary>
[RequestSizeLimit(64 * 1024 * 1024)]
public partial class BaiModel : PageModel
{
    private readonly SiteContent _content;
    private readonly ImageService _images;
    private readonly ILogger<BaiModel> _logger;

    public BaiModel(SiteContent content, ImageService images, ILogger<BaiModel> logger)
    {
        _content = content;
        _images = images;
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

        /// <summary>Mỗi dòng một kết quả.</summary>
        public string Outcomes { get; set; } = string.Empty;

        public string Exercise { get; set; } = string.Empty;
        public string NextStep { get; set; } = string.Empty;
    }

    private static List<string> Lines(string text) =>
        text.Replace("\r\n", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line => line.TrimStart('-', '*', '•', ' '))
            .Where(line => line.Length > 0)
            .Take(6)
            .ToList();

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
            FillInput(Lesson);
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
        PreviewHtml = MarkdownRenderer.ToHtml(Input.Body) +
                      (Input.Exercise.Trim().Length > 0 ? "<hr><h3>🛠️ Bài tập thực hành</h3>" + MarkdownRenderer.ToHtml(Input.Exercise) : string.Empty);
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
            lesson.Outcomes = Lines(Input.Outcomes);
            lesson.Exercise = Input.Exercise.Replace("\r\n", "\n").Trim();
            lesson.NextStep = Input.NextStep.Trim();

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
            FillInput(Lesson);
            return Page();
        }

        await _content.Courses.UpdateAsync(list =>
        {
            var course = list.FirstOrDefault(item => item.Slug == khoa);
            return course is not null && course.Lessons.RemoveAll(item => item.Slug == bai) > 0;
        }, cancellationToken);
        _images.Delete(Lesson.Images);

        _logger.LogWarning("Đã xoá bài {Khoa}/{Bai}", khoa, bai);
        TempData["Message"] = $"Đã xoá bài \"{Lesson.Title}\".";

        return RedirectToPage("/Admin/KhoaHoc/Sua", new { slug = khoa });
    }

    /// <summary>Tải ảnh để chèn vào bài. Bài phải lưu rồi mới có chỗ gắn ảnh.</summary>
    public async Task<IActionResult> OnPostAnhAsync(string khoa, string bai, List<IFormFile> files, CancellationToken cancellationToken)
    {
        if (!await LoadAsync(khoa, bai, cancellationToken) || Lesson is null)
        {
            return NotFound();
        }

        ModelState.Clear();
        var added = new List<string>();

        foreach (var file in files.Where(file => file.Length > 0).Take(ImageService.MaxFilesPerUpload))
        {
            try
            {
                added.Add(await _images.AddAsync(ImageService.Kind.Lesson, file, cancellationToken));
            }
            catch (ImageUploadException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
        }

        if (added.Count == 0 && ModelState.IsValid)
        {
            ModelState.AddModelError(string.Empty, "Chưa chọn ảnh nào.");
        }

        if (added.Count > 0)
        {
            await UpdateLessonAsync(khoa, bai, lesson => lesson.Images.AddRange(added), cancellationToken);
            Message = $"Đã tải {added.Count} ảnh. Bấm \"Chèn\" để đưa ảnh vào chỗ con trỏ trong bài.";
        }

        if (!ModelState.IsValid)
        {
            await LoadAsync(khoa, bai, cancellationToken);
            FillInput(Lesson!);
            return Page();
        }

        return RedirectToPage(new { khoa, bai });
    }

    public async Task<IActionResult> OnPostXoaAnhAsync(string khoa, string bai, string url, CancellationToken cancellationToken)
    {
        if (!await LoadAsync(khoa, bai, cancellationToken) || Lesson is null)
        {
            return NotFound();
        }

        ModelState.Clear();

        // Ảnh còn nằm trong bài mà xoá file thì bài hiện ảnh vỡ.
        if (Lesson.Body.Contains(url, StringComparison.Ordinal))
        {
            ModelState.AddModelError(string.Empty,
                "Ảnh này đang được dùng trong bài. Xoá dòng ![...](/" + url + ") khỏi bài, lưu, rồi mới xoá ảnh.");
            FillInput(Lesson);
            return Page();
        }

        var removed = false;
        await UpdateLessonAsync(khoa, bai, lesson => removed = lesson.Images.Remove(url), cancellationToken);

        if (removed)
        {
            _images.Delete(url);
            Message = "Đã xoá ảnh.";
        }

        return RedirectToPage(new { khoa, bai });
    }

    private Task<bool> UpdateLessonAsync(string khoa, string bai, Action<Lesson> mutate, CancellationToken cancellationToken) =>
        _content.Courses.UpdateAsync(list =>
        {
            var lesson = list.FirstOrDefault(item => item.Slug == khoa)?.Lessons.FirstOrDefault(item => item.Slug == bai);

            if (lesson is null)
            {
                return false;
            }

            mutate(lesson);
            return true;
        }, cancellationToken);

    private void FillInput(Lesson lesson) =>
        Input = new LessonInput
        {
            Title = lesson.Title, Slug = lesson.Slug, Minutes = lesson.Minutes, Video = lesson.Video, Body = lesson.Body,
            Outcomes = string.Join('\n', lesson.Outcomes), Exercise = lesson.Exercise, NextStep = lesson.NextStep
        };

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
