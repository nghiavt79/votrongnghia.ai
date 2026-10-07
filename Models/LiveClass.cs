namespace VoTrongNghia.Models;

/// <summary>
/// Lớp học online miễn phí, <c>Data/live.json</c>, sửa ở <c>/cms/lop-online</c>: điều kiện
/// đăng ký, bản cam kết và lịch các buổi.
///
/// <para>Lớp miễn phí nhưng chỗ có hạn, nên đăng ký có ba lớp lọc người học nghiêm túc:
/// bài đầu vào (<see cref="RequireCourse"/>), câu trả lời đủ dài và số giờ tự học tối
/// thiểu, bản cam kết phải tích đủ và gõ lại câu cam kết.</para>
/// </summary>
public sealed class LiveClass
{
    /// <summary>
    /// Khóa phải học xong mới mở được form đăng ký, trống là không yêu cầu. Tiến độ lưu
    /// trên trình duyệt người học (localStorage), nên đây là bộ lọc người chỉ tò mò chứ
    /// không phải khoá thật — máy chủ không kiểm được.
    /// </summary>
    public string RequireCourse { get; set; } = string.Empty;

    public int MinHoursPerWeek { get; set; } = 3;

    /// <summary>Các điều người học phải tích đủ.</summary>
    public List<string> Commitments { get; set; } = [];

    /// <summary>Câu người học phải tự gõ lại để xác nhận.</summary>
    public string Pledge { get; set; } = string.Empty;

    public List<LiveSession> Sessions { get; set; } = [];

    /// <summary>Buổi chưa qua, sớm nhất trước. Buổi đã qua ngày tự ẩn khỏi site.</summary>
    public IEnumerable<LiveSession> Upcoming(DateOnly today) =>
        Sessions.Where(session => session.Date >= today).OrderBy(session => session.Date);
}

public sealed class LiveSession
{
    /// <summary>Mã cố định của buổi, đơn đăng ký trỏ tới mã này.</summary>
    public string Id { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public DateOnly Date { get; set; }

    /// <summary>Giờ học, chữ tự do: "20:00 – 21:30".</summary>
    public string Time { get; set; } = string.Empty;

    /// <summary>Google Meet, Zoom… — chỉ là chữ hiện trên trang chủ.</summary>
    public string Platform { get; set; } = string.Empty;

    /// <summary>
    /// Link phòng học. KHÔNG BAO GIỜ hiện trên trang công khai: chỉ đi vào email báo duyệt gửi
    /// riêng cho người được nhận. Lớp miễn phí có cam kết — lộ link là ai cũng vào được.
    /// </summary>
    public string MeetingLink { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    /// <summary>Số chỗ, 0 là không giới hạn. Chỉ để hiện và để nhắc khi duyệt, không tự chặn đăng ký.</summary>
    public int Capacity { get; set; }

    public string Label => $"{Date:dd/MM/yyyy} · {Title}";
}
