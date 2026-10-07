using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using VoTrongNghia.Models;

namespace VoTrongNghia.Services;

/// <summary>Một thư văn bản thuần. Thư thuần chữ ít bị lọc vào Spam hơn thư HTML, và đọc tốt trên mọi máy.</summary>
public sealed record EmailMessage(string To, string Subject, string Body, string? ReplyTo = null);

/// <summary>
/// Gửi email qua Gmail (SMTP <c>smtp.gmail.com:587</c>, STARTTLS) bằng mật khẩu ứng dụng.
///
/// <para>Dùng <see cref="SmtpClient"/> có sẵn trong .NET chứ không kéo thêm thư viện: chỉ gửi
/// vài chục thư một ngày tới một máy chủ duy nhất, SmtpClient làm đủ. Giới hạn của nó (không
/// có SSL ngay từ đầu ở cổng 465) không chạm tới Gmail cổng 587.</para>
///
/// <para>Mật khẩu ứng dụng lưu đã mã hoá bằng Data Protection — cùng khoá đã dùng cho cookie
/// đăng nhập (Data/keys, DPAPI của máy trên Windows). Ai lấy được email.json mà không có máy
/// chủ thì cũng không đọc được mật khẩu.</para>
/// </summary>
public sealed class EmailSender
{
    private readonly IDataProtector _protector;
    private readonly ILogger<EmailSender> _logger;

    public EmailSender(ContentPaths paths, IDataProtectionProvider protection, ILogger<EmailSender> logger)
    {
        _protector = protection.CreateProtector("VoTrongNghia.Email.GmailAppPassword");
        _logger = logger;

        // Không có seed: máy chủ mới thì thiết lập lại ở /cms/email.
        Settings = new JsonFileStore<EmailSettings>(
            paths.EmailFile,
            Path.Combine(paths.SeedDirectory, "khong-co-seed.json"),
            paths.BackupsDirectory,
            logger);
    }

    public JsonFileStore<EmailSettings> Settings { get; }

    public string Protect(string password) => _protector.Protect(password);

    /// <summary>Gửi một thư. Trả về null khi gửi được, câu lỗi tiếng Việt khi không.</summary>
    public async Task<string?> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var settings = await Settings.ReadAsync(cancellationToken);

        if (!settings.IsReady)
        {
            return "Chưa bật gửi email — thiết lập ở /cms/email.";
        }

        string password;

        try
        {
            password = _protector.Unprotect(settings.ProtectedPassword);
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            // Khoá Data Protection đổi (chép email.json sang máy khác, hoặc mất Data/keys).
            return "Không giải mã được mật khẩu ứng dụng đã lưu (máy chủ đổi khoá). Nhập lại mật khẩu ở /cms/email.";
        }

        try
        {
            using var client = new SmtpClient(settings.Host, settings.Port)
            {
                EnableSsl = settings.UseSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(settings.GmailAddress, password),
                Timeout = 20000
            };

            var siteName = settings.FromName.Length > 0 ? settings.FromName : "votrongnghia.vn";

            using var mail = new MailMessage
            {
                From = new MailAddress(settings.GmailAddress, siteName, Encoding.UTF8),
                // Tiêu đề một dòng: xuống dòng trong tiêu đề là chỗ chèn thêm header thư.
                Subject = OneLine(message.Subject),
                SubjectEncoding = Encoding.UTF8,
                Body = message.Body,
                BodyEncoding = Encoding.UTF8,
                IsBodyHtml = false
            };

            mail.To.Add(new MailAddress(message.To));

            if (!string.IsNullOrEmpty(message.ReplyTo))
            {
                mail.ReplyToList.Add(new MailAddress(message.ReplyTo));
            }

            await client.SendMailAsync(mail, cancellationToken);
            _logger.LogInformation("Đã gửi email \"{Subject}\" tới {To}", mail.Subject, message.To);

            return null;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (ex is SmtpException or FormatException or InvalidOperationException or IOException)
        {
            _logger.LogWarning(ex, "Không gửi được email tới {To}", message.To);
            return Explain(ex, settings);
        }
    }

    /// <summary>Câu lỗi người không rành kỹ thuật đọc được, kèm việc cần làm.</summary>
    private static string Explain(Exception ex, EmailSettings settings)
    {
        var text = (ex.InnerException?.Message ?? string.Empty) + " " + ex.Message;

        if (ex is FormatException)
        {
            return "Địa chỉ email không đúng định dạng.";
        }

        if (text.Contains("5.7.8") || text.Contains("Username and Password not accepted") || text.Contains("5.7.0 Authentication Required"))
        {
            return "Gmail từ chối đăng nhập. Kiểm tra lại địa chỉ Gmail và mật khẩu ứng dụng — phải là mật khẩu ứng dụng 16 ký tự, không phải mật khẩu Gmail thường, và tài khoản phải đang bật xác minh 2 bước.";
        }

        if (text.Contains("5.4.5") || text.Contains("sending limit", StringComparison.OrdinalIgnoreCase))
        {
            return "Gmail báo đã vượt giới hạn gửi trong ngày (khoảng 500 thư). Chờ 24 giờ rồi gửi lại.";
        }

        if (ex is SmtpException { StatusCode: SmtpStatusCode.MailboxUnavailable or SmtpStatusCode.MailboxNameNotAllowed })
        {
            return "Gmail không giao được tới địa chỉ người nhận — kiểm tra lại email người nhận.";
        }

        if (ex is SmtpException { InnerException: IOException or System.Net.Sockets.SocketException } || text.Contains("timed out", StringComparison.OrdinalIgnoreCase))
        {
            return $"Không kết nối được {settings.Host}:{settings.Port}. Máy chủ có thể đang chặn cổng {settings.Port} đi ra ngoài — hỏi bên cho thuê máy chủ.";
        }

        return "Gửi email lỗi: " + ex.Message;
    }

    private static string OneLine(string text) => text.Replace('\r', ' ').Replace('\n', ' ').Trim();
}
