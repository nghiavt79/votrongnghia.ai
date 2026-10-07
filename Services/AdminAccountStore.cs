using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;

namespace VoTrongNghia.Services;

/// <summary>Tài khoản quản trị, đọc từ <c>admin.json</c> trong thư mục nội dung.</summary>
public sealed class AdminAccount
{
    public string UserName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Đổi mỗi lần đặt lại mật khẩu, và được gắn vào cookie đăng nhập.
    ///
    /// Không có nó thì đổi mật khẩu chỉ chặn được lần đăng nhập sau: ai đang mở
    /// sẵn trang quản trị trên máy khác vẫn dùng tiếp như không có gì xảy ra —
    /// mà đổi mật khẩu thường là vì sợ đúng chuyện đó. Cookie mang dấu cũ sẽ bị
    /// từ chối ở request kế tiếp.
    /// </summary>
    public string SecurityStamp { get; set; } = string.Empty;

    public DateTimeOffset? UpdatedAt { get; set; }
}

/// <summary>
/// Một tài khoản quản trị duy nhất, mật khẩu băm bằng PBKDF2 qua
/// <see cref="PasswordHasher{TUser}"/> của ASP.NET.
///
/// <para>File <c>admin.json</c> nằm trong thư mục nội dung chứ không phải
/// <c>appsettings.json</c>: appsettings nằm trong git, mà băm mật khẩu quản trị
/// thì không có lý do gì để nằm trong một repo trên GitHub.</para>
///
/// <para>Đặt mật khẩu lần đầu bằng <c>dotnet VoTrongNghia.dll --dat-mat-khau</c>
/// trên máy chủ. Lệnh đó hỏi mật khẩu qua bàn phím chứ không nhận tham số, để
/// mật khẩu không nằm lại trong lịch sử dòng lệnh. Bình thường thì tạo ngay ở /cms lần đầu mở, và tự đổi
/// trong trang quản trị, ở <c>/cms/doi-mat-khau</c>.</para>
/// </summary>
public sealed class AdminAccountStore
{
    /// <summary>
    /// Độ dài tối thiểu.
    ///
    /// <para>Cùng mức với kimthyland. Phần bù cho độ dài nằm ở chỗ khác: mỗi IP chỉ
    /// gửi được 10 lần mật khẩu trong 5 phút, nên dò từ xa là không khả thi.</para>
    /// </summary>
    public const int MinimumPasswordLength = 10;

    /// <summary>Tên claim mang dấu bảo mật trong cookie đăng nhập.</summary>
    public const string SecurityStampClaim = "vn:stamp";

    private readonly ContentPaths _paths;
    private readonly ILogger<AdminAccountStore> _logger;
    private readonly PasswordHasher<AdminAccount> _hasher = new();

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public AdminAccountStore(ContentPaths paths, ILogger<AdminAccountStore> logger)
    {
        _paths = paths;
        _logger = logger;
    }

    public bool HasAccount => File.Exists(_paths.AdminFile);

    public async Task<AdminAccount?> GetAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_paths.AdminFile))
        {
            return null;
        }

        try
        {
            await using var stream = File.OpenRead(_paths.AdminFile);
            return await JsonSerializer.DeserializeAsync<AdminAccount>(stream, Options, cancellationToken);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Không đọc được {Path}", _paths.AdminFile);
            return null;
        }
    }

    /// <summary>
    /// Kiểm mật khẩu. Trả về tài khoản khi đúng, null khi sai — kể cả khi chưa có
    /// tài khoản nào, để trang đăng nhập không nói cho người lạ biết site đã đặt
    /// mật khẩu hay chưa.
    /// </summary>
    public async Task<AdminAccount?> VerifyAsync(
        string userName,
        string password,
        CancellationToken cancellationToken = default)
    {
        var account = await GetAsync(cancellationToken);

        if (account is null || string.IsNullOrEmpty(account.PasswordHash))
        {
            return null;
        }

        if (!string.Equals(account.UserName, userName, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var result = _hasher.VerifyHashedPassword(account, account.PasswordHash, password);

        if (result == PasswordVerificationResult.Failed)
        {
            return null;
        }

        // SuccessRehashNeeded: bản băm dựng bằng tham số cũ hơn mặc định hiện tại.
        // Băm lại ngay lúc còn cầm mật khẩu gốc trong tay, vì lần sau không có nữa.
        // Giữ nguyên dấu bảo mật: đây không phải lần đổi mật khẩu, không có lý do
        // gì đá những phiên đang mở ra ngoài.
        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            account.PasswordHash = _hasher.HashPassword(account, password);
            await SaveAsync(account, cancellationToken);
        }

        return account;
    }

    /// <summary>
    /// Đặt mật khẩu mới, sinh dấu bảo mật mới. Dùng cho cả lệnh dòng lệnh lẫn
    /// trang đổi mật khẩu.
    /// </summary>
    public async Task<AdminAccount> SetPasswordAsync(
        string userName,
        string password,
        CancellationToken cancellationToken = default)
    {
        var account = new AdminAccount
        {
            UserName = userName,
            SecurityStamp = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)),
            UpdatedAt = DateTimeOffset.Now
        };

        account.PasswordHash = _hasher.HashPassword(account, password);

        await SaveAsync(account, cancellationToken);

        return account;
    }

    /// <summary>
    /// Đổi mật khẩu từ trong trang quản trị. Trả về tài khoản mới khi xong, null
    /// khi mật khẩu hiện tại sai.
    /// </summary>
    public async Task<AdminAccount?> ChangePasswordAsync(
        string userName,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        var account = await VerifyAsync(userName, currentPassword, cancellationToken);

        if (account is null)
        {
            return null;
        }

        _logger.LogInformation("{UserName} đã đổi mật khẩu quản trị", account.UserName);

        return await SetPasswordAsync(account.UserName, newPassword, cancellationToken);
    }

    private async Task SaveAsync(AdminAccount account, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_paths.AdminFile)!);

        await using var stream = File.Create(_paths.AdminFile);
        await JsonSerializer.SerializeAsync(stream, account, Options, cancellationToken);
    }
}
