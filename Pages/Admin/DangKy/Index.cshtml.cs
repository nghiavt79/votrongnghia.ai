using System.Text;
using VoTrongNghia.Models;
using VoTrongNghia.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace VoTrongNghia.Pages.Admin.DangKy;

public class IndexModel : PageModel
{
    private readonly RegistrationStore _registrations;
    private readonly SiteContent _content;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(RegistrationStore registrations, SiteContent content, ILogger<IndexModel> logger)
    {
        _registrations = registrations;
        _content = content;
        _logger = logger;
    }

    public IReadOnlyList<Registration> Items { get; private set; } = [];
    public IReadOnlyDictionary<string, int> CountByStatus { get; private set; } = new Dictionary<string, int>();
    public IReadOnlyList<LiveSession> Sessions { get; private set; } = [];
    public int Total { get; private set; }

    /// <summary>Lọc đang chọn; trống là tất cả.</summary>
    public string Status { get; private set; } = string.Empty;
    public string Session { get; private set; } = string.Empty;

    [TempData]
    public string? Message { get; set; }

    public async Task OnGetAsync(string? trangThai, string? buoi, CancellationToken cancellationToken)
    {
        var all = await _registrations.Registrations.ReadAsync(cancellationToken);
        var live = await _content.Live.ReadAsync(cancellationToken);

        Total = all.Count;
        CountByStatus = all.GroupBy(item => item.Status).ToDictionary(group => group.Key, group => group.Count());
        Sessions = live.Sessions.OrderByDescending(session => session.Date).ToList();
        Status = Registration.Statuses.Any(item => item.Key == trangThai) ? trangThai! : string.Empty;
        Session = buoi ?? string.Empty;
        Items = Filter(all, Status, Session).ToList();
    }

    /// <summary>
    /// Xuất CSV theo đúng bộ lọc đang xem, để gửi email hàng loạt hay mở bằng Excel.
    /// Có BOM UTF-8: thiếu BOM thì Excel trên Windows đọc tiếng Việt thành chữ lỗi.
    /// </summary>
    public async Task<IActionResult> OnGetCsvAsync(string? trangThai, string? buoi, CancellationToken cancellationToken)
    {
        var all = await _registrations.Registrations.ReadAsync(cancellationToken);
        var items = Filter(all, trangThai ?? string.Empty, buoi ?? string.Empty);

        static string Cell(string value) => "\"" + value.Replace("\"", "\"\"").Replace("\n", " ") + "\"";

        var csv = new StringBuilder();
        csv.AppendLine("Mã,Ngày gửi,Trạng thái,Họ tên,Email,SĐT/Zalo,Nghề nghiệp,Mức dùng AI,Buổi,Giờ/tuần,Mục tiêu,Muốn làm ra,Ghi chú");

        foreach (var item in items)
        {
            csv.AppendLine(string.Join(',', new[]
            {
                item.Code, item.CreatedAt.ToString("dd/MM/yyyy HH:mm"), item.StatusLabel, item.Name, item.Email,
                // Dấu ' đầu số điện thoại: không có thì Excel coi là số và nuốt mất số 0 đầu.
                "'" + item.Phone, item.Job, item.AiLevel, item.SessionLabel, item.HoursLabel, item.Goal, item.Project, item.AdminNote
            }.Select(Cell)));
        }

        _logger.LogInformation("{User} xuất {Count} đơn đăng ký ra CSV", User.Identity?.Name, items.Count());

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
        return File(bytes, "text/csv; charset=utf-8", $"dang-ky-{SiteTime.Now:yyyyMMdd}.csv");
    }

    private static IEnumerable<Registration> Filter(IEnumerable<Registration> items, string status, string session) =>
        items
            .Where(item => status.Length == 0 || item.Status == status)
            .Where(item => session.Length == 0 || item.SessionId == session);
}
