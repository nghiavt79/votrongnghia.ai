using System.Text.RegularExpressions;
using VoTrongNghia.Models;
using VoTrongNghia.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;

namespace VoTrongNghia.Pages.HocVien;

/// <summary>
/// Xin link đăng nhập trang học viên: nhập email, nhận link qua thư.
///
/// <para>Luôn trả cùng một câu "nếu email này là học viên, link đã được gửi" dù email có tài khoản
/// hay không — không cho người lạ dò ra ai đang học lớp online. Thư đi qua hàng đợi nền nên thời
/// gian phản hồi cũng không khác nhau theo việc có gửi thư hay không.</para>
/// </summary>
[EnableRateLimiting("hoc-vien")]
public partial class DangNhapModel : PageModel
{
    private readonly LearnerSession _session;
    private readonly LearnerStore _learners;
    private readonly LoginLinkService _links;
    private readonly EmailSender _email;
    private readonly EmailQueue _queue;
    private readonly SiteContent _content;
    private readonly string _baseUrl;
    private readonly ILogger<DangNhapModel> _logger;

    public DangNhapModel(LearnerSession session, LearnerStore learners, LoginLinkService links, EmailSender email,
        EmailQueue queue, SiteContent content, IConfiguration configuration, ILogger<DangNhapModel> logger)
    {
        _session = session;
        _learners = learners;
        _links = links;
        _email = email;
        _queue = queue;
        _content = content;
        _baseUrl = (configuration["Site:BaseUrl"] ?? string.Empty).TrimEnd('/');
        _logger = logger;
    }

    [BindProperty]
    public string Email { get; set; } = string.Empty;

    public string? Error { get; private set; }

    /// <summary>Email vừa xin link, hiện ở câu "đã gửi". Qua TempData để email không nằm trên URL.</summary>
    [TempData]
    public string? SentTo { get; set; }

    /// <summary>Câu báo sau khi đăng xuất / xoá tài khoản / link hỏng.</summary>
    [TempData]
    public string? Message { get; set; }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailPattern();

    public async Task<IActionResult> OnGetAsync()
    {
        if (await _session.CurrentAsync(HttpContext) is not null)
        {
            return RedirectToPage("/HocVien/Index");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var email = LearnerStore.NormalizeEmail(Email);

        if (email.Length > 120 || !EmailPattern().IsMatch(email))
        {
            Error = "Email chưa đúng — nhập email bạn đã dùng khi đăng ký lớp.";
            return Page();
        }

        var learner = await _learners.FindByEmailAsync(email, cancellationToken);
        var settings = await _email.Settings.ReadAsync(cancellationToken);

        if (learner is null || learner.Status == Learner.StatusLocked)
        {
            _logger.LogInformation("Xin link đăng nhập học viên cho email không có tài khoản (hoặc đã khoá)");
        }
        else if (!settings.IsReady)
        {
            // Người học không biết chuyện này; người quản trị thấy ở bảng điều khiển và có nút
            // chép link gửi tay ở /cms/hoc-vien.
            _logger.LogWarning("Học viên {Id} xin link đăng nhập nhưng site chưa thiết lập Gmail", learner.Id);
        }
        else if (await _links.CreateAsync(learner.Id, LoginLinkService.ShortLifetime, limitRequests: true, cancellationToken) is { } token)
        {
            var siteName = (await _content.Site.ReadAsync(cancellationToken)).Name;
            var message = EmailTemplates.LoginLink(learner, LoginLinkService.Url(_baseUrl, token), LoginLinkService.ShortLifetime, siteName, _baseUrl, settings.OwnerAddress);
            _queue.Enqueue(message, EmailLogEntry.KindLogin, registrationCode: null, learnerId: learner.Id);
        }
        else
        {
            _logger.LogWarning("Học viên {Id} xin quá {Max} link trong {Minutes} phút", learner.Id, LoginLinkService.MaxRequestsPerWindow, LoginLinkService.RequestWindow.TotalMinutes);
        }

        SentTo = email;
        return RedirectToPage();
    }
}
