using System.Text.RegularExpressions;
using VoTrongNghia.Models;
using VoTrongNghia.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace VoTrongNghia.Pages.Admin;

/// <summary>
/// Thiết lập gửi email bằng Gmail: hướng dẫn từng bước phía Google (xác minh 2 bước, mật khẩu
/// ứng dụng), ô dán, nút gửi thư thử. Cùng cách làm với /cms/thiet-lap-google của Dokma: người
/// không rành kỹ thuật làm theo được mà không cần ai ngồi cạnh.
/// </summary>
public partial class EmailModel : PageModel
{
    private readonly EmailSender _sender;
    private readonly SiteContent _content;
    private readonly ILogger<EmailModel> _logger;

    public EmailModel(EmailSender sender, SiteContent content, ILogger<EmailModel> logger)
    {
        _sender = sender;
        _content = content;
        _logger = logger;
    }

    public EmailSettings Settings { get; private set; } = new();
    public string SiteName { get; private set; } = string.Empty;

    [BindProperty]
    public EmailInput Input { get; set; } = new();

    [TempData]
    public string? Message { get; set; }

    [TempData]
    public string? TestError { get; set; }

    public sealed class EmailInput
    {
        public bool Enabled { get; set; }
        public string GmailAddress { get; set; } = string.Empty;

        /// <summary>Trống là giữ mật khẩu đã lưu.</summary>
        public string AppPassword { get; set; } = string.Empty;

        public string FromName { get; set; } = string.Empty;
        public string NotifyAddress { get; set; } = string.Empty;
        public bool SendReceipt { get; set; }
        public bool NotifyOwner { get; set; }
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailPattern();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken);

        Input = new EmailInput
        {
            Enabled = Settings.Enabled || Settings.ProtectedPassword.Length == 0,
            GmailAddress = Settings.GmailAddress,
            FromName = Settings.FromName,
            NotifyAddress = Settings.NotifyAddress,
            SendReceipt = Settings.SendReceipt,
            NotifyOwner = Settings.NotifyOwner
        };
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken);

        var gmail = Input.GmailAddress.Trim().ToLowerInvariant();
        var notify = Input.NotifyAddress.Trim();

        // Google hiện mật khẩu ứng dụng thành 4 nhóm cách nhau ("abcd efgh ijkl mnop"); chép
        // nguyên cả dấu cách cũng được.
        var password = Regex.Replace(Input.AppPassword, @"\s", string.Empty);

        if (!EmailPattern().IsMatch(gmail))
        {
            ModelState.AddModelError("Input.GmailAddress", "Địa chỉ Gmail chưa đúng.");
        }

        if (password.Length > 0 && !Regex.IsMatch(password, "^[a-zA-Z]{16}$"))
        {
            ModelState.AddModelError("Input.AppPassword",
                "Mật khẩu ứng dụng Google gồm đúng 16 chữ cái (ví dụ abcd efgh ijkl mnop). Đừng dán mật khẩu Gmail thường vào đây.");
        }

        if (password.Length == 0 && Settings.ProtectedPassword.Length == 0 && Input.Enabled)
        {
            ModelState.AddModelError("Input.AppPassword", "Dán mật khẩu ứng dụng (bước 3) trước khi bật gửi email.");
        }

        if (notify.Length > 0 && !EmailPattern().IsMatch(notify))
        {
            ModelState.AddModelError("Input.NotifyAddress", "Email nhận báo đơn mới chưa đúng.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var protectedPassword = password.Length > 0 ? _sender.Protect(password) : null;

        await _sender.Settings.UpdateAsync(settings =>
        {
            // Đổi tài khoản Gmail mà không dán mật khẩu mới thì mật khẩu cũ (của tài khoản kia) vô dụng.
            if (settings.GmailAddress != gmail && protectedPassword is null)
            {
                settings.ProtectedPassword = string.Empty;
            }

            settings.Enabled = Input.Enabled;
            settings.GmailAddress = gmail;
            settings.FromName = Input.FromName.Trim();
            settings.NotifyAddress = notify;
            settings.SendReceipt = Input.SendReceipt;
            settings.NotifyOwner = Input.NotifyOwner;

            if (protectedPassword is not null)
            {
                settings.ProtectedPassword = protectedPassword;
                settings.LastTestAt = null;
                settings.LastTestError = null;
            }

            settings.UpdatedAt = SiteTime.Now;
            return true;
        }, cancellationToken);

        _logger.LogInformation("{User} lưu thiết lập email ({Gmail}, bật: {Enabled})", User.Identity?.Name, gmail, Input.Enabled);

        Message = protectedPassword is not null
            ? "Đã lưu. Bấm \"Gửi thư thử\" ở bước 5 để chắc chắn Gmail nhận mật khẩu."
            : "Đã lưu thiết lập email.";

        return RedirectToPage();
    }

    /// <summary>Gửi thư thử ngay (không qua hàng đợi) để thấy kết quả tại chỗ.</summary>
    public async Task<IActionResult> OnPostGuiThuAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken);

        if (!Settings.IsReady)
        {
            TestError = "Chưa bật gửi email, hoặc chưa có địa chỉ Gmail / mật khẩu ứng dụng. Làm bước 4 trước.";
            return RedirectToPage();
        }

        var body = $"Chào bạn,\n\nĐây là thư thử từ trang quản trị {SiteName}. Nhận được thư này nghĩa là site đã gửi email được qua Gmail {Settings.GmailAddress}.\n\n" +
                   "Từ giờ site sẽ tự gửi:\n" +
                   (Settings.SendReceipt ? "- thư xác nhận cho người đăng ký lớp online,\n" : string.Empty) +
                   (Settings.NotifyOwner ? $"- thư báo đơn mới về {Settings.OwnerAddress},\n" : string.Empty) +
                   "- thư báo kết quả khi bạn duyệt đơn (nếu tích ô gửi email).\n\n" +
                   "Nếu thư này nằm trong mục Spam, hãy bấm \"Không phải thư rác\" để các thư sau vào Hộp thư đến.\n";

        var error = await _sender.SendAsync(new EmailMessage(Settings.OwnerAddress, $"[{SiteName}] Thư thử — gửi email đã chạy", body), cancellationToken);

        await _sender.Settings.UpdateAsync(settings =>
        {
            settings.LastTestAt = SiteTime.Now;
            settings.LastTestError = error;
            return true;
        }, cancellationToken);

        if (error is null)
        {
            Message = $"Đã gửi thư thử tới {Settings.OwnerAddress}. Mở hộp thư kiểm tra (xem cả mục Spam).";
        }
        else
        {
            TestError = error;
        }

        return RedirectToPage();
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        Settings = await _sender.Settings.ReadAsync(cancellationToken);
        SiteName = (await _content.Site.ReadAsync(cancellationToken)).Name;
    }
}
