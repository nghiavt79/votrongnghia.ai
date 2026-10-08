using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using VoTrongNghia.Models;

namespace VoTrongNghia.Services;

/// <summary>
/// Link đăng nhập học viên: <c>/hoc-vien/vao?ma=…</c>. Thay cho mật khẩu — không có gì để quên,
/// để lộ, để lưu.
///
/// <para>Mã ngẫu nhiên 32 byte, file <c>Data/dang-nhap.json</c> chỉ giữ băm SHA-256 của nó. Mã dùng
/// được một lần và có hạn: link tự xin ở trang đăng nhập hạn 30 phút; link đi kèm thư báo duyệt
/// hoặc người quản trị tạo để gửi tay qua Zalo hạn 7 ngày, vì người nhận chưa chắc mở thư ngay.</para>
/// </summary>
public sealed class LoginLinkService
{
    public static readonly TimeSpan ShortLifetime = TimeSpan.FromMinutes(30);
    public static readonly TimeSpan LongLifetime = TimeSpan.FromDays(7);

    /// <summary>Mỗi email tự xin tối đa chừng này link trong <see cref="RequestWindow"/>.</summary>
    public const int MaxRequestsPerWindow = 3;
    public static readonly TimeSpan RequestWindow = TimeSpan.FromMinutes(15);

    private readonly JsonFileStore<List<LoginToken>> _tokens;

    public LoginLinkService(ContentPaths paths, ILogger<LoginLinkService> logger)
    {
        // Không sao lưu: mã băm hết hạn sau vài phút, bản cũ không có gì để khôi phục.
        _tokens = new JsonFileStore<List<LoginToken>>(
            paths.LoginTokensFile,
            Path.Combine(paths.SeedDirectory, "khong-co-seed.json"),
            paths.BackupsDirectory,
            logger,
            keepBackups: false);
    }

    public static string Url(string baseUrl, string token) => $"{baseUrl.TrimEnd('/')}/hoc-vien/vao?ma={token}";

    /// <summary>
    /// Tạo một mã mới cho học viên, trả về mã thô (chỉ có trong link, không lưu ở đâu). Null khi
    /// <paramref name="limitRequests"/> bật và email này đã xin quá nhiều link gần đây.
    /// </summary>
    public async Task<string?> CreateAsync(string learnerId, TimeSpan lifetime, bool limitRequests, CancellationToken cancellationToken = default)
    {
        var token = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var now = SiteTime.Now;

        var created = await _tokens.UpdateAsync(list =>
        {
            list.RemoveAll(item => item.ExpiresAt <= now);

            if (limitRequests && list.Count(item => item.LearnerId == learnerId && item.CreatedAt > now - RequestWindow) >= MaxRequestsPerWindow)
            {
                return false;
            }

            list.Add(new LoginToken { Hash = Hash(token), LearnerId = learnerId, CreatedAt = now, ExpiresAt = now + lifetime });
            return true;
        }, cancellationToken);

        return created ? token : null;
    }

    /// <summary>Mã còn dùng được thì trả về mã học viên — không dùng mất mã (trang xác nhận trước khi vào).</summary>
    public async Task<string?> PeekAsync(string? token, CancellationToken cancellationToken = default) =>
        Find(await _tokens.ReadAsync(cancellationToken), token)?.LearnerId;

    /// <summary>Dùng mã: trả về mã học viên và xoá mã đi, để link không vào được lần thứ hai.</summary>
    public async Task<string?> ConsumeAsync(string? token, CancellationToken cancellationToken = default)
    {
        string? learnerId = null;

        await _tokens.UpdateAsync(list =>
        {
            var item = Find(list, token);

            if (item is null)
            {
                return false;
            }

            learnerId = item.LearnerId;
            list.Remove(item);
            return true;
        }, cancellationToken);

        return learnerId;
    }

    /// <summary>Huỷ mọi link còn hạn của một học viên (khoá, xoá tài khoản).</summary>
    public Task RevokeAsync(string learnerId, CancellationToken cancellationToken = default) =>
        _tokens.UpdateAsync(list => list.RemoveAll(item => item.LearnerId == learnerId) > 0, cancellationToken);

    /// <summary>
    /// Tìm mã còn hạn. So từng băm với thời gian cố định thay vì tra từ điển: không để thời gian
    /// phản hồi tiết lộ mã đoán được khớp bao nhiêu ký tự đầu.
    /// </summary>
    private static LoginToken? Find(List<LoginToken> list, string? token)
    {
        if (string.IsNullOrEmpty(token) || token.Length > 100)
        {
            return null;
        }

        var hash = Encoding.ASCII.GetBytes(Hash(token));
        var now = SiteTime.Now;
        LoginToken? found = null;

        foreach (var item in list)
        {
            if (CryptographicOperations.FixedTimeEquals(hash, Encoding.ASCII.GetBytes(item.Hash)) && item.ExpiresAt > now)
            {
                found = item;
            }
        }

        return found;
    }

    private static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
