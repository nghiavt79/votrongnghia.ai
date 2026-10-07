using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace VoTrongNghia.Pages.Admin;

/// <summary>
/// Đăng xuất chỉ nhận POST. Một đường dẫn đăng xuất mở bằng GET thì bất kỳ trang
/// nào cũng đá được người quản trị ra ngoài chỉ bằng một thẻ img trỏ tới nó.
/// </summary>
public class DangXuatModel : PageModel
{
    public IActionResult OnGet() => RedirectToPage("/Admin/Index");

    public async Task<IActionResult> OnPostAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return RedirectToPage("/Admin/DangNhap");
    }
}
