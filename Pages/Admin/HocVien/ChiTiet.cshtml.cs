using VoTrongNghia.Models;
using VoTrongNghia.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace VoTrongNghia.Pages.Admin.HocVien;

/// <summary>
/// Một học viên: dòng thời gian học, các đơn, lịch sử email, ghi chú, trạng thái. Gửi link đăng nhập
/// qua email, hoặc tạo link để gửi tay qua Zalo khi Gmail trục trặc. Soạn thư nhắc học.
/// </summary>
public class ChiTietModel : PageModel
{
    private readonly LearnerStore _learners;
    private readonly LoginLinkService _links;
    private readonly RegistrationStore _registrations;
    private readonly SiteContent _content;
    private readonly EmailSender _email;
    private readonly string _baseUrl;
    private readonly ILogger<ChiTietModel> _logger;

    public ChiTietModel(LearnerStore learners, LoginLinkService links, RegistrationStore registrations, SiteContent content,
        EmailSender email, IConfiguration configuration, ILogger<ChiTietModel> logger)
    {
        _learners = learners;
        _links = links;
        _registrations = registrations;
        _content = content;
        _email = email;
        _baseUrl = (configuration["Site:BaseUrl"] ?? string.Empty).TrimEnd('/');
        _logger = logger;
    }

    public Learner Item { get; private set; } = new();
    public IReadOnlyList<Course> Courses { get; private set; } = [];
    public IReadOnlyList<Registration> Registrations { get; private set; } = [];

    /// <summary>Một bài đã học: lúc đánh dấu, tên khóa, tên bài (khoá thô khi bài đã bị xoá khỏi khóa).</summary>
    public sealed record TimelineEntry(DateTimeOffset At, string Course, string Lesson);

    public IReadOnlyList<TimelineEntry> Timeline { get; private set; } = [];

    public string SiteName { get; private set; } = string.Empty;
    public EmailSettings EmailSettings { get; private set; } = new();
    public bool EmailReady => EmailSettings.IsReady;

    [BindProperty]
    public string Status { get; set; } = string.Empty;

    [BindProperty]
    public string AdminNote { get; set; } = string.Empty;

    [BindProperty]
    public string EmailSubject { get; set; } = string.Empty;

    [BindProperty]
    public string EmailBody { get; set; } = string.Empty;

    [TempData]
    public string? Message { get; set; }

    [TempData]
    public string? EmailError { get; set; }

    /// <summary>Link đăng nhập vừa tạo để gửi tay — hiện đúng một lần, không lưu ở đâu.</summary>
    [TempData]
    public string? ManualLink { get; set; }

    public int Done(Course course) =>
        course.Lessons.Count(lesson => Item.Progress.ContainsKey($"{course.Slug}/{lesson.Slug}"));

    public async Task<IActionResult> OnGetAsync(string id, CancellationToken cancellationToken)
    {
        if (!await LoadAsync(id, cancellationToken))
        {
            return NotFound();
        }

        Status = Item.Status;
        AdminNote = Item.AdminNote;
        FillReminder();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string id, CancellationToken cancellationToken)
    {
        if (Learner.Statuses.All(item => item.Key != Status))
        {
            return BadRequest();
        }

        if (!await _learners.UpdateAsync(id, Status, AdminNote.Replace("\r\n", "\n").Trim(), cancellationToken))
        {
            return NotFound();
        }

        if (Status == Learner.StatusLocked)
        {
            await _links.RevokeAsync(id, cancellationToken);
        }

        _logger.LogInformation("{User} đổi học viên {Id} sang {Status}", User.Identity?.Name, id, Status);
        Message = Status == Learner.StatusLocked ? "Đã lưu. Tài khoản bị khoá: mọi phiên đăng nhập đang mở bị đăng xuất." : "Đã lưu.";
        return RedirectToPage(new { id });
    }

    /// <summary>Gửi link đăng nhập (hạn 7 ngày) qua email, ngay trong request để thấy lỗi tại chỗ.</summary>
    public async Task<IActionResult> OnPostGuiLinkAsync(string id, CancellationToken cancellationToken)
    {
        if (!await LoadAsync(id, cancellationToken) || Item.Email.Length == 0)
        {
            return NotFound();
        }

        if (Item.Status == Learner.StatusLocked)
        {
            EmailError = "Tài khoản đang khoá — mở khoá trước khi gửi link.";
            return RedirectToPage(new { id });
        }

        var token = await _links.CreateAsync(Item.Id, LoginLinkService.LongLifetime, limitRequests: false, cancellationToken);
        var siteName = (await _content.Site.ReadAsync(cancellationToken)).Name;
        var message = EmailTemplates.LoginLink(Item, LoginLinkService.Url(_baseUrl, token!), LoginLinkService.LongLifetime, siteName, _baseUrl, EmailSettings.OwnerAddress);
        var error = await SendAndLogAsync(message, EmailLogEntry.KindLogin, cancellationToken);

        if (error is null)
        {
            Message = $"Đã gửi link đăng nhập tới {Item.Email}.";
        }
        else
        {
            EmailError = error;
        }

        return RedirectToPage(new { id });
    }

    /// <summary>
    /// Tạo link đăng nhập (hạn 7 ngày) để chép gửi tay qua Zalo — lối thoát khi Gmail lỗi. Link theo
    /// địa chỉ đang mở /cms, nên trên máy dev là link localhost, trên máy chủ là link thật.
    /// </summary>
    public async Task<IActionResult> OnPostTaoLinkAsync(string id, CancellationToken cancellationToken)
    {
        if (!await LoadAsync(id, cancellationToken))
        {
            return NotFound();
        }

        if (Item.Status == Learner.StatusLocked)
        {
            EmailError = "Tài khoản đang khoá — mở khoá trước khi tạo link.";
            return RedirectToPage(new { id });
        }

        var token = await _links.CreateAsync(Item.Id, LoginLinkService.LongLifetime, limitRequests: false, cancellationToken);
        ManualLink = LoginLinkService.Url($"{Request.Scheme}://{Request.Host}", token!);
        _logger.LogInformation("{User} tạo link đăng nhập gửi tay cho học viên {Id}", User.Identity?.Name, id);

        return RedirectToPage(new { id });
    }

