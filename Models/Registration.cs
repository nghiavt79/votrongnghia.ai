namespace VoTrongNghia.Models;

/// <summary>
/// Một đơn đăng ký lớp học online, <c>Data/registrations.json</c>. Dữ liệu cá nhân: ngoài
/// git, ngoài bản publish. Nhận ở <c>/dang-ky</c>, duyệt ở <c>/cms/dang-ky</c>.
/// </summary>
public sealed class Registration
{
    public const string StatusNew = "moi";
    public const string StatusApproved = "duyet";
    public const string StatusWaiting = "cho";
    public const string StatusRejected = "tu-choi";

    /// <summary>Tên hiện của từng trạng thái, theo thứ tự hiện ở bộ lọc.</summary>
    public static readonly IReadOnlyList<(string Key, string Label, string Badge)> Statuses =
    [
        (StatusNew, "Mới", "text-bg-primary"),
        (StatusApproved, "Đã duyệt", "text-bg-success"),
        (StatusWaiting, "Chờ đợt sau", "text-bg-warning"),
        (StatusRejected, "Không phù hợp", "text-bg-secondary"),
    ];

    /// <summary>Mã đơn, dạng <c>DK261007-01</c>. Người đăng ký thấy mã này ở trang cảm ơn.</summary>
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Job { get; set; } = string.Empty;

    /// <summary>Mức đang dùng AI, một trong <see cref="AiLevels"/>.</summary>
    public string AiLevel { get; set; } = string.Empty;

    /// <summary>Buổi muốn tham gia; trống khi đăng ký lúc chưa có lịch.</summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>Tên buổi lúc đăng ký, giữ lại để đọc được kể cả khi buổi đã bị xoá khỏi lịch.</summary>
    public string SessionLabel { get; set; } = string.Empty;

    public string Goal { get; set; } = string.Empty;
    public string Project { get; set; } = string.Empty;
    public int HoursPerWeek { get; set; }

    public string Status { get; set; } = StatusNew;

    /// <summary>Ghi chú riêng của người duyệt, người đăng ký không thấy.</summary>
    public string AdminNote { get; set; } = string.Empty;

    /// <summary>Các email đã gửi (hoặc gửi lỗi) liên quan tới đơn này, cũ trước.</summary>
    public List<EmailLogEntry> Emails { get; set; } = [];

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public static readonly IReadOnlyList<string> AiLevels =
    [
        "Chưa từng dùng",
        "Thỉnh thoảng hỏi đáp",
        "Dùng hằng ngày cho công việc",
        "Đã tự làm sản phẩm / công cụ với AI",
    ];

    /// <summary>Các mức giờ tự học mỗi tuần: (giá trị lưu, chữ hiện).</summary>
    public static readonly IReadOnlyList<(int Value, string Label)> HourOptions =
    [
        (1, "Dưới 3 giờ"),
        (3, "3 – 5 giờ"),
        (6, "6 – 10 giờ"),
        (10, "Trên 10 giờ"),
    ];

    public string StatusLabel => Statuses.FirstOrDefault(item => item.Key == Status).Label ?? Status;
    public string StatusBadge => Statuses.FirstOrDefault(item => item.Key == Status).Badge ?? "text-bg-light";
    public string HoursLabel => HourOptions.FirstOrDefault(item => item.Value == HoursPerWeek).Label ?? $"{HoursPerWeek} giờ";
}
