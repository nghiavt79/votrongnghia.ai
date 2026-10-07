using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VoTrongNghia.Services;

/// <summary>
/// Đọc ghi một file JSON nội dung: <c>site.json</c>, <c>posts.json</c>, <c>courses.json</c>,
/// <c>live.json</c>, <c>registrations.json</c>.
///
/// <para>Ba việc làm ở đây một lần cho mọi file nội dung:</para>
/// <list type="number">
/// <item>Ghi nguyên tử: ghi ra file tạm rồi <see cref="File.Replace(string, string, string?)"/>.
/// Mất điện hay app pool recycle giữa chừng thì hoặc bản mới nguyên vẹn, hoặc bản
/// cũ nguyên vẹn, không bao giờ còn lại một file JSON cụt làm trắng cả site.</item>
/// <item>Sao lưu: bản cũ đẩy vào <c>Data/backups/</c> mỗi lần ghi, giữ 20 bản gần nhất.</item>
/// <item>Nhớ đệm: trang chủ đọc gần như mọi file nội dung ở mỗi lượt xem, nên giữ bản
/// đã đọc cho tới khi file đổi mốc sửa.</item>
/// </list>
/// </summary>
public sealed class JsonFileStore<T> where T : class, new()
{
    private readonly string _path;
    private readonly string _seedPath;
    private readonly string _backupsDirectory;
    private readonly string _backupPrefix;
    private readonly ILogger _logger;

    // Một file, có khi hai tab cùng bấm Lưu. Khoá để hai lượt ghi không đan vào nhau
    // mà mất một bên.
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    // Bản ghi lớp chứ không phải tuple: gán một tham chiếu là nguyên tử, còn gán
    // một struct ba trường thì request khác có thể đọc được nửa cũ nửa mới.
    private sealed record CacheEntry(DateTime Stamp, long Length, T Value);

    private volatile CacheEntry? _cache;

    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // UnsafeRelaxedJsonEscaping: mặc định "Bài viết" thành "Bài viết". Vẫn
    // là JSON hợp lệ nhưng không ai mở file ra soát được, và diff git thì vô dụng.
    public static readonly JsonSerializerOptions WriteOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public JsonFileStore(string path, string seedPath, string backupsDirectory, ILogger logger)
    {
        _path = path;
        _seedPath = seedPath;
        _backupsDirectory = backupsDirectory;
        _backupPrefix = Path.GetFileNameWithoutExtension(path);
        _logger = logger;
    }

    /// <summary>
    /// File đang được đọc. Chưa có file sống (máy chủ thiếu quyền ghi nên chưa chép
    /// được seed sang) thì đọc thẳng seed để site vẫn có hàng.
    /// </summary>
    private string SourcePath => File.Exists(_path) || !File.Exists(_seedPath) ? _path : _seedPath;

    /// <summary>Mốc sửa của file.</summary>
    public DateTime LastWriteUtc =>
        File.Exists(SourcePath) ? File.GetLastWriteTimeUtc(SourcePath) : DateTime.MinValue;

    /// <summary>
    /// Bản đang có. Đối tượng trả về dùng chung giữa các request — chỉ đọc, đừng
    /// sửa. Muốn đổi thì qua <see cref="UpdateAsync"/>.
    /// </summary>
    public async Task<T> ReadAsync(CancellationToken cancellationToken = default)
    {
        var path = SourcePath;

        if (!File.Exists(path))
        {
            return new T();
        }

        var info = new FileInfo(path);

        if (_cache is { } cached && cached.Stamp == info.LastWriteTimeUtc && cached.Length == info.Length)
        {
            return cached.Value;
        }

        var value = await ReadFromDiskAsync(path, cancellationToken);
        _cache = new CacheEntry(info.LastWriteTimeUtc, info.Length, value);

        return value;
    }

    /// <summary>
    /// Đọc bản mới nhất trên đĩa, cho <paramref name="mutate"/> sửa, rồi ghi lại.
    /// Trả về false từ <paramref name="mutate"/> là bỏ, không ghi gì.
    /// </summary>
    public async Task<bool> UpdateAsync(Func<T, bool> mutate, CancellationToken cancellationToken = default)
    {
        await _writeLock.WaitAsync(cancellationToken);

        try
        {
            // Đọc lại từ đĩa chứ không lấy bản nhớ đệm: bản đó đang được các request
            // khác dùng chung, sửa thẳng lên nó là site đổi trước khi kịp ghi.
            var value = File.Exists(SourcePath)
                ? await ReadFromDiskAsync(SourcePath, cancellationToken)
                : new T();

            if (!mutate(value))
            {
                return false;
            }

            await WriteAsync(value, cancellationToken);
            _cache = null;

            return true;
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <summary>Nội dung file hiện hành, cho nút tải bản sao lưu.</summary>
    public async Task<byte[]> ExportAsync(CancellationToken cancellationToken = default) =>
        File.Exists(SourcePath) ? await File.ReadAllBytesAsync(SourcePath, cancellationToken) : [];

    private async Task<T> ReadFromDiskAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<T>(stream, ReadOptions, cancellationToken) ?? new T();
        }
        catch (JsonException ex)
        {
            // File hỏng mà trả về rỗng thì lượt Lưu kế tiếp sẽ ghi đè cái rỗng đó lên
            // toàn bộ dữ liệu. Ném lỗi để không ai ghi được gì cho tới khi sửa file.
            _logger.LogError(ex, "Không đọc được {Path}", path);
            throw new InvalidOperationException(
                $"File {Path.GetFileName(path)} bị hỏng, không đọc được. Khôi phục từ Data/backups/.", ex);
        }
    }

    private async Task WriteAsync(T value, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);

        var temporary = _path + ".tmp";

        await using (var stream = File.Create(temporary))
        {
            await JsonSerializer.SerializeAsync(stream, value, WriteOptions, cancellationToken);
        }

        if (File.Exists(_path))
        {
            Directory.CreateDirectory(_backupsDirectory);

            var backup = Path.Combine(_backupsDirectory, $"{_backupPrefix}-{DateTime.Now:yyyyMMdd-HHmmss-fff}.json");
            File.Replace(temporary, _path, backup);
            PruneBackups();
        }
        else
        {
            File.Move(temporary, _path);
        }

        _logger.LogInformation("Đã lưu {File}", Path.GetFileName(_path));
    }

    /// <summary>Giữ 20 bản sao lưu gần nhất của file này, phần còn lại xoá đi.</summary>
    private void PruneBackups()
    {
        try
        {
            var stale = new DirectoryInfo(_backupsDirectory)
                .GetFiles($"{_backupPrefix}-*.json")
                .OrderByDescending(file => file.Name, StringComparer.Ordinal)
                .Skip(20);

            foreach (var file in stale)
            {
                file.Delete();
            }
        }
        catch (IOException ex)
        {
            // Dọn bản sao lưu hỏng thì kệ nó, đừng để việc lưu thất bại theo.
            _logger.LogWarning(ex, "Không dọn được bản sao lưu cũ");
        }
    }
}
