using System.Text.RegularExpressions;
using VoTrongNghia.Models;
using VoTrongNghia.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace VoTrongNghia.Pages.Admin.HocVien;

/// <summary>
/// Danh sách học viên: tiến độ từng khóa, lần học gần nhất, trạng thái. Lọc người lâu không học để
/// nhắc. Thêm tay học viên, tạo tài khoản cho đơn đã duyệt từ trước khi có tính năng này.
/// </summary>
public partial class IndexModel : PageModel
{
    private readonly LearnerStore _learners;
    private readonly RegistrationStore _registrations;
    private readonly SiteContent _content;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(LearnerStore learners, RegistrationStore registrations, SiteContent content, ILogger<IndexModel> logger)
    {
        _learners = learners;
        _registrations = registrations;
        _content = content;
        _logger = logger;
    }

    public IReadOnlyList<Learner> Items { get; private set; } = [];
    public IReadOnlyList<Course> Courses { get; private set; } = [];
    public int Total { get; private set; }
    public int IdleCount { get; private set; }
    public int ExpiredCount { get; private set; }
    public IReadOnlyDictionary<string, int> CountByStatus { get; private set; } = new Dictionary<string, int>();

    /// <summary>Đơn đã duyệt mà email chưa có tài khoản (duyệt trước khi có trang học viên).</summary>
    public int ApprovedWithoutAccount { get; private set; }

    /// <summary>Lọc: trống là tất cả, "lau-khong-hoc", hoặc một trạng thái.</summary>
    public string Filter { get; private set; } = string.Empty;

    public const string FilterIdle = "lau-khong-hoc";

    [BindProperty]
    public string NewName { get; set; } = string.Empty;

    [BindProperty]
    public string NewEmail { get; set; } = string.Empty;

    [BindProperty]
    public string NewPhone { get; set; } = string.Empty;

    [TempData]
    public string? Message { get; set; }

    public string? Error { get; private set; }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailPattern();

    public int Done(Learner learner, Course course) =>
        course.Lessons.Count(lesson => learner.Progress.ContainsKey($"{course.Slug}/{lesson.Slug}"));

    public async Task OnGetAsync(string? loc, CancellationToken cancellationToken)
    {
        Filter = loc ?? string.Empty;
        await LoadAsync(cancellationToken);
    }

    /// <summary>Thêm tay một học viên — người được hướng dẫn ngoài form đăng ký.</summary>
    public async Task<IActionResult> OnPostThemAsync(CancellationToken cancellationToken)
    {
        var name = NewName.Trim();
        var email = LearnerStore.NormalizeEmail(NewEmail);

        if (name.Length < 2 || name.Length > 80)
        {
            Error = "Nhập họ tên (2–80 ký tự).";
        }
        else if (email.Length > 120 || !EmailPattern().IsMatch(email))
        {
            Error = "Email chưa đúng.";
        }
        else if (await _learners.AddAsync(name, email, NewPhone, cancellationToken) is { } learner)
        {
            _logger.LogInformation("{User} thêm tay học viên {Id}", User.Identity?.Name, learner.Id);
            Message = $"Đã thêm {learner.Name}. Gửi link đăng nhập ở trang chi tiết.";
            return RedirectToPage("/Admin/HocVien/ChiTiet", new { id = learner.Id });
        }
        else
        {
            Error = "Email này đã có tài khoản học viên.";
        }

        await LoadAsync(cancellationToken);
        return Page();
    }

    /// <summary>Tạo tài khoản cho mọi đơn đã duyệt mà email chưa có tài khoản. Không gửi thư nào.</summary>
    public async Task<IActionResult> OnPostTuDonAsync(CancellationToken cancellationToken)
    {
        var count = 0;

        foreach (var registration in await PendingApprovedAsync(cancellationToken))
        {
            await _learners.EnsureForRegistrationAsync(registration, cancellationToken);
            count++;
        }

        _logger.LogInformation("{User} tạo {Count} tài khoản học viên từ đơn đã duyệt", User.Identity?.Name, count);
        Message = count > 0
            ? $"Đã tạo tài khoản cho {count} đơn đã duyệt. Chưa gửi thư nào — gửi link đăng nhập ở trang từng học viên."
            : "Mọi đơn đã duyệt đều đã có tài khoản.";

        return RedirectToPage();
    }

    /// <summary>Thực hiện cam kết giữ dữ liệu: ẩn danh hoá học viên không hoạt động quá hạn.</summary>
    public async Task<IActionResult> OnPostAnDanhAsync(CancellationToken cancellationToken)
    {
        var count = await _learners.AnonymizeExpiredAsync(cancellationToken);
        _logger.LogInformation("{User} ẩn danh hoá {Count} học viên", User.Identity?.Name, count);
        Message = count > 0 ? $"Đã ẩn danh hoá {count} học viên." : "Không có học viên nào quá hạn.";
        return RedirectToPage();
    }

    private async Task<List<Registration>> PendingApprovedAsync(CancellationToken cancellationToken)
    {
        var learners = await _learners.Learners.ReadAsync(cancellationToken);
        var emails = learners.Select(item => item.Email).ToHashSet(StringComparer.Ordinal);

        // Một email nhiều đơn đã duyệt: lấy hết, EnsureForRegistrationAsync gắn từng đơn vào cùng tài khoản.
        return (await _registrations.Registrations.ReadAsync(cancellationToken))
            .Where(item => item.Status == Registration.StatusApproved && item.Email.Length > 0)
            .Where(item => !emails.Contains(LearnerStore.NormalizeEmail(item.Email)) ||
                           learners.Any(learner => learner.Email == LearnerStore.NormalizeEmail(item.Email) && !learner.RegistrationCodes.Contains(item.Code)))
            .ToList();
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var all = await _learners.Learners.ReadAsync(cancellationToken);
        Courses = (await _content.PublishedCoursesAsync(cancellationToken)).Where(course => course.Lessons.Count > 0).ToList();
        Total = all.Count;
        IdleCount = all.Count(LearnerStore.IsIdle);
        ExpiredCount = all.Count(LearnerStore.IsExpired);
        CountByStatus = all.GroupBy(item => item.Status).ToDictionary(group => group.Key, group => group.Count());
        ApprovedWithoutAccount = (await PendingApprovedAsync(cancellationToken)).Count;

        Items = (Filter switch
        {
            FilterIdle => all.Where(LearnerStore.IsIdle).OrderBy(item => item.LastActivity),
            "" => all.OrderByDescending(item => item.LastActivity),
            _ => all.Where(item => item.Status == Filter).OrderByDescending(item => item.LastActivity)
        }).ToList();
    }
}
