using VoTrongNghia.Models;
using VoTrongNghia.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;

namespace VoTrongNghia.Pages;

/// <summary>
/// Đăng ký lớp học online miễn phí. Ba lớp lọc người học nghiêm túc: bài đầu vào (JS mở
/// khóa form khi học xong khóa yêu cầu), câu trả lời đủ dài và đủ giờ tự học, bản cam kết.
/// Máy chủ kiểm lại mọi thứ trừ bài đầu vào (tiến độ nằm trên máy người học) — xem
/// <see cref="RegistrationStore.ReceiveAsync"/>.
/// </summary>
[EnableRateLimiting("dang-ky")]
public class DangKyModel : PageModel
{
    private readonly SiteContent _content;
    private readonly RegistrationStore _registrations;

    public DangKyModel(SiteContent content, RegistrationStore registrations)
    {
        _content = content;
        _registrations = registrations;
    }

    [BindProperty]
    public RegistrationStore.Request Input { get; set; } = new();

    public LiveClass Live { get; private set; } = new();
    public IReadOnlyList<LiveSession> Sessions { get; private set; } = [];

    /// <summary>Khóa phải học xong trước, null khi không yêu cầu (hoặc khóa đã bị gỡ).</summary>
    public Course? RequiredCourse { get; private set; }

    /// <summary>Lỗi theo từng ô, để hiện ngay dưới ô đó.</summary>
    public IReadOnlyList<RegistrationStore.FieldError> Errors { get; private set; } = [];

    /// <summary>Mã đơn vừa gửi xong, hiện ở trang cảm ơn. Qua TempData để tên người học không nằm trên URL.</summary>
    [TempData]
    public string? DoneCode { get; set; }

    [TempData]
    public string? DoneName { get; set; }

    [TempData]
    public string? DoneEmail { get; set; }

    /// <summary>Số đơn đã duyệt của từng buổi, để hiện "còn mấy chỗ". Chỉ là số đếm, không lộ ai.</summary>
    public IReadOnlyDictionary<string, int> ApprovedBySession { get; private set; } = new Dictionary<string, int>();

    /// <summary>Số chỗ còn lại của một buổi; null khi buổi không giới hạn chỗ.</summary>
    public int? SeatsLeft(LiveSession session) =>
        session.Capacity > 0 ? Math.Max(0, session.Capacity - ApprovedBySession.GetValueOrDefault(session.Id)) : null;

    public string? ErrorFor(string field) => Errors.FirstOrDefault(error => error.Field == field)?.Message;

    public async Task OnGetAsync(string? buoi, CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken);
        Input.SessionId = Sessions.Any(session => session.Id == buoi) ? buoi! : string.Empty;
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken);

        var result = await _registrations.ReceiveAsync(Input, cancellationToken);

        if (result.Code is null)
        {
            Errors = result.Errors;
            return Page();
        }

        DoneCode = result.Code;
        DoneName = Input.Name.Trim();
        DoneEmail = Input.Email.Trim();

        // Chuyển hướng sau khi gửi: F5 ở trang cảm ơn không gửi lại đơn lần nữa.
        return RedirectToPage();
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        Live = await _content.Live.ReadAsync(cancellationToken);
        Sessions = Live.Upcoming(SiteTime.Today).ToList();
        ApprovedBySession = (await _registrations.Registrations.ReadAsync(cancellationToken))
            .Where(item => item.Status == Registration.StatusApproved && item.SessionId.Length > 0)
            .GroupBy(item => item.SessionId)
            .ToDictionary(group => group.Key, group => group.Count());

        if (Live.RequireCourse.Length > 0)
        {
            RequiredCourse = (await _content.PublishedCoursesAsync(cancellationToken))
                .FirstOrDefault(course => course.Slug == Live.RequireCourse && course.Lessons.Count > 0);
        }
    }
}
