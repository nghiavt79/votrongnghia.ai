using VoTrongNghia.Models;
using VoTrongNghia.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace VoTrongNghia.Pages.Admin.DangKy;

/// <summary>
/// Đọc một đơn và quyết định: duyệt, chờ đợt sau, không phù hợp.
///
/// <para>Đã thiết lập Gmail (/cms/email) thì lưu trạng thái kèm gửi luôn thư báo kết quả theo
/// mẫu (<see cref="EmailTemplates.Result"/>), hoặc soạn thư tuỳ ý rồi gửi. Gửi ngay trong
/// request chứ không qua hàng đợi: người duyệt cần thấy gửi được hay lỗi ngay tại chỗ. Chưa
/// thiết lập thì vẫn còn nút mở ứng dụng email của người duyệt với nội dung soạn sẵn.</para>
///
/// <para>Duyệt đơn là tạo tài khoản học viên (hoặc gắn đơn vào tài khoản cùng email đã có), và thư
/// báo duyệt mang link đăng nhập trang học viên hạn 7 ngày.</para>
/// </summary>
public class ChiTietModel : PageModel
{
    private readonly RegistrationStore _registrations;
    private readonly SiteContent _content;
    private readonly EmailSender _email;
    private readonly LearnerStore _learners;
    private readonly LoginLinkService _links;
    private readonly string _baseUrl;
    private readonly ILogger<ChiTietModel> _logger;

    public ChiTietModel(RegistrationStore registrations, SiteContent content, EmailSender email, LearnerStore learners,
        LoginLinkService links, IConfiguration configuration, ILogger<ChiTietModel> logger)
    {
        _learners = learners;
        _links = links;
        _registrations = registrations;
        _content = content;
        _email = email;
        _baseUrl = (configuration["Site:BaseUrl"] ?? string.Empty).TrimEnd('/');
        _logger = logger;
    }

    public Registration Item { get; private set; } = new();
    public LiveSession? Session { get; private set; }

    /// <summary>Số đơn đã duyệt của cùng buổi, để so với số chỗ.</summary>
    public int ApprovedInSession { get; private set; }

    /// <summary>Đơn khác cùng email hoặc số điện thoại (đăng ký buổi khác, hoặc từng bị từ chối).</summary>
    public IReadOnlyList<Registration> Others { get; private set; } = [];

    /// <summary>Tài khoản học viên cùng email, nếu có.</summary>
    public Learner? Learner { get; private set; }

    public string SiteName { get; private set; } = string.Empty;
    public EmailSettings EmailSettings { get; private set; } = new();
    public bool EmailReady => EmailSettings.IsReady;

    [BindProperty]
    public string Status { get; set; } = string.Empty;

    [BindProperty]
    public string AdminNote { get; set; } = string.Empty;

    /// <summary>Tích "Gửi email báo kết quả" khi lưu trạng thái.</summary>
    [BindProperty]
    public bool SendResult { get; set; }

    [BindProperty]
    public string EmailSubject { get; set; } = string.Empty;

    [BindProperty]
    public string EmailBody { get; set; } = string.Empty;

    [TempData]
    public string? Message { get; set; }

    [TempData]
    public string? EmailError { get; set; }

    public async Task<IActionResult> OnGetAsync(string ma, CancellationToken cancellationToken)
    {
        if (!await LoadAsync(ma, cancellationToken))
        {
            return NotFound();
        }

        Status = Item.Status;
        AdminNote = Item.AdminNote;
        SendResult = EmailReady;
        FillDraft();
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

        await LoadAsync(ma, cancellationToken);

        string? loginUrl = null;

        if (Status == Registration.StatusApproved)
        {
            var learner = await _learners.EnsureForRegistrationAsync(Item, cancellationToken);

            // Mã đăng nhập chỉ sinh khi thật sự gửi thư — mỗi lần bấm Lưu không để lại một mã thừa.
            if (SendResult && EmailReady && learner.Status != Models.Learner.StatusLocked &&
                await _links.CreateAsync(learner.Id, LoginLinkService.LongLifetime, limitRequests: false, cancellationToken) is { } token)
            {
                loginUrl = LoginLinkService.Url(_baseUrl, token);
            }
        }

        if (SendResult && EmailReady && EmailTemplates.Result(Item, Session, SiteName, _baseUrl, EmailSettings.OwnerAddress, loginUrl) is { } result)
        {
            var error = await SendAndLogAsync(result, EmailLogEntry.KindResult, cancellationToken);

            if (error is null)
            {
                Message = $"Đã lưu và gửi email báo kết quả tới {Item.Email}.";
            }
            else
            {
                Message = "Đã lưu trạng thái.";
                EmailError = "Chưa gửi được email báo kết quả: " + error;
            }
        }
        else
        {
            Message = EmailReady || Status == Registration.StatusNew
                ? "Đã lưu."
                : "Đã lưu. Nhớ báo kết quả cho người học — bấm \"Soạn email\".";
        }

        return RedirectToPage(new { ma });
    }

