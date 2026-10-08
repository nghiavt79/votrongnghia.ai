using System.Globalization;
using VoTrongNghia.Models;
using VoTrongNghia.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace VoTrongNghia.Pages.Admin;

/// <summary>Điều kiện đăng ký lớp online (bài đầu vào, giờ tối thiểu, cam kết) và lịch các buổi.</summary>
public class LopOnlineModel : PageModel
{
    private readonly SiteContent _content;
    private readonly RegistrationStore _registrations;

    public LopOnlineModel(SiteContent content, RegistrationStore registrations)
    {
        _content = content;
        _registrations = registrations;
    }

    public LiveClass Live { get; private set; } = new();
    public IReadOnlyList<Course> Courses { get; private set; } = [];

    /// <summary>Số đơn (chưa bị từ chối) và số đơn đã duyệt của từng buổi.</summary>
    public IReadOnlyDictionary<string, (int All, int Approved)> Counts { get; private set; } = new Dictionary<string, (int, int)>();

    [BindProperty]
    public RulesInput Rules { get; set; } = new();

    [BindProperty]
    public SessionInput Session { get; set; } = new();

    /// <summary>Buổi đang sửa (?sua=mã); null là đang thêm buổi mới.</summary>
    public string? EditingId { get; private set; }

    [TempData]
    public string? Message { get; set; }

    public sealed class RulesInput
    {
        public string RequireCourse { get; set; } = string.Empty;
        public int MinHoursPerWeek { get; set; }

        /// <summary>Mỗi dòng một cam kết.</summary>
        public string Commitments { get; set; } = string.Empty;

        public string Pledge { get; set; } = string.Empty;

        /// <summary>Mỗi dòng một ý.</summary>
        public string ForWho { get; set; } = string.Empty;
        public string NotForWho { get; set; } = string.Empty;
        public string Outcomes { get; set; } = string.Empty;
    }

    /// <summary>Tách ô nhiều dòng thành danh sách: bỏ dòng trống, bỏ gạch đầu dòng gõ thừa.</summary>
    private static List<string> Lines(string text, int max = 10) =>
        text.Replace("\r\n", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line => line.TrimStart('-', '*', '•', ' '))
            .Where(line => line.Length > 0)
            .Take(max)
            .ToList();

