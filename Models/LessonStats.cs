namespace VoTrongNghia.Models;

/// <summary>
/// Thống kê ẩn danh lượt mở bài và học xong, <c>Data/thong-ke.json</c>. Theo tháng (giờ Việt Nam),
/// rồi theo <c>"khóa/bài"</c>. Không có IP, không có gì cho biết ai đã học — chỉ là số đếm.
/// </summary>
public sealed class LessonStats
{
    /// <summary>Khoá tháng dạng "2026-10".</summary>
    public Dictionary<string, Dictionary<string, LessonCounter>> Months { get; set; } = [];
}

public sealed class LessonCounter
{
    /// <summary>Số trình duyệt mở bài này lần đầu.</summary>
    public int Opens { get; set; }

    /// <summary>Số trình duyệt bấm "Đánh dấu đã học" lần đầu.</summary>
    public int Completions { get; set; }
}