    /// <summary>Gửi thư tự soạn (đã điền sẵn theo trạng thái, sửa thoải mái trước khi gửi).</summary>
    public async Task<IActionResult> OnPostGuiEmailAsync(string ma, CancellationToken cancellationToken)
    {
        if (!await LoadAsync(ma, cancellationToken))
        {
            return NotFound();
        }

        var subject = EmailSubject.Trim();
        var body = EmailBody.Replace("\r\n", "\n").Trim();

        if (subject.Length == 0 || body.Length == 0)
        {
            EmailError = "Nhập tiêu đề và nội dung thư.";
            return RedirectToPage(new { ma });
        }

        if (MarkdownRenderer.FindPlaceholder(body) is { } placeholder)
        {
            EmailError = $"Thư còn chỗ trống chưa điền: {placeholder}.";
            return RedirectToPage(new { ma });
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
            FillDraft();
            return Page();
        }

        await _registrations.Registrations.UpdateAsync(list => list.RemoveAll(entry => entry.Code == ma) > 0, cancellationToken);
        _logger.LogWarning("{User} đã xoá đơn {Code}", User.Identity?.Name, ma);
        TempData["Message"] = $"Đã xoá đơn {ma}.";

        return RedirectToPage("/Admin/DangKy/Index");
    }

    /// <summary>Link mailto soạn sẵn thư báo kết quả — lối cũ khi chưa thiết lập Gmail.</summary>
    public string MailTo()
    {
        var draft = EmailTemplates.Result(Item, Session, SiteName, _baseUrl, null)
                    ?? EmailTemplates.Receipt(Item, SiteName, _baseUrl, null);

        return $"mailto:{Item.Email}?subject={Uri.EscapeDataString(draft.Subject)}&body={Uri.EscapeDataString(draft.Body)}";
    }

    private async Task<string?> SendAndLogAsync(EmailMessage message, string kind, CancellationToken cancellationToken)
    {
        var error = await _email.SendAsync(message, cancellationToken);

        await _registrations.LogEmailAsync(Item.Code, new EmailLogEntry
        {
            At = SiteTime.Now,
            Kind = kind,
            To = message.To,
            Subject = message.Subject,
            Error = error
        }, cancellationToken);

        return error;
    }

    /// <summary>Điền sẵn khung thư tự soạn theo trạng thái đã lưu.</summary>
    private void FillDraft()
    {
        var draft = EmailTemplates.Result(Item, Session, SiteName, _baseUrl, null);
        EmailSubject = draft?.Subject ?? $"[{SiteName}] Về đơn đăng ký lớp online của {EmailTemplates.FirstName(Item.Name)}";
        EmailBody = draft?.Body ?? $"Chào {EmailTemplates.FirstName(Item.Name)},\n\n\n\n{SiteName}\n{_baseUrl}\n";
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
        EmailSettings = await _email.Settings.ReadAsync(cancellationToken);
        Session = (await _content.Live.ReadAsync(cancellationToken)).Sessions.FirstOrDefault(session => session.Id == item.SessionId);
        ApprovedInSession = item.SessionId.Length == 0
            ? 0
            : all.Count(entry => entry.SessionId == item.SessionId && entry.Status == Registration.StatusApproved);
        Others = all
            .Where(entry => entry.Code != item.Code && (entry.Email == item.Email || entry.Phone == item.Phone))
            .ToList();
        Learner = await _learners.FindByEmailAsync(item.Email, cancellationToken);

        return true;
    }
}