    public sealed class SessionInput
    {
        public string Title { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
        public string Platform { get; set; } = "Google Meet";
        public string MeetingLink { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Capacity { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(string? sua, CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken);
        FillRules();

        if (sua is not null)
        {
            var session = Live.Sessions.FirstOrDefault(item => item.Id == sua);

            if (session is null)
            {
                return RedirectToPage();
            }

            EditingId = session.Id;
            Session = new SessionInput
            {
                Title = session.Title,
                Date = session.Date.ToString("yyyy-MM-dd"),
                Time = session.Time,
                Platform = session.Platform,
                MeetingLink = session.MeetingLink,
                Description = session.Description,
                Capacity = session.Capacity
            };
        }

        return Page();
    }

    public async Task<IActionResult> OnPostDieuKienAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken);
        ModelState.Clear();

        var commitments = Lines(Rules.Commitments);

        if (Rules.RequireCourse.Length > 0 && Courses.All(course => course.Slug != Rules.RequireCourse))
        {
            ModelState.AddModelError("Rules.RequireCourse", "Chỉ chọn được khóa đã đăng và có bài.");
        }

        if (Rules.MinHoursPerWeek is < 0 or > 40)
        {
            ModelState.AddModelError("Rules.MinHoursPerWeek", "Số giờ tối thiểu từ 0 đến 40.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        await _content.Live.UpdateAsync(live =>
        {
            live.RequireCourse = Rules.RequireCourse;
            live.MinHoursPerWeek = Rules.MinHoursPerWeek;
            live.Commitments = commitments;
            live.Pledge = Rules.Pledge.Trim();
            live.ForWho = Lines(Rules.ForWho);
            live.NotForWho = Lines(Rules.NotForWho);
            live.Outcomes = Lines(Rules.Outcomes);
            return true;
        }, cancellationToken);

        Message = "Đã lưu điều kiện đăng ký.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostBuoiAsync(string? sua, CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken);
        ModelState.Clear();
        EditingId = sua;

        var title = Session.Title.Trim();

        if (title.Length == 0)
        {
            ModelState.AddModelError("Session.Title", "Nhập chủ đề buổi học.");
        }

        if (!DateOnly.TryParseExact(Session.Date.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            ModelState.AddModelError("Session.Date", "Chọn ngày học.");
        }
        else if (sua is null && date < SiteTime.Today)
        {
            ModelState.AddModelError("Session.Date", "Ngày học đã qua — buổi đã qua tự ẩn khỏi site.");
        }

        if (Session.Time.Trim().Length == 0)
        {
            ModelState.AddModelError("Session.Time", "Nhập giờ học, ví dụ 20:00 – 21:30.");
        }

        var meetingLink = Session.MeetingLink.Trim();

        if (meetingLink.Length > 0 && !meetingLink.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError("Session.MeetingLink", "Link phòng học phải bắt đầu bằng https://");
        }

        if (Session.Capacity is < 0 or > 1000)
        {
            ModelState.AddModelError("Session.Capacity", "Số chỗ từ 0 (không giới hạn) đến 1000.");
        }

        if (!ModelState.IsValid)
        {
            FillRules();
            return Page();
        }

        var saved = await _content.Live.UpdateAsync(live =>
        {
            var session = sua is null ? new LiveSession() : live.Sessions.FirstOrDefault(item => item.Id == sua);

            if (session is null)
            {
                return false;
            }

            if (sua is null)
            {
                // Mã cố định theo ngày + chủ đề lúc tạo. Đơn đăng ký trỏ tới mã này, nên sửa
                // chủ đề hay dời ngày về sau cũng không đổi mã.
                var id = $"{date:yyyy-MM-dd}-{Slugs.From(title)}";
                session.Id = live.Sessions.Any(item => item.Id == id) ? $"{id}-{Guid.NewGuid().ToString("N")[..4]}" : id;
                live.Sessions.Add(session);
            }

            session.Title = title;
            session.Date = date;
            session.Time = Session.Time.Trim();
            session.Platform = Session.Platform.Trim();
            session.MeetingLink = meetingLink;
            session.Description = Session.Description.Trim();
            session.Capacity = Session.Capacity;
            live.Sessions = live.Sessions.OrderBy(item => item.Date).ToList();
            return true;
        }, cancellationToken);

        Message = !saved ? "Buổi này vừa bị xoá ở tab khác." : sua is null ? "Đã thêm buổi học — trang chủ hiện ngay." : "Đã lưu buổi học.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostXoaBuoiAsync(string id, CancellationToken cancellationToken)
    {
        await _content.Live.UpdateAsync(live => live.Sessions.RemoveAll(item => item.Id == id) > 0, cancellationToken);
        Message = "Đã xoá buổi học. Đơn đã đăng ký buổi này vẫn giữ tên buổi để đọc lại.";
        return RedirectToPage();
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        Live = await _content.Live.ReadAsync(cancellationToken);
        Courses = (await _content.PublishedCoursesAsync(cancellationToken)).Where(course => course.Lessons.Count > 0).ToList();

        var registrations = await _registrations.Registrations.ReadAsync(cancellationToken);
        Counts = registrations
            .Where(item => item.SessionId.Length > 0 && item.Status != Registration.StatusRejected)
            .GroupBy(item => item.SessionId)
            .ToDictionary(group => group.Key, group => (group.Count(), group.Count(item => item.Status == Registration.StatusApproved)));
    }

    private void FillRules()
    {
        Rules = new RulesInput
        {
            RequireCourse = Live.RequireCourse,
            MinHoursPerWeek = Live.MinHoursPerWeek,
            Commitments = string.Join('\n', Live.Commitments),
            Pledge = Live.Pledge,
            ForWho = string.Join('\n', Live.ForWho),
            NotForWho = string.Join('\n', Live.NotForWho),
            Outcomes = string.Join('\n', Live.Outcomes)
        };
    }
}
