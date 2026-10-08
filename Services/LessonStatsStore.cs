using System.Collections.Concurrent;
using VoTrongNghia.Models;

namespace VoTrongNghia.Services;

/// <summary>
/// Đếm ẩn danh lượt mở bài / học xong (giai đoạn 1 của docs/ke-hoach-hoc-vien.md).
///
/// <para>Đếm trong bộ nhớ, ghi xuống <c>Data/thong-ke.json</c> 5 phút một lần và khi app dừng —
/// không ghi mỗi lượt bấm: mỗi lần ghi là đọc lại và viết lại cả file. App pool chết đột ngột
/// (không qua dừng êm) thì mất tối đa 5 phút số đếm — chấp nhận được với số liệu xu hướng.</para>
///
/// <para>Số liệu là xấp xỉ: một người học trên hai máy đếm thành hai, xoá dữ liệu trình duyệt rồi
/// học lại đếm thêm. Đủ để thấy bài nào nhiều người bỏ dở, không phải số tuyệt đối.</para>
/// </summary>
public sealed class LessonStatsStore : BackgroundService
{
    public const string EventOpen = "mo";
    public const string EventComplete = "xong";

    private static readonly TimeSpan FlushInterval = TimeSpan.FromMinutes(5);

    // Số đếm chưa ghi xuống đĩa: khoá "tháng|khóa/bài|sự kiện".
    private readonly ConcurrentDictionary<string, int> _pending = new();
    private readonly JsonFileStore<LessonStats> _store;
    private readonly ILogger<LessonStatsStore> _logger;

    public LessonStatsStore(ContentPaths paths, ILogger<LessonStatsStore> logger)
    {
        _logger = logger;
        _store = new JsonFileStore<LessonStats>(
            paths.StatsFile,
            Path.Combine(paths.SeedDirectory, "khong-co-seed.json"),
            paths.BackupsDirectory,
            logger,
            keepBackups: false);
    }

    public static string MonthKey(DateTimeOffset time) => time.ToOffset(SiteTime.VietnamOffset).ToString("yyyy-MM");

    /// <summary>Ghi nhận một sự kiện. Gọi sau khi đã kiểm khóa / bài có thật.</summary>
    public void Record(string lessonKey, string kind) =>
        _pending.AddOrUpdate($"{MonthKey(SiteTime.Now)}|{lessonKey}|{kind}", 1, (_, count) => count + 1);

    /// <summary>Số liệu đã ghi cộng phần còn trong bộ nhớ — trang /cms thấy số mới nhất, không phải chờ 5 phút.</summary>
    public async Task<LessonStats> ReadAsync(CancellationToken cancellationToken = default)
    {
        var saved = await _store.ReadAsync(cancellationToken);

        // Bản sao: đối tượng đọc từ kho dùng chung giữa các request, không được cộng thẳng vào.
        var result = new LessonStats
        {
            Months = saved.Months.ToDictionary(
                month => month.Key,
                month => month.Value.ToDictionary(item => item.Key, item => new LessonCounter { Opens = item.Value.Opens, Completions = item.Value.Completions }))
        };

        Merge(result, _pending.ToArray());
        return result;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(FlushInterval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await FlushAsync(CancellationToken.None);
            }
        }
        catch (OperationCanceledException)
        {
            // App đang dừng — StopAsync ghi nốt.
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        await FlushAsync(CancellationToken.None);
    }

    private async Task FlushAsync(CancellationToken cancellationToken)
    {
        // Rút số đếm ra khỏi bộ nhớ trước khi ghi: lượt nào tới trong lúc ghi thì vào đợt sau.
        var batch = new List<KeyValuePair<string, int>>();

        foreach (var key in _pending.Keys)
        {
            if (_pending.TryRemove(key, out var count))
            {
                batch.Add(new(key, count));
            }
        }

        if (batch.Count == 0)
        {
            return;
        }

        try
        {
            await _store.UpdateAsync(stats =>
            {
                Merge(stats, batch);
                return true;
            }, cancellationToken);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            // Ghi lỗi thì trả số đếm về bộ nhớ, lần sau ghi lại — không mất.
            foreach (var (key, count) in batch)
            {
                _pending.AddOrUpdate(key, count, (_, existing) => existing + count);
            }

            _logger.LogWarning(ex, "Không ghi được thống kê học, sẽ thử lại");
        }
    }

    private static void Merge(LessonStats stats, IEnumerable<KeyValuePair<string, int>> counts)
    {
        foreach (var (key, count) in counts)
        {
            var parts = key.Split('|');

            if (parts.Length != 3)
            {
                continue;
            }

            if (!stats.Months.TryGetValue(parts[0], out var month))
            {
                stats.Months[parts[0]] = month = [];
            }

            if (!month.TryGetValue(parts[1], out var counter))
            {
                month[parts[1]] = counter = new LessonCounter();
            }

            if (parts[2] == EventOpen)
            {
                counter.Opens += count;
            }
            else
            {
                counter.Completions += count;
            }
        }
    }
}
