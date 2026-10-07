using System.ComponentModel.DataAnnotations;
using VoTrongNghia.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;

namespace VoTrongNghia.Pages.Admin;

/// <summary>
/// Đổi mật khẩu quản trị.
///
/// <para>Vẫn giới hạn số lần thử như trang đăng nhập: ô "mật khẩu hiện tại" cũng
/// là một chỗ đoán mật khẩu, và một máy tính bị bỏ quên lúc còn đăng nhập là đủ
/// để ai đó ngồi dò.</para>
/// </summary>
[EnableRateLimiting("dang-nhap")]
public class DoiMatKhauModel : PageModel
{
    private readonly AdminAccountStore _accounts;
    private readonly ILogger<DoiMatKhauModel> _logger;

    public DoiMatKhauModel(AdminAccountStore accounts, ILogger<DoiMatKhauModel> logger)
    {
        _accounts = accounts;
        _logger = logger;
    }

    public const int MinimumLength = AdminAccountStore.MinimumPasswordLength;

    [BindProperty]
    [Required(ErrorMessage = "Nhập mật khẩu hiện tại.")]
    public string CurrentPassword { get; set; } = string.Empty;

    [BindProperty]
    [Required(ErrorMessage = "Nhập mật khẩu mới.")]
    [StringLength(128, MinimumLength = MinimumLength,
        ErrorMessage = "Mật khẩu mới phải từ {2} ký tự trở lên.")]
    public string NewPassword { get; set; } = string.Empty;

    [BindProperty]
    [Required(ErrorMessage = "Nhập lại mật khẩu mới.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [TempData]
    public string? Message { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!string.Equals(NewPassword, ConfirmPassword, StringComparison.Ordinal))
        {
            ModelState.AddModelError("ConfirmPassword", "Hai lần nhập mật khẩu mới không giống nhau.");
        }

        if (string.Equals(NewPassword, CurrentPassword, StringComparison.Ordinal))
        {
            ModelState.AddModelError("NewPassword", "Mật khẩu mới trùng mật khẩu đang dùng.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var userName = User.Identity!.Name!;
        var account = await _accounts.ChangePasswordAsync(userName, CurrentPassword, NewPassword, cancellationToken);

        if (account is null)
        {
            ModelState.AddModelError("CurrentPassword", "Mật khẩu hiện tại không đúng.");
            _logger.LogWarning(
                "Đổi mật khẩu thất bại cho {UserName} từ {Ip}",
                userName,
                HttpContext.Connection.RemoteIpAddress);

            return Page();
        }

        // Đổi mật khẩu là đổi dấu bảo mật, nên cookie đang cầm trong tay cũng hết
        // hiệu lực. Cấp lại ngay để người vừa đổi không bị chính thao tác của mình
        // đá ra ngoài; các phiên khác thì rơi ở request kế tiếp, đúng như mong đợi.
        await DangNhapModel.SignInAsync(HttpContext, account);

        Message = "Đã đổi mật khẩu. Những máy khác đang mở trang quản trị sẽ phải đăng nhập lại.";

        return RedirectToPage("/Admin/Index");
    }
}
