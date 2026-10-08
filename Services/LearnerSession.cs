using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using VoTrongNghia.Models;

namespace VoTrongNghia.Services;

/// <summary>
/// Phiên đăng nhập của học viên: cookie <c>vn.hocvien</c>, scheme riêng tách hẳn cookie quản trị.
///
/// <para>Scheme mặc định của app vẫn là cookie quản trị, nên ở mọi trang công khai
/// <c>User.Identity.IsAuthenticated</c> vẫn nghĩa là "người quản trị" (trang bài viết / khóa học dùng
/// điều này để cho xem bản nháp). Học viên không bao giờ được gán vào <c>HttpContext.User</c> ở
/// trang công khai — lấy học viên qua <see cref="CurrentAsync"/>. Chỉ các trang trong
/// <c>Pages/HocVien</c> (chính sách <see cref="Policy"/>) mới có <c>User</c> là học viên.</para>
/// </summary>
public sealed class LearnerSession
{
    public const string Scheme = "HocVien";
    public const string Policy = "HocVien";
    public const string CookieName = "vn.hocvien";
    public const string IdClaim = "vn:hv";
    public const string StampClaim = "vn:hv-stamp";

    /// <summary>Tên header JS gửi kèm khi ghi tiến độ (mã chống giả, xem <see cref="RequestToken"/>).</summary>
    public const string TokenHeader = "X-Hoc-Vien-Token";

    private static readonly object ItemKey = new();

    private readonly LearnerStore _learners;
    private readonly IDataProtector _protector;

    public LearnerSession(LearnerStore learners, IDataProtectionProvider protection)
    {
        _learners = learners;
        _protector = protection.CreateProtector("VoTrongNghia.HocVien.RequestToken");
    }

    /// <summary>
    /// Học viên đang đăng nhập trên request này, hoặc null. Cookie đã bị kiểm dấu bảo mật và tài
    /// khoản bị khoá ở <c>OnValidatePrincipal</c> (Program.cs). Nhớ kết quả trong request: layout và trang cùng hỏi.
    /// </summary>
    public async Task<Learner?> CurrentAsync(HttpContext context)
    {
        if (context.Items.TryGetValue(ItemKey, out var cached))
        {
            return cached as Learner;
        }

        Learner? learner = null;

        // Không có cookie thì khỏi chạy cả bộ xác thực — đa số khách là người học tự do.
        if (context.Request.Cookies.ContainsKey(CookieName))
        {
            var result = await context.AuthenticateAsync(Scheme);

            if (result.Succeeded)
            {
                learner = await _learners.FindAsync(result.Principal.FindFirst(IdClaim)?.Value, context.RequestAborted);
            }
        }

        context.Items[ItemKey] = learner;
        return learner;
    }

    public Task SignInAsync(HttpContext context, Learner learner)
    {
        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Name, learner.Name),
                new Claim(IdClaim, learner.Id),
                new Claim(StampClaim, learner.SecurityStamp)
            ],
            Scheme);

        return context.SignInAsync(Scheme, new ClaimsPrincipal(identity), new AuthenticationProperties { IsPersistent = true });
    }

    public Task SignOutAsync(HttpContext context) => context.SignOutAsync(Scheme);

    /// <summary>
    /// Mã chống giả cho API ghi tiến độ, in vào trang cho JS gửi lại qua header.
    ///
    /// <para>Không dùng mã antiforgery của ASP.NET: mã đó gắn với <c>HttpContext.User</c>, mà ở trang
    /// bài học <c>User</c> là khách / quản trị còn ở API là học viên — hai bên không khớp. Mã này gắn
    /// với học viên và dấu bảo mật, mã hoá bằng Data Protection: trang của site khác không đọc được
    /// trang mình nên không lấy được mã, không giả được request.</para>
    /// </summary>
    public string RequestToken(Learner learner) => _protector.Protect($"{learner.Id}|{learner.SecurityStamp}");

    public bool IsValidRequest(HttpContext context, Learner learner)
    {
        var header = context.Request.Headers[TokenHeader].ToString();

        if (header.Length == 0)
        {
            return false;
        }

        try
        {
            return _protector.Unprotect(header) == $"{learner.Id}|{learner.SecurityStamp}";
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            return false;
        }
    }
}