    /// <summary>Gửi thư tự soạn (điền sẵn thư nhắc học, sửa thoải mái trước khi gửi).</summary>
    public async Task<IActionResult> OnPostGuiEmailAsync(string id, CancellationToken cancellationToken)
    {
        if (!await LoadAsync(id, cancellationToken) || Item.Email.Length == 0)
        {
            return NotFound();
        }

        var subject = EmailSubject.Trim();
        var body = EmailBody.Replace("\r\n", "\n").Trim();

        if (subject.Length == 0 || body.Length == 0)
        {
            EmailError = "Nhập tiêu đề và nội dung thư.";
            return RedirectToPage(new { id });
        }

        if (MarkdownRenderer.FindPlaceholder(body) is { } placeholder)
        {
            EmailError = $"Thư còn chỗ trống chưa điền: {placeholder}.";
            return RedirectToPage(new { id });
        }

        var error = await SendAndLogAsync(new EmailMessage(Item.Email, subject, body, EmailSettings.OwnerAddress), EmailLogEntry.KindCustom, cancellationToken);

        if (error is null)
        {
            Message = $"Đã gửi email tới {Item.Email}.";
        }
        else
        {
            EmailError = error;
        }

        return RedirectToPage(new { id });
    }

    /// <summary>Xoá hẳn tài khoản (học viên xin xoá). Đơn đăng ký không bị xoá theo — xoá ở trang đơn.</summary>
    public async Task<IActionResult> OnPostXoaAsync(string id, string? confirm, CancellationToken cancellationToken)
    {
        if (!await LoadAsync(id, cancellationToken))
        {
            return NotFound();
        }

        var expected = Item.Email.Length > 0 ? Item.Email : Item.Id;

        if (!string.Equals(confirm?.Trim(), expected, StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(string.Empty, $"Muốn xoá thì gõ đúng \"{expected}\" vào ô xác nhận.");
            Status = Item.Status;
            AdminNote = Item.AdminNote;
            FillReminder();
            return Page();
        }

        await _learners.DeleteAsync(id, cancellationToken);
        await _links.RevokeAsync(id, cancellationToken);
        _logger.LogWarning("{User} đã xoá học viên {Id}", User.Identity?.Name, id);
        TempData["Message"] = $"Đã xoá tài khoản học viên {Item.Name}.";

        return RedirectToPage("/Admin/HocVien/Index");
    }

    private async Task<string?> SendAndLogAsync(EmailMessage message, string kind, CancellationToken cancellationToken)
    {
        var error = await _email.SendAsync(message, cancellationToken);

        await _learners.LogEmailAsync(Item.Id, new EmailLogEntry
        {
            At = SiteTime.Now,
            Kind = kind,
            To = message.To,
            Subject = message.Subject,
            Error = error
        }, cancellationToken);

        return error;
    }

    /// <summary>Điền sẵn khung thư bằng thư nhắc, trỏ tới bài kế tiếp chưa học.</summary>
    private void FillReminder()
    {
        var next = Courses
            .SelectMany(course => course.Lessons.Select(lesson => (course, lesson)))
            .FirstOrDefault(pair => !Item.Progress.ContainsKey($"{pair.course.Slug}/{pair.lesson.Slug}"));
        var nextText = next.course is null ? string.Empty : $"{next.lesson.Title} ({next.course.Title})\n{_baseUrl}{next.course.LessonUrl(next.lesson)}";
        var draft = EmailTemplates.Reminder(Item, nextText, SiteName, _baseUrl, null);

        EmailSubject = draft.Subject;
        EmailBody = draft.Body;
    }

    private async Task<bool> LoadAsync(string id, CancellationToken cancellationToken)
    {
        if (await _learners.FindAsync(id, cancellationToken) is not { } item)
        {
            return false;
        }

        Item = item;
        SiteName = (await _content.Site.ReadAsync(cancellationToken)).Name;
        EmailSettings = await _email.Settings.ReadAsync(cancellationToken);
        Courses = (await _content.PublishedCoursesAsync(cancellationToken)).Where(course => course.Lessons.Count > 0).ToList();
        Registrations = (await _registrations.Registrations.ReadAsync(cancellationToken))
            .Where(entry => item.RegistrationCodes.Contains(entry.Code) || (item.Email.Length > 0 && entry.Email == item.Email))
            .ToList();

        // Tra theo mọi khóa (kể cả chưa đăng) để bài trong khóa đã ẩn vẫn hiện được tên.
        var lessons = (await _content.Courses.ReadAsync(cancellationToken))
            .SelectMany(course => course.Lessons.Select(lesson => (Key: $"{course.Slug}/{lesson.Slug}", Course: course.Title, Lesson: lesson.Title)))
            .GroupBy(entry => entry.Key)
            .ToDictionary(group => group.Key, group => group.First());

        Timeline = item.Progress
            .Select(pair => lessons.TryGetValue(pair.Key, out var found)
                ? new TimelineEntry(pair.Value, found.Course, found.Lesson)
                : new TimelineEntry(pair.Value, "(bài đã bị xoá)", pair.Key))
            .OrderByDescending(entry => entry.At)
            .ToList();

        return true;
    }
}
