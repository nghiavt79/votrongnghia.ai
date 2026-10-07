using VoTrongNghia.Models;
using VoTrongNghia.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace VoTrongNghia.Pages.Admin;

/// <summary>
/// Thông tin chung của trang, <c>Data/site.json</c>. Các danh sách (hành trình, công cụ, video)
/// sửa theo dòng: thêm thì điền vào dòng trống cuối bảng, bỏ thì xoá trắng ô tên. Không có
/// nút thêm/xoá bằng JS — tắt JS vẫn sửa được hết.
/// </summary>
[RequestSizeLimit(64 * 1024 * 1024)]
public class ThongTinModel : PageModel
{
    /// <summary>Số dòng trống thêm vào cuối mỗi danh sách để gõ mục mới.</summary>
    public const int BlankRows = 3;

    private readonly SiteContent _content;
    private readonly IWebHostEnvironment _environment;
    private readonly ImageService _images;

    public ThongTinModel(SiteContent content, IWebHostEnvironment environment, ImageService images)
    {
        _content = content;
        _environment = environment;
        _images = images;
    }

    [BindProperty]
    public SiteInfo Input { get; set; } = new();

    [TempData]
    public string? Message { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var site = await _content.Site.ReadAsync(cancellationToken);

        // Bản sao để thêm dòng trống: đối tượng ReadAsync trả về dùng chung giữa các request, không được sửa.
        Input = System.Text.Json.JsonSerializer.Deserialize<SiteInfo>(System.Text.Json.JsonSerializer.Serialize(site))!;
        AddBlankRows();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        string Clean(string value) => value.Trim();

        Input.Name = Clean(Input.Name);
        Input.Avatar = Clean(Input.Avatar).TrimStart('/');
        Input.Story = Input.Story.Where(item => item.Title.Trim().Length > 0)
            .Select(item => new StoryStep { Icon = Clean(item.Icon), Title = Clean(item.Title), Description = Clean(item.Description) }).ToList();
        Input.Tools = Input.Tools.Where(item => item.Name.Trim().Length > 0)
            .Select(item => new ToolLink { Name = Clean(item.Name), Description = Clean(item.Description), Icon = Clean(item.Icon), Url = Clean(item.Url) }).ToList();
        Input.Videos = Input.Videos.Where(item => item.YoutubeId.Trim().Length > 0)
            .Select(item => new VideoLink { Title = Clean(item.Title), YoutubeId = YoutubeIdOf(Clean(item.YoutubeId)) }).ToList();

        if (Input.Name.Length == 0)
        {
            ModelState.AddModelError("Input.Name", "Nhập tên hiện trên trang.");
        }

        if (Input.Avatar.Length == 0 || !System.IO.File.Exists(Path.Combine(_environment.WebRootPath, Input.Avatar)))
        {
            ModelState.AddModelError("Input.Avatar", $"Không thấy ảnh \"{Input.Avatar}\" trong wwwroot. Chép ảnh vào wwwroot/assets/img/ rồi điền đường dẫn, ví dụ assets/img/nghia.jpg.");
        }

        if (Input.Email.Length > 0 && !Input.Email.Contains('@'))
        {
            ModelState.AddModelError("Input.Email", "Email chưa đúng.");
        }

        // Link mạng xã hội phải là link thật — link sai hay link mẫu kiểu "your-profile" mà hiện ra
        // thì người đọc bấm vào trang lỗi.
        foreach (var (field, value) in new[]
                 {
                     ("Youtube", Input.Youtube), ("Facebook", Input.Facebook), ("FacebookGroup", Input.FacebookGroup),
                     ("Tiktok", Input.Tiktok), ("Linkedin", Input.Linkedin), ("X", Input.X), ("Threads", Input.Threads),
                     ("Paypal", Input.Paypal)
                 })
        {
            if (value.Trim().Length > 0 && !value.Trim().StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("Input." + field, $"Link {field} phải bắt đầu bằng https://");
            }
            else if (value.Contains("your-", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("Input." + field, $"Link {field} vẫn là link mẫu — điền link thật hoặc để trống.");
            }
        }

        if (Input.Tools.Any(tool => !tool.Url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
        {
            ModelState.AddModelError(string.Empty, "Link công cụ AI phải bắt đầu bằng https://");
        }

        if (Input.Videos.Any(video => video.YoutubeId.Length != 11))
        {
            ModelState.AddModelError(string.Empty, "Có video không đọc được mã — dán link YouTube hoặc mã 11 ký tự.");
        }

        if (Input.Donate && Input.BankNumber.Trim().Length == 0 && Input.Paypal.Trim().Length == 0)
        {
            ModelState.AddModelError("Input.Donate", "Bật mục Ủng hộ thì cần số tài khoản hoặc link PayPal.");
        }

        if (!ModelState.IsValid)
        {
            AddBlankRows();
            return Page();
        }

        var input = Input;

        await _content.Site.UpdateAsync(site =>
        {
            site.Name = input.Name;
            site.Title = input.Title.Trim();
            site.Badge = input.Badge.Trim();
            site.HeroLead = input.HeroLead.Trim();
            site.HeroDescription = input.HeroDescription.Trim();
            site.Bio = input.Bio.Trim();
            site.Avatar = input.Avatar;
            site.Email = input.Email.Trim();
            site.Youtube = input.Youtube.Trim();
            site.Facebook = input.Facebook.Trim();
            site.FacebookGroup = input.FacebookGroup.Trim();
            site.Tiktok = input.Tiktok.Trim();
            site.Linkedin = input.Linkedin.Trim();
            site.X = input.X.Trim();
            site.Threads = input.Threads.Trim();
            site.BankName = input.BankName.Trim();
            site.BankNumber = input.BankNumber.Trim();
            site.BankOwner = input.BankOwner.Trim();
            site.Paypal = input.Paypal.Trim();
            site.Donate = input.Donate;
            site.Story = input.Story;
            site.Tools = input.Tools;
            site.Videos = input.Videos;
            return true;
        }, cancellationToken);

        Message = "Đã lưu thông tin trang. Tải lại trang chủ là thấy.";
        return RedirectToPage();
    }

    /// <summary>Tải ảnh đại diện mới: cắt vuông, thay ảnh cũ, xoá file cũ nếu là ảnh đã tải lên.</summary>
    public async Task<IActionResult> OnPostAnhDaiDienAsync(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            TempData["Error"] = "Chưa chọn ảnh nào.";
            return RedirectToPage();
        }

        string url;

        try
        {
            url = await _images.AddAsync(ImageService.Kind.Avatar, file, cancellationToken);
        }
        catch (ImageUploadException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToPage();
        }

        string? old = null;

        await _content.Site.UpdateAsync(site =>
        {
            old = site.Avatar;
            site.Avatar = url;
            return true;
        }, cancellationToken);

        _images.Delete(old);
        Message = "Đã thay ảnh đại diện. Tải lại trang chủ là thấy.";

        return RedirectToPage();
    }

    private void AddBlankRows()
    {
        for (var i = 0; i < BlankRows; i++)
        {
            Input.Story.Add(new StoryStep());
            Input.Tools.Add(new ToolLink());
            Input.Videos.Add(new VideoLink());
        }
    }

    /// <summary>Nhận cả link YouTube lẫn mã video, trả về mã 11 ký tự (hoặc nguyên chữ nếu không đọc được).</summary>
    private static string YoutubeIdOf(string value)
    {
        var match = System.Text.RegularExpressions.Regex.Match(value, @"(?:v=|youtu\.be/|embed/|shorts/)([A-Za-z0-9_-]{11})");
        return match.Success ? match.Groups[1].Value : value;
    }
}
