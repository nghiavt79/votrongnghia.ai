namespace VoTrongNghia.Models;

/// <summary>
/// Một học viên lớp online, <c>Data/hoc-vien.json</c> (giai đoạn 2 của docs/ke-hoach-hoc-vien.md).
/// Dữ liệu cá nhân: ngoài git, ngoài bản publish, không có seed.
///
/// <para>Tài khoản chỉ sinh ra khi người quản trị duyệt đơn (hoặc thêm tay ở <c>/cms/hoc-vien</c>) —
/// người lạ không tự tạo được. Không có mật khẩu: đăng nhập bằng link gửi qua email.</para>
/// </summary>
public sealed class Learner
{
    public const string StatusActive = "dang-hoc";
    public const string StatusPaused = "tam-dung";
    public const string StatusFinished = "hoan-thanh";
    public const string StatusLocked = "khoa";

    /// <summary>Tên hiện của từng trạng thái, theo thứ tự hiện ở bộ lọc.</summary>
    public static readonly IReadOnlyList<(string Key, string Label, string Badge)> Statuses =
    [
        (StatusActive, "Đang học", "text-bg-success"),
        (StatusPaused, "Tạm dừng", "text-bg-warning"),
        (StatusFinished, "Hoàn thành", "text-bg-primary"),
        (StatusLocked, "Đã khoá", "text-bg-secondary"),
    ];

    /// <summary>Mã cố định, dạng <c>hv-7f3a9c</c>. Cookie đăng nhập mang mã này, không mang email.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Viết thường. Là "tên đăng nhập": link đăng nhập gửi tới địa chỉ này.</summary>
    public string Email { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;

    /// <summary>Các đơn đăng ký đã duyệt của người này — buổi học và link phòng học lấy từ đây.</summary>
    public List<string> RegistrationCodes { get; set; } = [];

    public string Status { get; set; } = StatusActive;

    /// <summary>
    /// Bài đã học xong: khoá <c>"khóa/bài"</c> (cùng khoá với <c>localStorage</c> trên máy người học),
    /// giá trị là lúc đánh dấu. Lưu thời điểm chứ không chỉ đúng/sai để biết ai bỏ dở bao lâu.
    /// </summary>
    public Dictionary<string, DateTimeOffset> Progress { get; set; } = [];

    /// <summary>Lần đăng nhập hoặc đánh dấu bài gần nhất.</summary>
    public DateTimeOffset? LastSeenAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Ghi chú riêng của người quản trị, học viên không thấy.</summary>
    public string AdminNote { get; set; } = string.Empty;

    /// <summary>
    /// Đổi khi khoá tài khoản hoặc ẩn danh hoá. Cookie học viên mang dấu này: đổi dấu là mọi phiên
    /// đang mở ở mọi máy bị từ chối ngay ở request kế tiếp (như dấu bảo mật của tài khoản quản trị).
    /// </summary>
    public string SecurityStamp { get; set; } = string.Empty;

    /// <summary>Thư đã gửi riêng cho học viên (link đăng nhập, thư tự soạn), cũ trước.</summary>
    public List<EmailLogEntry> Emails { get; set; } = [];

    public string StatusLabel => Statuses.FirstOrDefault(item => item.Key == Status).Label ?? Status;
    public string StatusBadge => Statuses.FirstOrDefault(item => item.Key == Status).Badge ?? "text-bg-light";

    public DateTimeOffset LastActivity => LastSeenAt ?? CreatedAt;
}

/// <summary>
/// Một link đăng nhập đang chờ dùng, <c>Data/dang-nhap.json</c>. Chỉ lưu băm SHA-256 của mã: ai đọc
/// được file cũng không dựng lại được link.
/// </summary>
public sealed class LoginToken
{
    /// <summary>SHA-256 của mã, dạng hex.</summary>
    public string Hash { get; set; } = string.Empty;

    public string LearnerId { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}
