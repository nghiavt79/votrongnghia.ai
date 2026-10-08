using VoTrongNghia.Models;
using VoTrongNghia.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace VoTrongNghia.Pages.HocVien;

/// <summary>
/// Trang học viên: buổi học đã được duyệt (kèm link phòng học), tiến độ từng khóa, đăng xuất,
/// tự xoá tài khoản.
///
/// <para>Link phòng học (<see cref="LiveSession.MeetingLink"/>) chỉ hiện ở đây, cho đúng người có đơn
/// <em>đã duyệt</em> vào buổi đó — vẫn không bao giờ ra trang công khai.</para>
/// </summary>
public class IndexModel : PageModel
{
    private readonly LearnerSession _session;
    private readonly LearnerStore _learners;
    private readonly LoginLinkService _links;
    private readonly RegistrationStore _registrations;
    private readonly SiteContent _content;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(LearnerSession session, LearnerStore learners, LoginLinkService links, RegistrationStore registrations,
        SiteContent content, ILogger<IndexModel> logger)
    {
        _session = session;
        _learners = learners;
        _links = links;
        _registrations = registrations;
        _content = content;
        _logger = logger;
    }

    public Learner Learner { get; private set; } = new();

    /// <summary>Một khóa và tiến độ của học viên trong khóa đó.</summary>
    public sealed record CourseProgress(Course Course, int Done, Lesson? Next)
    {
        public int Percent => Course.Lessons.Count == 0 ? 0 : (int)Math.Round(100.0 * Done / Course.Lessons.Count);
    }

    public IReadOnlyList<CourseProgress> Courses { get; private set; } = [];

    /// <summary>Buổi học của các đơn đã duyệt; null khi buổi đã bị xoá khỏi lịch (còn tên trong đơn).</summary>
    public sealed record ApprovedSession(Registration Registration, LiveSession? Session)
    {
        public bool IsPast => Session is not null && Session.Date < SiteTime.Today;
    }

    public IReadOnlyList<ApprovedSession> Sessions { get; private set; } = [];

    /// <summary>Người duyệt đã nhận nhưng đăng ký lúc chưa có lịch.</summary>
    public bool WaitingForSchedule { get; private set; }

    public string? Error { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        if (!await LoadAsync(cancellationToken))
        {
            return RedirectToPage("/HocVien/DangNhap");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostDangXuatAsync()
    {
        await _session.SignOutAsync(HttpContext);
        TempData["Message"] = "Đã đăng xuất. Cần vào lại thì nhập email để nhận link mới.";
        return RedirectToPage("/HocVien/DangNhap");
    }

    /// <summary>
    /// Học viên tự xoá tài khoản (Nghị định 13/2023: người dùng có quyền yêu cầu xoá dữ liệu). Xoá
    /// tài khoản và tiến độ; đơn đăng ký giữ theo hạn của nó — trang chính sách dữ liệu nói rõ.
    /// </summary>
    public async Task<IActionResult> OnPostXoaTaiKhoanAsync(bool confirm, CancellationToken cancellationToken)
    {
        if (!await LoadAsync(cancellationToken))
        {
            return RedirectToPage("/HocVien/DangNhap");
        }

        if (!confirm)
        {
            Error = "Tích ô xác nhận trước khi xoá tài khoản.";
            return Page();
        }

        await _learners.DeleteAsync(Learner.Id, cancellationToken);
        await _links.RevokeAsync(Learner.Id, cancellationToken);
        await _session.SignOutAsync(HttpContext);
        _logger.LogWarning("Học viên {Id} tự xoá tài khoản", Learner.Id);

        TempData["Message"] = "Đã xoá tài khoản học viên và tiến độ học của bạn khỏi máy chủ.";
        return RedirectToPage("/HocVien/DangNhap");
    }

    private async Task<bool> LoadAsync(CancellationToken cancellationToken)
    {
        if (await _session.CurrentAsync(HttpContext) is not { } learner)
        {
            return false;
        }

        Learner = learner;

        Courses = (await _content.PublishedCoursesAsync(cancellationToken))
            .Where(course => course.Lessons.Count > 0)
            .Select(course => new CourseProgress(
                course,
                course.Lessons.Count(lesson => learner.Progress.ContainsKey($"{course.Slug}/{lesson.Slug}")),
                course.Lessons.FirstOrDefault(lesson => !learner.Progress.ContainsKey($"{course.Slug}/{lesson.Slug}"))))
            .ToList();

        var live = await _content.Live.ReadAsync(cancellationToken);
        var approved = (await _registrations.Registrations.ReadAsync(cancellationToken))
            .Where(item => learner.RegistrationCodes.Contains(item.Code) && item.Status == Registration.StatusApproved)
            .ToList();

        WaitingForSchedule = approved.Any(item => item.SessionId.Length == 0);
        Sessions = approved
            .Where(item => item.SessionId.Length > 0)
            .Select(item => new ApprovedSession(item, live.Sessions.FirstOrDefault(session => session.Id == item.SessionId)))
            .OrderBy(item => item.IsPast)
            .ThenBy(item => item.Session?.Date)
            .ToList();

        return true;
    }
}
