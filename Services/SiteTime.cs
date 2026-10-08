namespace VoTrongNghia.Services;

/// <summary>
/// Giờ Việt Nam. Máy chủ có thể đặt múi giờ khác (máy thuê ở nước ngoài, hay UTC), mà
/// "hôm nay" của lịch học và mã đơn theo ngày phải là hôm nay ở Việt Nam.
/// </summary>
public static class SiteTime
{
    public static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);

    public static DateTimeOffset Now => DateTimeOffset.UtcNow.ToOffset(VietnamOffset);

    public static DateOnly Today => DateOnly.FromDateTime(Now.DateTime);

    /// <summary>"07/10/2026".</summary>
    public static string Day(DateTimeOffset? value) => value?.ToOffset(VietnamOffset).ToString("dd/MM/yyyy") ?? string.Empty;

    /// <summary>"hôm nay", "hôm qua", "12 ngày trước" — tính theo ngày ở Việt Nam.</summary>
    public static string DaysAgo(DateTimeOffset value) =>
        (Today.DayNumber - DateOnly.FromDateTime(value.ToOffset(VietnamOffset).DateTime).DayNumber) switch
        {
            <= 0 => "hôm nay",
            1 => "hôm qua",
            var days => $"{days} ngày trước"
        };

    /// <summary>Số phút đọc ước tính, khoảng 200 chữ một phút, tối thiểu 1.</summary>
    public static int ReadMinutes(string markdown) => Math.Max(1, (int)Math.Round(MarkdownRenderer.CountWords(markdown) / 200.0));
}
