using System.IO.Compression;
using VoTrongNghia.Models;
using VoTrongNghia.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace VoTrongNghia.Pages.Admin;

public class IndexModel : PageModel
{
    private readonly SiteContent _content;
    private readonly RegistrationStore _registrations;
    private readonly ContentPaths _paths;
    private readonly EmailSender _email;

    public IndexModel(SiteContent content, RegistrationStore registrations, ContentPaths paths, EmailSender email)
    {
        _content = content;
        _registrations = registrations;
        _paths = paths;
        _email = email;
    }

    [TempData]
    public string? Message { get; set; }

    public IReadOnlyList<string> Unwritable { get; private set; } = [];
    public IReadOnlyList<Registration> Latest { get; private set; } = [];
    public int NewCount { get; private set; }
    public int TotalRegistrations { get; private set; }
    public int PublishedPosts { get; private set; }
    public int DraftPosts { get; private set; }
    public int PublishedCourses { get; private set; }
    public int LessonCount { get; private set; }
    public IReadOnlyList<LiveSession> Upcoming { get; private set; } = [];

    /// <summary>Một việc còn thiếu: câu mô tả, trang /cms để làm, có bắt buộc trước khi mở đăng ký không.</summary>
    public sealed record Todo(string Text, string Page, bool Important);

    public IReadOnlyList<Todo> Todos { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Unwritable = _paths.FindUnwritableDirectories();

        var registrations = await _registrations.Registrations.ReadAsync(cancellationToken);
        TotalRegistrations = registrations.Count;
        NewCount = registrations.Count(item => item.Status == Registration.StatusNew);
        Latest = registrations.Take(5).ToList();

        var posts = await _content.Posts.ReadAsync(cancellationToken);
        PublishedPosts = posts.Count(post => post.Published);
        DraftPosts = posts.Count - PublishedPosts;

        var courses = await _content.PublishedCoursesAsync(cancellationToken);
        PublishedCourses = courses.Count;
        LessonCount = courses.Sum(course => course.Lessons.Count);

        var site = await _content.Site.ReadAsync(cancellationToken);
        var live = await _content.Live.ReadAsync(cancellationToken);
        Upcoming = live.Upcoming(SiteTime.Today).ToList();

        // Việc cần làm, máy tự đọc từ dữ liệu. Làm xong thì dòng đó tự biến mất.
        var todos = new List<Todo>();

        if (site.Email.Length == 0)
        {
            todos.Add(new("Chưa có email liên hệ: mục Liên hệ đang ẩn, người học không có cách hỏi bạn.", "/Admin/ThongTin", true));
        }

        var email = await _email.Settings.ReadAsync(cancellationToken);

        if (!email.IsReady)
        {
            todos.Add(new("Chưa thiết lập Gmail: người đăng ký không nhận được thư xác nhận, bạn không được báo khi có đơn mới.", "/Admin/Email", true));
        }
        else if (email.LastTestAt is null || email.LastTestError is not null)
        {
            todos.Add(new("Gmail đã thiết lập nhưng chưa gửi thư thử thành công.", "/Admin/Email", true));
        }

        if (Upcoming.Any(session => session.MeetingLink.Length == 0))
        {
            todos.Add(new("Có buổi học sắp tới chưa có link phòng học — thư báo duyệt sẽ hẹn gửi link sau.", "/Admin/LopOnline", false));
        }

        if (site.Avatar.EndsWith("avatar.svg", StringComparison.OrdinalIgnoreCase))
        {
            todos.Add(new("Ảnh đại diện vẫn là chữ \"VN\" tạm — tải ảnh thật ở Thông tin trang.", "/Admin/ThongTin", false));
        }

        if (site.Facebook.Length == 0 && site.FacebookGroup.Length == 0)
        {
            todos.Add(new("Chưa có link Facebook / nhóm Facebook: mục Cộng đồng chỉ có nút đăng ký học.", "/Admin/ThongTin", false));
        }

        if (live.RequireCourse.Length > 0 && courses.All(course => course.Slug != live.RequireCourse))
        {
            todos.Add(new("Khóa bắt buộc trước khi đăng ký không còn (hoặc chưa đăng) — form đăng ký đang mở cho mọi người.", "/Admin/LopOnline", true));
        }

        if (Upcoming.Count == 0)
        {
            todos.Add(new("Chưa có lịch buổi học nào sắp tới: trang chủ đang hiện \"Sắp khai giảng\".", "/Admin/LopOnline", false));
        }

        if (DraftPosts > 0)
        {
            todos.Add(new($"Còn {DraftPosts} bài viết nháp chưa đăng.", "/Admin/BaiViet/Index", false));
        }

        Todos = todos;
    }

    /// <summary>
    /// Tải bản sao lưu nội dung (site, bài viết, khóa học, lớp online) thành một file zip.
    /// Không gồm đơn đăng ký — đó là dữ liệu cá nhân, xuất riêng ở trang Đơn đăng ký khi cần.
    /// Chép các file này vào Data/seed/ khi muốn seed phản ánh nội dung thật trên máy chủ.
    /// </summary>
    public IActionResult OnGetSaoLuu()
    {
        var stream = new MemoryStream();

        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var file in _paths.ContentFiles.Where(System.IO.File.Exists))
            {
                zip.CreateEntryFromFile(file, Path.GetFileName(file));
            }
        }

        stream.Position = 0;
        return File(stream, "application/zip", $"votrongnghia-noi-dung-{SiteTime.Now:yyyyMMdd-HHmm}.zip");
    }
}
