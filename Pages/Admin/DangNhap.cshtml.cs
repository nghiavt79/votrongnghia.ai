using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using VoTrongNghia.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;

namespace VoTrongNghia.Pages.Admin;

[EnableRateLimiting("dang-nhap")]
public class DangNhapModel : PageModel
{
    private readonly AdminAccountStore _accounts;
    private readonly ILogger<DangNhapModel> _logger;

    public DangNhapModel(AdminAccountStore accounts, ILogger<DangNhapModel> logger)
    {
        _accounts = accounts;
        _logger = logger;
    }

    [BindProperty]
    [Required(ErrorMessage = "Nhập tên đăng nhập.")]
    public string UserName { get; set; } = string.Empty;

    [BindProperty]
    [Required(ErrorMessage = "Nhập mật khẩu.")]
    public string Password { get; set; } = string.Empty;

    public bool HasAccount { get; private set; }

    /// <summary>
    /// Ô của biểu mẫu tạo tài khoản lần đầu. Chỉ dùng khi máy chủ chưa có tài
    /// khoản nào; lúc đó trang này hiện biểu mẫu tạo thay cho biểu mẫu đăng nhập.
    /// </summary>
    // Khai nullable có chủ ý. Kiểu string không nullable thì ASP.NET tự gắn luật
    // "bắt buộc nhập", mà ba ô này KHÔNG có mặt trong biểu mẫu đăng nhập — hậu
    // quả là mỗi lần đăng nhập đều trượt với ba dòng lỗi về ô người dùng không
    // hề thấy. Đã dính đúng bẫy đó một lần.
    [BindProperty]
    public string? NewUserName { get; set; }

    [BindProperty]
    public string? NewPassword { get; set; }

    [BindProperty]
    public string? ConfirmPassword { get; set; }

    public static int MinimumPasswordLength => AdminAccountStore.MinimumPasswordLength;

    /// <summary>
    /// Dựng cookie đăng nhập. Trang đổi mật khẩu gọi lại hàm này để cấp cookie
    /// mang dấu bảo mật mới, nếu không chính người vừa đổi mật khẩu sẽ bị đá ra.
    /// </summary>
    public static Task SignInAsync(HttpContext context, AdminAccount account)
    {
        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Name, account.UserName),
                new Claim(AdminAccountStore.SecurityStampClaim, account.SecurityStamp)
            ],
            CookieAuthenticationDefaults.AuthenticationScheme);

        return context.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));
    }

    public IActionResult OnGet(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToPage("/Admin/Index");
        }

        HasAccount = _accounts.HasAccount;

        return Page();
    }

    /// <summary>
    /// Tạo tài khoản đầu tiên, ngay trên trình duyệt.
    ///
    /// <para>Trước đây việc này phải làm bằng lệnh <c>--dat-mat-khau</c> trên máy
    /// chủ. Bỏ đi vì mục tiêu là publish lên là chạy: người dựng site không phải
    /// mở dòng lệnh, không phải nhớ cú pháp.</para>
    ///
    /// <para>Chỉ chạy được khi máy chủ chưa có tài khoản nào. Có tài khoản rồi thì
    /// handler này trả người gọi về trang đăng nhập, kể cả khi họ tự gửi POST.</para>
    /// </summary>
    public async Task<IActionResult> OnPostTaoTaiKhoanAsync(CancellationToken cancellationToken)
    {
        HasAccount = _accounts.HasAccount;

        if (HasAccount)
        {
            return RedirectToPage();
        }

        // Biểu mẫu đăng nhập không gửi lên ở bước này, nên dọn lỗi "chưa nhập tên
        // đăng nhập" của nó đi trước khi kiểm phần thật.
        ModelState.Clear();

        var userName = (NewUserName ?? string.Empty).Trim();
        var password = NewPassword ?? string.Empty;

        if (userName.Length < 3)
        {
            ModelState.AddModelError("NewUserName", "Tên đăng nhập phải từ 3 ký tự trở lên.");
        }

        if (password.Length < AdminAccountStore.MinimumPasswordLength)
        {
            ModelState.AddModelError(
                "NewPassword",
                $"Mật khẩu phải từ {AdminAccountStore.MinimumPasswordLength} ký tự trở lên.");
        }

        if (!string.Equals(password, ConfirmPassword, StringComparison.Ordinal))
        {
            ModelState.AddModelError("ConfirmPassword", "Hai lần nhập mật khẩu không giống nhau.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        AdminAccount account;

        try
        {
            account = await _accounts.SetPasswordAsync(userName, password, cancellationToken);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Gần như luôn là app pool chưa có quyền ghi. Nói thẳng ra thay vì để
            // trang lỗi 500 trắng.
            ModelState.AddModelError(
                string.Empty,
                "Không ghi được tài khoản xuống đĩa. Tài khoản Windows chạy website " +
                $"({ContentPaths.ProcessIdentity}) cần quyền sửa trên thư mục dữ liệu của site.");
            _logger.LogError(ex, "Không tạo được tài khoản quản trị đầu tiên");

            return Page();
        }

        await SignInAsync(HttpContext, account);
        _logger.LogInformation("Đã tạo tài khoản quản trị đầu tiên: {UserName}", account.UserName);

        return RedirectToPage("/Admin/Index");
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl, CancellationToken cancellationToken)
    {
        HasAccount = _accounts.HasAccount;

        // Chưa có tài khoản nào thì trang này không phải chỗ đăng nhập, và biểu
        // mẫu đăng nhập cũng không hiện ra. Chặn sớm cho chắc.
        if (!HasAccount)
        {
            return RedirectToPage();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var account = await _accounts.VerifyAsync(UserName, Password, cancellationToken);

        if (account is null)
        {
            // Một câu chung cho cả sai tên lẫn sai mật khẩu. Tách ra hai câu là
            // nói cho người dò biết họ đã đoán trúng tên tài khoản.
            ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không đúng.");
            _logger.LogWarning(
                "Đăng nhập quản trị thất bại cho {UserName} từ {Ip}",
                UserName,
                HttpContext.Connection.RemoteIpAddress);

            return Page();
        }

        await SignInAsync(HttpContext, account);

        _logger.LogInformation("{UserName} đã đăng nhập trang quản trị", account.UserName);

        // IsLocalUrl chặn ?returnUrl=https://site-khac — không có nó thì trang
        // đăng nhập thành bàn đạp đẩy khách sang chỗ lạ.
        return Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : RedirectToPage("/Admin/Index");
    }
}
