namespace VoTrongNghia.Models;

/// <summary>
/// Một khóa học miễn phí, <c>Data/courses.json</c>. Trang tổng quan: <c>/khoa-hoc/{slug}</c>,
/// từng bài: <c>/khoa-hoc/{slug}/{bài}</c>.
///
/// <para>Thứ tự khóa trên site theo <see cref="Level"/>, thứ tự bài theo thứ tự trong
/// danh sách <see cref="Lessons"/>.</para>
/// </summary>
public sealed class Course
{
    public string Slug { get; set; } = string.Empty;

    /// <summary>Cấp độ 1, 2, 3… — vừa là nhãn vừa là thứ tự hiện.</summary>
    public int Level { get; set; } = 1;

    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;

    /// <summary>Chưa đăng thì khách không thấy cả khóa lẫn các bài trong khóa.</summary>
    public bool Published { get; set; }

    public List<Lesson> Lessons { get; set; } = [];

    public DateTimeOffset? UpdatedAt { get; set; }

    public int TotalMinutes => Lessons.Sum(lesson => lesson.Minutes);

    public string PublicUrl => "/khoa-hoc/" + Slug;

    public string LessonUrl(Lesson lesson) => $"/khoa-hoc/{Slug}/{lesson.Slug}";
}

public sealed class Lesson
{
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;

    /// <summary>Thời lượng ước tính, phút.</summary>
    public int Minutes { get; set; } = 5;

    /// <summary>Mã video YouTube hiện ở đầu bài, không bắt buộc.</summary>
    public string Video { get; set; } = string.Empty;

    /// <summary>
    /// "Học xong bài này bạn làm được" — 2–4 kết quả cụ thể, hiện đầu bài. Người học biết ngay bài
    /// này đáng đọc không, và tự kiểm được mình đã học xong chưa.
    /// </summary>
    public List<string> Outcomes { get; set; } = [];

    /// <summary>Nội dung bài, Markdown.</summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// Bài tập thực hành (Markdown), hiện trong khung riêng cuối bài, ngay trên nút "Đánh dấu đã
    /// học" — làm xong bài tập rồi mới đánh dấu.
    /// </summary>
    public string Exercise { get; set; } = string.Empty;

    /// <summary>"Bước tiếp theo" — một câu nối sang bài sau hoặc việc nên làm tiếp. Không bắt buộc.</summary>
    public string NextStep { get; set; } = string.Empty;

    /// <summary>Đủ khung bài học: có kết quả cần đạt và bài tập.</summary>
    public bool HasFramework => Outcomes.Count > 0 && Exercise.Trim().Length > 0;

    /// <summary>Ảnh đã tải lên để chèn vào bài (<c>uploads/khoa-hoc/…</c>), để lấy lại mã chèn và để dọn khi xoá.</summary>
    public List<string> Images { get; set; } = [];
}
