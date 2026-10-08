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
    private readonly LessonStatsStore _stats;
    private readonly LearnerStore _learners;

    public IndexModel(SiteContent content, RegistrationStore registrations, ContentPaths paths, EmailSender email, LessonStatsStore stats, LearnerStore learners)
    {
        _learners = learners;
        _stats = stats;
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

    /// <summary>Tháng này (thống kê ẩn danh): lượt bắt đầu một khóa, lượt học xong khóa đầu vào.</summary>
    public int StartedThisMonth { get; private set; }
    public int GateFinishedThisMonth { get; private set; }
    public string GateCourseTitle { get; private set; } = string.Empty;

    /// <summary>Học viên đang học, và số người trong đó lâu không học (cần nhắc).</summary>
    public int ActiveLearners { get; private set; }
    public int IdleLearners { get; private set; }

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

        var month = (await _stats.ReadAsync(cancellationToken)).Months.GetValueOrDefault(LessonStatsStore.MonthKey(SiteTime.Now)) ?? [];
        int Count(Course course, Lesson lesson, bool completions) =>
            month.GetValueOrDefault($"{course.Slug}/{lesson.Slug}") is { } counter ? (completions ? counter.Completions : counter.Opens) : 0;

        StartedThisMonth = courses.Where(course => course.Lessons.Count > 0).Sum(course => Count(course, course.Lessons[0], false));

        if (courses.FirstOrDefault(course => course.Slug == live.RequireCourse && course.Lessons.Count > 0) is { } gate)
        {
            GateCourseTitle = gate.Title;
            GateFinishedThisMonth = Count(gate, gate.Lessons[^1], true);
        }
        Upcoming = live.Upcoming(SiteTime.Today).ToList();

        var learners = await _learners.Learners.ReadAsync(cancellationToken);
        ActiveLearners = learners.Count(item => item.Status == Learner.StatusActive);
        IdleLearners = learners.Count(LearnerStore.IsIdle);

        // Việc cần làm, máy tự đọc từ dữ liệu. Làm xong thì dòng đó tự biến mất.
        var todos = new List<Todo>();

        if (!site.ShowContact)
        {
            todos.Add(new("Chưa có email hay Zalo liên hệ: mục Liên hệ đang ẩn, người học không có cách hỏi bạn.", "/Admin/ThongTin", true));
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

        if (site.FacebookGroup.Length == 0 && site.ZaloGroup.Length == 0)
        {
            todos.Add(new("Chưa có nhóm Facebook / nhóm Zalo: mục Cộng đồng chỉ có nút đăng ký học.", "/Admin/ThongTin", false));
        }

        if (live.RequireCourse.Length > 0 && courses.All(course => course.Slug != live.RequireCourse))
        {
            todos.Add(new("Khóa bắt buộc trước khi đăng ký không còn (hoặc chưa đăng) — form đăng ký đang mở cho mọi người.", "/Admin/LopOnline", true));
        }

        if (Upcoming.Count == 0)
        {
            todos.Add(new("Chưa có lịch buổi học nào sắp tới: trang chủ đang hiện \"Sắp khai giảng\".", "/Admin/LopOnline", false));
        }

        var expired = registrations.Count(RegistrationStore.IsExpired);

        if (expired > 0)
        {
            todos.Add(new($"{expired} đơn đăng ký quá {RegistrationStore.RetentionMonths} tháng — chính sách dữ liệu cam kết ẩn danh hoá.", "/Admin/DangKy/Index", true));
        }

        var expiredLearners = learners.Count(LearnerStore.IsExpired);

        if (expiredLearners > 0)
        {
            todos.Add(new($"{expiredLearners} học viên không hoạt động quá {RegistrationStore.RetentionMonths} tháng — chính sách dữ liệu cam kết ẩn danh hoá.", "/Admin/HocVien/Index", true));
        }

        if (IdleLearners > 0)
        {
            todos.Add(new($"{IdleLearners} học viên không học quá {LearnerStore.IdleDays} ngày — gửi thư nhắc.", "/Admin/HocVien/Index", false));
        }

        var unframed = courses.Sum(course => course.Lessons.Count(lesson => !lesson.HasFramework));

        if (unframed > 0)
        {
            todos.Add(new($"{unframed} bài học chưa có \"Học xong làm được gì\" hoặc bài tập thực hành.", "/Admin/KhoaHoc/Index", false));
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
