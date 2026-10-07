using VoTrongNghia.Models;
using VoTrongNghia.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace VoTrongNghia.Pages.Admin.DangKy;

/// <summary>
/// Đọc một đơn và quyết định: duyệt, chờ đợt sau, không phù hợp. Đổi trạng thái chỉ ghi lại
/// trong sổ — site không tự gửi email. Báo kết quả cho người học bằng nút "Soạn email" (mở
/// ứng dụng email của người duyệt với nội dung soạn sẵn), để mỗi thư vẫn do người thật gửi.
/// </summary>
public class ChiTietModel : PageModel
{
    private readonly RegistrationStore _registrations;
    private readonly SiteContent _content;
    private readonly ILogger<ChiTietModel> _logger;

    public ChiTietModel(RegistrationStore registrations, SiteContent content, ILogger<ChiTietModel> logger)
    {
        _registrations = registrations;
        _content = content;
        _logger = logger;
    }

    public Registration Item { get; private set; } = new();
    public LiveSession? Session { get; private set; }

    /// <summary>Số đơn đã duyệt của cùng buổi, để so với số chỗ.</summary>
    public int ApprovedInSession { get; private set; }

    /// <summary>Đơn khác cùng email hoặc số điện thoại (đăng ký buổi khác, hoặc từng bị từ chối).</summary>
    public IReadOnlyList<Registration> Others { get; private set; } = [];

    public string SiteName { get; private set; } = string.Empty;

    [BindProperty]
    public string Status { get; set; } = string.Empty;

    [BindProperty]
    public string AdminNote { get; set; } = string.Empty;

    [TempData]
    public string? Message { get; set; }

    public async Task<IActionResult> OnGetAsync(string ma, CancellationToken cancellationToken)
    {
        if (!await LoadAsync(ma, cancellationToken))
        {
            return NotFound();
        }

        Status = Item.Status;
        AdminNote = Item.AdminNote;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string ma, CancellationToken cancellationToken)
    {
        if (Registration.Statuses.All(item => item.Key != Status))
        {
            return BadRequest();
        }

        var note = AdminNote.Replace("\r\n", "\n").Trim();
        var saved = await _registrations.Registrations.UpdateAsync(list =>
        {
            var item = list.FirstOrDefault(entry => entry.Code == ma);

            if (item is null)
            {
                return false;
            }

            item.Status = Status;
            item.AdminNote = note.Length > 2000 ? note[..2000] : note;
            item.UpdatedAt = SiteTime.Now;
            return true;
        }, cancellationToken);

        if (!saved)
        {
            return NotFound();
        }

        _logger.LogInformation("{User} đổi đơn {Code} sang {Status}", User.Identity?.Name, ma, Status);
        Message = "Đã lưu. Nhớ báo kết quả cho người học — bấm \"Soạn email\".";

        return RedirectToPage(new { ma });
    }

    /// <summary>Xoá hẳn một đơn — cho đơn rác lọt qua, hoặc khi người học xin xoá thông tin.</summary>
    public async Task<IActionResult> OnPostXoaAsync(string ma, string? confirm, CancellationToken cancellationToken)
    {
        if (!string.Equals(confirm?.Trim(), ma, StringComparison.Ordinal))
        {
            if (!await LoadAsync(ma, cancellationToken))
            {
                return NotFound();
            }

            ModelState.AddModelError(string.Empty, $"Muốn xoá thì gõ đúng mã \"{ma}\" vào ô xác nhận.");
            Status = Item.Status;
            AdminNote = Item.AdminNote;
            return Page();
        }

        await _registrations.Registrations.UpdateAsync(list => list.RemoveAll(entry => entry.Code == ma) > 0, cancellationToken);
        _logger.LogWarning("{User} đã xoá đơn {Code}", User.Identity?.Name, ma);
        TempData["Message"] = $"Đã xoá đơn {ma}.";

        return RedirectToPage("/Admin/DangKy/Index");
    }

    /// <summary>Link mailto soạn sẵn thư báo kết quả theo trạng thái đang chọn.</summary>
    public string MailTo()
    {
        var first = Item.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? Item.Name;
        var session = Session is null ? "lớp học online" : $"buổi \"{Session.Title}\" ngày {Session.Date:dd/MM/yyyy} ({Session.Time}, {Session.Platform})";

        var (subject, body) = Item.Status switch
        {
            Registration.StatusApproved => (
                $"[{SiteName}] Chúc mừng {first} — bạn đã được nhận vào lớp online",
                $"Chào {first},\n\nCảm ơn bạn đã đăng ký và chia sẻ rất cụ thể về mục tiêu của mình. Mình rất vui báo bạn đã được nhận vào {session}.\n\nLink vào lớp: [điền link]\n\nNhắc lại cam kết: tham gia đầy đủ, làm bài tập sau mỗi buổi, báo trước nếu vắng.\n\nHẹn gặp bạn!\n{SiteName}"),
            Registration.StatusWaiting => (
                $"[{SiteName}] Đơn đăng ký của {first} — hẹn bạn đợt sau",
                $"Chào {first},\n\nCảm ơn bạn đã đăng ký. Đợt này lớp đã đủ chỗ nên mình xin hẹn bạn ở đợt kế tiếp — mình sẽ báo ngay khi có lịch.\n\nTrong lúc chờ, bạn cứ học tiếp các khóa miễn phí trên trang nhé.\n\n{SiteName}"),
            Registration.StatusRejected => (
                $"[{SiteName}] Về đơn đăng ký lớp online của {first}",
                $"Chào {first},\n\nCảm ơn bạn đã quan tâm. Lần này mình chưa thể nhận bạn vào lớp vì [lý do]. Bạn có thể học các khóa miễn phí trên trang và đăng ký lại ở đợt sau.\n\n{SiteName}"),
            _ => (
                $"[{SiteName}] Đã nhận đơn đăng ký của {first}",
                $"Chào {first},\n\nMình đã nhận đơn đăng ký (mã {Item.Code}) và sẽ phản hồi trong vài ngày tới.\n\n{SiteName}")
        };

        return $"mailto:{Item.Email}?subject={Uri.EscapeDataString(subject)}&body={Uri.EscapeDataString(body)}";
    }

    private async Task<bool> LoadAsync(string ma, CancellationToken cancellationToken)
    {
        var all = await _registrations.Registrations.ReadAsync(cancellationToken);
        var item = all.FirstOrDefault(entry => entry.Code == ma);

        if (item is null)
        {
            return false;
        }

        Item = item;
        SiteName = (await _content.Site.ReadAsync(cancellationToken)).Name;
        Session = (await _content.Live.ReadAsync(cancellationToken)).Sessions.FirstOrDefault(session => session.Id == item.SessionId);
        ApprovedInSession = item.SessionId.Length == 0
            ? 0
            : all.Count(entry => entry.SessionId == item.SessionId && entry.Status == Registration.StatusApproved);
        Others = all
            .Where(entry => entry.Code != item.Code && (entry.Email == item.Email || entry.Phone == item.Phone))
            .ToList();

        return true;
    }
}
