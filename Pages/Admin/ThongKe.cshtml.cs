using VoTrongNghia.Models;
using VoTrongNghia.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace VoTrongNghia.Pages.Admin;

/// <summary>
/// Phễu học của từng khóa từ thống kê ẩn danh: bao nhiêu người mở từng bài, bao nhiêu người học
/// xong, và bài nào người ta bỏ nhiều nhất — để biết bài nào cần viết lại cho dễ hơn.
/// </summary>
public class ThongKeModel : PageModel
{
    private readonly LessonStatsStore _stats;
    private readonly SiteContent _content;
    private readonly RegistrationStore _registrations;

    public ThongKeModel(LessonStatsStore stats, SiteContent content, RegistrationStore registrations)
    {
        _stats = stats;
        _content = content;
        _registrations = registrations;
    }

    public sealed record LessonRow(int Number, Lesson Lesson, string Url, int Opens, int Completions)
    {
        /// <summary>Tỉ lệ người mở bài rồi học xong bài đó.</summary>
        public double CompletionRate => Opens == 0 ? 0 : (double)Completions / Opens;
    }

    public sealed record CourseFunnel(Course Course, IReadOnlyList<LessonRow> Rows)
    {
        public int Started => Rows.Count > 0 ? Rows[0].Opens : 0;
        public int Finished => Rows.Count > 0 ? Rows[^1].Completions : 0;

        /// <summary>So với số người mở bài 1, còn bao nhiêu phần trăm mở tới bài này.</summary>
        public double Retention(LessonRow row) => Started == 0 ? 0 : (double)row.Opens / Started;

        /// <summary>
        /// Bài làm rơi nhiều người nhất: so lượt mở với bài ngay trước, tỉ lệ còn lại thấp nhất. Cần
        /// đủ người (từ 5 lượt mở bài trước) mới kết luận — 2 người bỏ trên 3 không nói lên gì.
        /// </summary>
        public LessonRow? BiggestDrop =>
            Rows.Skip(1)
                .Select((row, i) => (row, before: Rows[i]))
                .Where(pair => pair.before.Opens >= 5)
                .OrderBy(pair => (double)pair.row.Opens / pair.before.Opens)
                .Select(pair => pair.row)
                .FirstOrDefault();
    }

    public IReadOnlyList<string> Months { get; private set; } = [];

    /// <summary>Tháng đang xem ("2026-10"), trống là toàn bộ.</summary>
    public string Month { get; private set; } = string.Empty;

    public IReadOnlyList<CourseFunnel> Funnels { get; private set; } = [];
    public int TotalOpens { get; private set; }
    public int TotalCompletions { get; private set; }
    public int RegistrationsInPeriod { get; private set; }

    public async Task OnGetAsync(string? thang, CancellationToken cancellationToken)
    {
        var stats = await _stats.ReadAsync(cancellationToken);
        Months = stats.Months.Keys.OrderDescending().ToList();
        Month = thang is not null && stats.Months.ContainsKey(thang) ? thang : string.Empty;

        // Gộp các tháng được chọn thành một bảng "khóa/bài" → số đếm.
        var totals = new Dictionary<string, LessonCounter>();

        foreach (var (month, lessons) in stats.Months)
        {
            if (Month.Length > 0 && month != Month)
            {
                continue;
            }

            foreach (var (key, counter) in lessons)
            {
                var total = totals.TryGetValue(key, out var existing) ? existing : totals[key] = new LessonCounter();
                total.Opens += counter.Opens;
                total.Completions += counter.Completions;
            }
        }

        Funnels = (await _content.PublishedCoursesAsync(cancellationToken))
            .Select(course => new CourseFunnel(course, course.Lessons.Select((lesson, i) =>
            {
                var counter = totals.GetValueOrDefault($"{course.Slug}/{lesson.Slug}") ?? new LessonCounter();
                return new LessonRow(i + 1, lesson, course.LessonUrl(lesson), counter.Opens, counter.Completions);
            }).ToList()))
            .ToList();

        TotalOpens = totals.Values.Sum(counter => counter.Opens);
        TotalCompletions = totals.Values.Sum(counter => counter.Completions);

        var registrations = await _registrations.Registrations.ReadAsync(cancellationToken);
        RegistrationsInPeriod = registrations.Count(item => Month.Length == 0 || LessonStatsStore.MonthKey(item.CreatedAt) == Month);
    }
}
