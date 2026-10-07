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

    /// <summary>Nội dung bài, Markdown.</summary>
    public string Body { get; set; } = string.Empty;
}
