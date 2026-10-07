using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using VoTrongNghia.Models;

namespace VoTrongNghia.Services;

/// <summary>
/// Sổ đơn đăng ký lớp online, <c>Data/registrations.json</c>.
///
/// <para>Tách khỏi <see cref="SiteContent"/> như sổ đơn hàng của Dokma: đây không phải nội
/// dung site, không có seed. Và đây là chỗ duy nhất người lạ ghi được vào đĩa, nên mọi thứ
/// gửi lên đều qua <see cref="ReceiveAsync"/> kiểm trước — kiểm lại đúng những luật trang
/// đăng ký đã kiểm bằng JS, vì JS thì ai cũng tắt được.</para>
/// </summary>
public sealed partial class RegistrationStore
{
    public const int GoalMinLength = 80;
    public const int ProjectMinLength = 30;

    private readonly SiteContent _content;
    private readonly ILogger<RegistrationStore> _logger;

    public RegistrationStore(ContentPaths paths, SiteContent content, ILogger<RegistrationStore> logger)
    {
        _content = content;
        _logger = logger;

        // Không có seed: đường dẫn seed trỏ vào file không tồn tại.
        Registrations = new JsonFileStore<List<Registration>>(
            paths.RegistrationsFile,
            Path.Combine(paths.SeedDirectory, "khong-co-seed.json"),
            paths.BackupsDirectory,
            logger);
    }

    public JsonFileStore<List<Registration>> Registrations { get; }

    [GeneratedRegex(@"^0\d{9,10}$")]
    private static partial Regex PhonePattern();

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailPattern();

    /// <summary>Những gì form đăng ký gửi lên. Tên trường khớp với Pages/DangKy.cshtml.</summary>
    public sealed class Request
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Job { get; set; } = string.Empty;
        public string AiLevel { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
        public string Goal { get; set; } = string.Empty;
        public string Project { get; set; } = string.Empty;
        public int HoursPerWeek { get; set; }

        /// <summary>Chỉ số các cam kết đã tích.</summary>
        public List<int> Commitments { get; set; } = [];

        public string Pledge { get; set; } = string.Empty;

        /// <summary>Ô bẫy, ẩn với người thật. Máy gửi rác điền mọi ô nó thấy.</summary>
        public string Website { get; set; } = string.Empty;
    }

    /// <summary>Lỗi gắn với một ô (tên trường trong <see cref="Request"/>), hoặc chung khi Field trống.</summary>
    public sealed record FieldError(string Field, string Message);

    /// <summary>Kết quả: có mã đơn khi đã lưu, có danh sách lỗi khi dữ liệu chưa đạt.</summary>
    public sealed record Result(string? Code, IReadOnlyList<FieldError> Errors, bool IsSpam = false);

