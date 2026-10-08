using System.Security.Cryptography;
using VoTrongNghia.Models;

namespace VoTrongNghia.Services;

/// <summary>
/// Sổ học viên, <c>Data/hoc-vien.json</c>. Tách khỏi <see cref="RegistrationStore"/>: một người có
/// thể có nhiều đơn (đăng ký lại đợt sau) nhưng chỉ một tài khoản, gắn theo email.
///
/// <para>Mỗi lần học viên đánh dấu một bài là ghi lại cả file. 500 học viên × 30 bài vẫn dưới 1MB,
/// ghi tức thì — quá vài nghìn học viên hoạt động cùng lúc thì mới phải tính chuyển sang SQLite.</para>
/// </summary>
public sealed class LearnerStore
{
    /// <summary>"Lâu không học": quá chừng này ngày không đăng nhập, không đánh dấu bài nào.</summary>
    public const int IdleDays = 7;

    public const string AnonymizedName = "(đã ẩn danh)";

    private readonly ILogger<LearnerStore> _logger;

    public LearnerStore(ContentPaths paths, ILogger<LearnerStore> logger)
    {
        _logger = logger;

        // Có sao lưu như sổ đơn: mất file này là mất tiến độ của mọi học viên.
        Learners = new JsonFileStore<List<Learner>>(
            paths.LearnersFile,
            Path.Combine(paths.SeedDirectory, "khong-co-seed.json"),
            paths.BackupsDirectory,
            logger);
    }

    public JsonFileStore<List<Learner>> Learners { get; }

    public static string NormalizeEmail(string? email) => (email ?? string.Empty).Trim().ToLowerInvariant();

    public async Task<Learner?> FindAsync(string? id, CancellationToken cancellationToken = default) =>
        string.IsNullOrEmpty(id)
            ? null
            : (await Learners.ReadAsync(cancellationToken)).FirstOrDefault(item => item.Id == id);

    public async Task<Learner?> FindByEmailAsync(string? email, CancellationToken cancellationToken = default)
    {
        var key = NormalizeEmail(email);
        return key.Length == 0
            ? null
            : (await Learners.ReadAsync(cancellationToken)).FirstOrDefault(item => item.Email == key);
    }

    public static bool IsIdle(Learner item) =>
        item.Status == Learner.StatusActive && item.LastActivity < SiteTime.Now.AddDays(-IdleDays);

    /// <summary>Quá hạn giữ thông tin cá nhân — cùng thời hạn với đơn đăng ký, xem /chinh-sach-du-lieu.</summary>
    public static bool IsExpired(Learner item) =>
        item.Name != AnonymizedName && item.LastActivity < SiteTime.Now.AddMonths(-RegistrationStore.RetentionMonths);

    /// <summary>
    /// Duyệt đơn: tạo học viên từ đơn, hoặc gắn thêm đơn vào học viên cùng email đã có. Học viên đang
    /// tạm dừng / đã hoàn thành mà được duyệt đợt mới thì quay về "đang học"; tài khoản đã khoá giữ nguyên
    /// — khoá là quyết định riêng của người quản trị.
    /// </summary>
    public async Task<Learner> EnsureForRegistrationAsync(Registration registration, CancellationToken cancellationToken = default)
    {
        Learner? result = null;
        var email = NormalizeEmail(registration.Email);

        await Learners.UpdateAsync(list =>
        {
            var item = list.FirstOrDefault(entry => entry.Email == email);

            if (item is null)
            {
                item = NewLearner(list, email, registration.Name, registration.Phone);
                list.Insert(0, item);
                _logger.LogInformation("Tạo học viên {Id} từ đơn {Code}", item.Id, registration.Code);
            }
            else if (item.Status is Learner.StatusPaused or Learner.StatusFinished)
            {
                item.Status = Learner.StatusActive;
            }

            if (!item.RegistrationCodes.Contains(registration.Code))
            {
                item.RegistrationCodes.Add(registration.Code);
            }

            result = item;
            return true;
        }, cancellationToken);

        return result!;
    }

    /// <summary>Thêm tay một học viên (người được hướng dẫn ngoài form). Null khi email đã có tài khoản.</summary>
    public async Task<Learner?> AddAsync(string name, string email, string phone, CancellationToken cancellationToken = default)
    {
        Learner? result = null;
        email = NormalizeEmail(email);

        await Learners.UpdateAsync(list =>
        {
            if (list.Any(entry => entry.Email == email))
            {
                return false;
            }

            result = NewLearner(list, email, name.Trim(), new string(phone.Where(char.IsAsciiDigit).ToArray()));
            list.Insert(0, result);
            return true;
        }, cancellationToken);

        return result;
    }

    /// <summary>Đánh dấu / bỏ đánh dấu một bài. Bài đã đánh dấu thì giữ thời điểm cũ.</summary>
    public Task<bool> SetProgressAsync(string id, string lessonKey, bool done, CancellationToken cancellationToken = default) =>
        Learners.UpdateAsync(list =>
        {
            var item = list.FirstOrDefault(entry => entry.Id == id);

            if (item is null)
            {
                return false;
            }

            if (done)
            {
                item.Progress.TryAdd(lessonKey, SiteTime.Now);
            }
            else
            {
                item.Progress.Remove(lessonKey);
            }

            item.LastSeenAt = SiteTime.Now;
            return true;
        }, cancellationToken);

