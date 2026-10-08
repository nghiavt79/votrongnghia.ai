using VoTrongNghia.Models;
using VoTrongNghia.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;

namespace VoTrongNghia.Pages.HocVien;

/// <summary>
/// Trang link đăng nhập trỏ tới: <c>/hoc-vien/vao?ma=…</c>.
///
/// <para>Mở link (GET) chỉ kiểm mã và hiện nút "Vào trang học viên"; bấm nút (POST) mới dùng mã và
/// đăng nhập. Hai bước vì nhiều hộp thư (Outlook, bộ lọc thư công ty) tự mở trước mọi link trong thư
/// để quét — nếu GET dùng luôn mã thì máy quét đã tiêu mã trước khi người học kịp bấm.</para>
/// </summary>
[EnableRateLimiting("hoc-vien")]
public class VaoModel : PageModel
{
    private readonly LoginLinkService _links;
    private readonly LearnerStore _learners;
    private readonly LearnerSession _session;
    private readonly ILogger<VaoModel> _logger;

    public VaoModel(LoginLinkService links, LearnerStore learners, LearnerSession session, ILogger<VaoModel> logger)
    {
        _links = links;
        _learners = learners;
        _session = session;
        _logger = logger;
    }

    /// <summary>Học viên của mã, null khi mã sai / hết hạn / đã dùng.</summary>
    public Learner? Learner { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync(string? ma, CancellationToken cancellationToken)
    {
        Learner = await FindUsableAsync(await _links.PeekAsync(ma, cancellationToken), cancellationToken);
        Code = ma ?? string.Empty;

        // Đang đăng nhập đúng người này rồi thì khỏi bấm thêm (mã để nguyên, tự hết hạn).
        if (Learner is not null && (await _session.CurrentAsync(HttpContext))?.Id == Learner.Id)
        {
            return RedirectToPage("/HocVien/Index");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? ma, CancellationToken cancellationToken)
    {
        var learner = await FindUsableAsync(await _links.ConsumeAsync(ma, cancellationToken), cancellationToken);

        if (learner is null)
        {
            Code = ma ?? string.Empty;
            return Page();
        }

        await _session.SignInAsync(HttpContext, learner);
        await _learners.TouchAsync(learner.Id, cancellationToken);
        _logger.LogInformation("Học viên {Id} đăng nhập bằng link email", learner.Id);

        return RedirectToPage("/HocVien/Index");
    }

    private async Task<Learner?> FindUsableAsync(string? learnerId, CancellationToken cancellationToken) =>
        await _learners.FindAsync(learnerId, cancellationToken) is { Status: not Models.Learner.StatusLocked } learner ? learner : null;
}