    public async Task<Result> ReceiveAsync(Request request, CancellationToken cancellationToken)
    {
        // Ô bẫy có chữ: trả "thành công" giả để máy gửi rác không biết mà đổi cách,
        // nhưng không ghi gì.
        if (request.Website.Length > 0)
        {
            _logger.LogWarning("Bỏ một đơn đăng ký điền ô bẫy");
            return new Result("DK000000-00", [], IsSpam: true);
        }

        var live = await _content.Live.ReadAsync(cancellationToken);
        var errors = new List<FieldError>();

        var name = Clean(request.Name, 80);
        var email = Clean(request.Email, 120).ToLowerInvariant();
        var phone = new string(request.Phone.Where(char.IsAsciiDigit).ToArray());
        var job = Clean(request.Job, 120);
        var goal = Clean(request.Goal, 2000, keepLines: true);
        var project = Clean(request.Project, 1000, keepLines: true);

        if (name.Length < 2)
        {
            errors.Add(new("Name", "Nhập họ và tên."));
        }

        if (!EmailPattern().IsMatch(email))
        {
            errors.Add(new("Email", "Email chưa đúng — mình sẽ báo kết quả duyệt qua email này."));
        }

        if (!PhonePattern().IsMatch(phone))
        {
            errors.Add(new("Phone", "Số điện thoại / Zalo cần 10 hoặc 11 chữ số, bắt đầu bằng 0."));
        }

        if (job.Length < 2)
        {
            errors.Add(new("Job", "Nhập nghề nghiệp hiện tại."));
        }

        if (!Registration.AiLevels.Contains(request.AiLevel))
        {
            errors.Add(new("AiLevel", "Chọn mức bạn đang dùng AI."));
        }

        var upcoming = live.Upcoming(SiteTime.Today).ToList();
        var session = upcoming.FirstOrDefault(item => item.Id == request.SessionId);

        if (upcoming.Count > 0 && session is null)
        {
            errors.Add(new("SessionId", "Chọn buổi bạn muốn tham gia."));
        }

        if (goal.Length < GoalMinLength)
        {
            errors.Add(new("Goal", $"Phần mục tiêu cần ít nhất {GoalMinLength} ký tự — hãy chia sẻ cụ thể hơn."));
        }

        if (project.Length < ProjectMinLength)
        {
            errors.Add(new("Project", $"Phần \"muốn làm ra gì\" cần ít nhất {ProjectMinLength} ký tự."));
        }

        if (Registration.HourOptions.All(item => item.Value != request.HoursPerWeek))
        {
            errors.Add(new("HoursPerWeek", "Chọn số giờ bạn dành cho việc học mỗi tuần."));
        }
        else if (request.HoursPerWeek < live.MinHoursPerWeek)
        {
            errors.Add(new("HoursPerWeek",
                $"Lớp cần tối thiểu {live.MinHoursPerWeek} giờ tự học mỗi tuần. Khi sắp xếp được thời gian, bạn quay lại đăng ký nhé!"));
        }

        var ticked = request.Commitments.Distinct().Count(index => index >= 0 && index < live.Commitments.Count);

        if (ticked < live.Commitments.Count)
        {
            errors.Add(new("Commitments", "Bạn cần đồng ý với tất cả các cam kết."));
        }

        if (live.Pledge.Length > 0 && NormalizePledge(request.Pledge) != NormalizePledge(live.Pledge))
        {
            errors.Add(new("Pledge", "Câu cam kết chưa khớp — hãy gõ lại chính xác nhé."));
        }

        if (errors.Count > 0)
        {
            return new Result(null, errors);
        }

        var registration = new Registration
        {
            Name = name,
            Email = email,
            Phone = phone,
            Job = job,
            AiLevel = request.AiLevel,
            SessionId = session?.Id ?? string.Empty,
            SessionLabel = session?.Label ?? string.Empty,
            Goal = goal,
            Project = project,
            HoursPerWeek = request.HoursPerWeek,
            CreatedAt = SiteTime.Now
        };

        var duplicate = false;

        await Registrations.UpdateAsync(list =>
        {
            // Một email chỉ đăng ký một lần cho mỗi buổi (hoặc một lần khi chưa có lịch). Bấm
            // gửi hai lần hay quay lại gửi lại thì không thành hai đơn cho người duyệt đọc.
            if (list.Any(item => item.Email == registration.Email && item.SessionId == registration.SessionId &&
                                 item.Status != Registration.StatusRejected))
            {
                duplicate = true;
                return false;
            }

            // Đánh số trong khoá ghi, để hai đơn đến cùng lúc không trùng mã.
            var prefix = $"DK{registration.CreatedAt:yyMMdd}-";
            var next = list
                .Where(item => item.Code.StartsWith(prefix, StringComparison.Ordinal))
                .Select(item => int.TryParse(item.Code[prefix.Length..], out var n) ? n : 0)
                .DefaultIfEmpty(0)
                .Max() + 1;

            registration.Code = $"{prefix}{next:00}";

            // Mới nhất lên đầu file: /cms/dang-ky đọc từ trên xuống.
            list.Insert(0, registration);
            return true;
        }, cancellationToken);

        if (duplicate)
        {
            return new Result(null, [new("Email",
                "Email này đã đăng ký rồi. Mình sẽ phản hồi qua email trong vài ngày — không cần gửi lại nhé.")]);
        }

        _logger.LogInformation("Nhận đơn đăng ký {Code}", registration.Code);

        return new Result(registration.Code, []);
    }

    public async Task<int> CountNewAsync(CancellationToken cancellationToken = default) =>
        (await Registrations.ReadAsync(cancellationToken)).Count(item => item.Status == Registration.StatusNew);

    /// <summary>
    /// So câu cam kết: bỏ khác biệt hoa thường, dấu cách thừa, dấu chấm cuối câu — người
    /// gõ đúng ý thì đừng bắt lỗi vì một dấu chấm. Chuẩn hoá Unicode vì bộ gõ tiếng Việt
    /// có thể ra chữ dựng sẵn hoặc chữ tổ hợp, nhìn giống hệt nhau mà so không bằng.
    /// </summary>
    public static string NormalizePledge(string text) =>
        Regex.Replace(text.Normalize(NormalizationForm.FormC).Trim(), @"\s+", " ")
            .TrimEnd('.', '!')
            .ToLower(CultureInfo.GetCultureInfo("vi-VN"));

    private static string Clean(string? value, int max, bool keepLines = false)
    {
        var text = (value ?? string.Empty).Replace("\r\n", "\n").Trim();
        text = keepLines ? Regex.Replace(text, @"\n{3,}", "\n\n") : Regex.Replace(text, @"\s+", " ");
        return text.Length > max ? text[..max] : text;
    }
}
