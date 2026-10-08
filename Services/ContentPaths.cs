namespace VoTrongNghia.Services;

/// <summary>
/// Nơi cất phần nội dung ghi được: thông tin trang, bài viết, khóa học, lớp online, đơn
/// đăng ký, tài khoản quản trị và bản sao lưu.
///
/// <para>Tất cả nằm ngay trong thư mục app — <c>Data/</c>. Thứ giữ cho deploy không đè
/// lên là <c>VoTrongNghia.csproj</c>: các đường dẫn đó khai <c>CopyToPublishDirectory="Never"</c>
/// nên bản publish không mang theo chúng.</para>
/// </summary>
public sealed class ContentPaths
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<ContentPaths> _logger;

    public ContentPaths(IWebHostEnvironment environment, ILogger<ContentPaths> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public string DataDirectory => Path.Combine(_environment.ContentRootPath, "Data");

    /// <summary>Dữ liệu gốc đi kèm bản publish, chỉ dùng để dựng máy chủ mới.</summary>
    public string SeedDirectory => Path.Combine(DataDirectory, "seed");

    public string SiteFile => Path.Combine(DataDirectory, "site.json");
    public string PostsFile => Path.Combine(DataDirectory, "posts.json");
    public string CoursesFile => Path.Combine(DataDirectory, "courses.json");
    public string LiveFile => Path.Combine(DataDirectory, "live.json");

    /// <summary>
    /// Đơn đăng ký học: họ tên, email, số điện thoại của người học. Ngoài git, ngoài bản
    /// publish, và nằm trong Data/ chứ không phải wwwroot nên không tải thẳng được.
    /// </summary>
    public string RegistrationsFile => Path.Combine(DataDirectory, "registrations.json");

    /// <summary>Thống kê ẩn danh lượt mở bài / học xong. Ngoài git, ngoài bản publish, không có seed.</summary>
    public string StatsFile => Path.Combine(DataDirectory, "thong-ke.json");

    /// <summary>Cấu hình SMTP (mật khẩu đã mã hoá). Ngoài git, ngoài bản publish, không có seed.</summary>
    public string EmailFile => Path.Combine(DataDirectory, "email.json");

    /// <summary>Tài khoản quản trị. Không bao giờ nằm trong git.</summary>
    public string AdminFile => Path.Combine(DataDirectory, "admin.json");

    public string BackupsDirectory => Path.Combine(DataDirectory, "backups");

    /// <summary>
    /// Ảnh tải lên qua /cms, phục vụ ra đường dẫn <c>uploads/…</c>. Nằm trong wwwroot để
    /// được phục vụ như file tĩnh, nhưng ngoài git và ngoài bản publish như dữ liệu sống.
    /// </summary>
    public string UploadsDirectory => Path.Combine(_environment.WebRootPath, "uploads");

    /// <summary>Thư mục app pool phải ghi được.</summary>
    public IReadOnlyList<string> WritableDirectories => [DataDirectory, BackupsDirectory, UploadsDirectory];

    /// <summary>Các file nội dung có seed, theo thứ tự hiện ở nút tải bản sao lưu.</summary>
    public IReadOnlyList<string> ContentFiles => [SiteFile, PostsFile, CoursesFile, LiveFile];

    /// <summary>
    /// Máy chủ mới tinh chưa có dữ liệu thì chép từ <c>Data/seed/</c> sang.
    ///
    /// <para>Chỉ chép khi bên đích chưa có file, nên gọi ở mỗi lần khởi động là vô
    /// hại. Không có bước này thì lần deploy đầu ra một site không có bài nào, vì bản
    /// publish cố tình không mang theo <c>Data/posts.json</c>.</para>
    /// </summary>
    public void EnsureInitialized()
    {
        foreach (var file in ContentFiles)
        {
            var seed = Path.Combine(SeedDirectory, Path.GetFileName(file));

            if (File.Exists(file) || !File.Exists(seed))
            {
                continue;
            }

            try
            {
                Directory.CreateDirectory(DataDirectory);
                File.Copy(seed, file);
                _logger.LogInformation("Đã chép {File} từ Data/seed", Path.GetFileName(file));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Thiếu quyền ghi: site vẫn chạy bằng cách đọc thẳng file seed (xem
                // JsonFileStore), còn bảng điều khiển sẽ hiện băng đỏ hướng dẫn cấp quyền.
                _logger.LogError(ex, "Không chép được {File} từ Data/seed", Path.GetFileName(file));
            }
        }
    }

    /// <summary>
    /// Thử ghi thật một file tạm vào từng thư mục, trả về những chỗ không ghi được.
    ///
    /// <para>Không đoán theo ACL mà ghi thử, vì quyền trên Windows còn phụ thuộc thừa
    /// kế, deny rule và cả thuộc tính chỉ đọc. Ghi thử một byte là câu trả lời chắc
    /// chắn duy nhất — và lỗi thiếu quyền hiện thành một câu tiếng Việt trên bảng
    /// điều khiển thay vì một trang lỗi 500 lúc bấm Lưu.</para>
    /// </summary>
    public IReadOnlyList<string> FindUnwritableDirectories()
    {
        var hong = new List<string>();

        foreach (var directory in WritableDirectories)
        {
            try
            {
                Directory.CreateDirectory(directory);

                var probe = Path.Combine(directory, $".ghi-thu-{Guid.NewGuid():N}.tmp");
                File.WriteAllText(probe, string.Empty);
                File.Delete(probe);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                hong.Add(directory);
            }
        }

        return hong;
    }

    /// <summary>
    /// Tài khoản Windows mà website đang chạy dưới đó, để câu hướng dẫn cấp quyền
    /// ghi đúng tên chứ không bắt người đọc tự tra.
    /// </summary>
    public static string ProcessIdentity
    {
        get
        {
            try
            {
                return OperatingSystem.IsWindows()
                    ? System.Security.Principal.WindowsIdentity.GetCurrent().Name
                    : Environment.UserName;
            }
            catch (Exception)
            {
                return Environment.UserName;
            }
        }
    }
}