    /// <summary>
    /// Gộp tiến độ đang có trên máy người học (học trước khi có tài khoản) lên tài khoản. Chỉ thêm,
    /// không bớt. Trả về toàn bộ bài đã học sau khi gộp.
    /// </summary>
    public async Task<IReadOnlyList<string>> MergeProgressAsync(string id, IEnumerable<string> lessonKeys, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<string> result = [];
        var keys = lessonKeys.ToList();

        await Learners.UpdateAsync(list =>
        {
            var item = list.FirstOrDefault(entry => entry.Id == id);

            if (item is null)
            {
                return false;
            }

            var added = keys.Count(key => item.Progress.TryAdd(key, SiteTime.Now));
            result = item.Progress.Keys.ToList();
            return added > 0;
        }, cancellationToken);

        return result;
    }

    /// <summary>Ghi nhận lần đăng nhập.</summary>
    public Task TouchAsync(string id, CancellationToken cancellationToken = default) =>
        Learners.UpdateAsync(list =>
        {
            var item = list.FirstOrDefault(entry => entry.Id == id);

            if (item is null)
            {
                return false;
            }

            item.LastSeenAt = SiteTime.Now;
            return true;
        }, cancellationToken);

    /// <summary>Đổi trạng thái và ghi chú. Khoá tài khoản thì đổi dấu bảo mật: mọi phiên đang mở bị đăng xuất.</summary>
    public Task<bool> UpdateAsync(string id, string status, string adminNote, CancellationToken cancellationToken = default) =>
        Learners.UpdateAsync(list =>
        {
            var item = list.FirstOrDefault(entry => entry.Id == id);

            if (item is null)
            {
                return false;
            }

            if (status == Learner.StatusLocked && item.Status != Learner.StatusLocked)
            {
                item.SecurityStamp = NewStamp();
            }

            item.Status = status;
            item.AdminNote = adminNote.Length > 2000 ? adminNote[..2000] : adminNote;
            return true;
        }, cancellationToken);

    /// <summary>Ghi một lần gửi email vào lịch sử của học viên. Giữ 50 thư gần nhất.</summary>
    public Task LogEmailAsync(string id, EmailLogEntry entry, CancellationToken cancellationToken = default) =>
        Learners.UpdateAsync(list =>
        {
            var item = list.FirstOrDefault(learner => learner.Id == id);

            if (item is null)
            {
                return false;
            }

            item.Emails.Add(entry);

            if (item.Emails.Count > 50)
            {
                item.Emails.RemoveRange(0, item.Emails.Count - 50);
            }

            return true;
        }, cancellationToken);

    /// <summary>Xoá hẳn tài khoản: học viên tự xoá ở /hoc-vien, hoặc người quản trị xoá ở /cms.</summary>
    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var deleted = await Learners.UpdateAsync(list => list.RemoveAll(entry => entry.Id == id) > 0, cancellationToken);

        if (deleted)
        {
            _logger.LogWarning("Đã xoá học viên {Id}", id);
        }

        return deleted;
    }

    /// <summary>
    /// Ẩn danh hoá học viên không hoạt động quá hạn giữ: xoá tên, email, điện thoại, ghi chú, địa chỉ
    /// trong lịch sử thư; khoá tài khoản. Giữ tiến độ — đủ để đếm số liệu mà không còn biết ai là ai.
    /// </summary>
    public async Task<int> AnonymizeExpiredAsync(CancellationToken cancellationToken = default)
    {
        var count = 0;

        await Learners.UpdateAsync(list =>
        {
            foreach (var item in list.Where(IsExpired))
            {
                item.Name = AnonymizedName;
                item.Email = string.Empty;
                item.Phone = string.Empty;
                item.AdminNote = string.Empty;
                item.Status = Learner.StatusLocked;
                item.SecurityStamp = NewStamp();

                foreach (var mail in item.Emails)
                {
                    mail.To = string.Empty;
                    mail.Subject = string.Empty;
                }

                count++;
            }

            return count > 0;
        }, cancellationToken);

        if (count > 0)
        {
            _logger.LogInformation("Đã ẩn danh hoá {Count} học viên không hoạt động quá {Months} tháng", count, RegistrationStore.RetentionMonths);
        }

        return count;
    }

    private static Learner NewLearner(List<Learner> list, string email, string name, string phone)
    {
        string id;

        do
        {
            id = "hv-" + Convert.ToHexString(RandomNumberGenerator.GetBytes(3)).ToLowerInvariant();
        }
        while (list.Any(entry => entry.Id == id));

        return new Learner
        {
            Id = id,
            Email = email,
            Name = name,
            Phone = phone,
            CreatedAt = SiteTime.Now,
            SecurityStamp = NewStamp()
        };
    }

    private static string NewStamp() => Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
}
