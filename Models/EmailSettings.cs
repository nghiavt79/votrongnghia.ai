namespace VoTrongNghia.Models;

/// <summary>
/// Cấu hình gửi email qua Gmail, <c>Data/email.json</c>, thiết lập ở <c>/cms/email</c>.
///
/// <para>Ngoài git, ngoài bản publish, không có seed: có mật khẩu ứng dụng Gmail (đã mã hoá
/// bằng Data Protection — xem <c>EmailSender.Protect</c>; khoá mã hoá theo máy, nên chép file
/// sang máy khác là phải nhập lại mật khẩu).</para>
/// </summary>
public sealed class EmailSettings
{
    /// <summary>Tắt là site không gửi gì cả, kể cả khi đã điền đủ.</summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Địa chỉ Gmail: vừa là tài khoản đăng nhập SMTP, vừa là người gửi — Gmail không cho gửi
    /// dưới tên địa chỉ khác tài khoản đăng nhập.
    /// </summary>
    public string GmailAddress { get; set; } = string.Empty;

    /// <summary>Mật khẩu ứng dụng 16 ký tự (không phải mật khẩu Gmail), đã mã hoá. Không bao giờ hiện lại trong form.</summary>
    public string ProtectedPassword { get; set; } = string.Empty;

    /// <summary>Tên hiện ở ô "Từ" trong hộp thư người nhận. Trống thì lấy tên trang.</summary>
    public string FromName { get; set; } = string.Empty;

    /// <summary>Nơi nhận báo có đơn mới, và là địa chỉ người học bấm "Trả lời". Trống thì dùng chính Gmail gửi.</summary>
    public string NotifyAddress { get; set; } = string.Empty;

    /// <summary>Gửi thư xác nhận "đã nhận đơn" cho người đăng ký.</summary>
    public bool SendReceipt { get; set; } = true;

    /// <summary>Báo cho người quản trị mỗi khi có đơn mới.</summary>
    public bool NotifyOwner { get; set; } = true;

    /// <summary>
    /// Máy chủ SMTP. Không có trong form — luôn là Gmail. Chỉ sửa tay trong email.json khi thử
    /// với máy chủ SMTP giả trên máy dev (lúc đó tắt <see cref="UseSsl"/>).
    /// </summary>
    public string Host { get; set; } = "smtp.gmail.com";

    /// <summary>587 + STARTTLS: cổng Gmail khuyên dùng. Cổng 465 không dùng được với SmtpClient.</summary>
    public int Port { get; set; } = 587;

    public bool UseSsl { get; set; } = true;

    /// <summary>Lần gửi thư thử gần nhất và kết quả, để trang thiết lập đánh dấu bước cuối đã xong.</summary>
    public DateTimeOffset? LastTestAt { get; set; }
    public string? LastTestError { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public bool IsReady => Enabled && GmailAddress.Length > 0 && ProtectedPassword.Length > 0;

    public string OwnerAddress => NotifyAddress.Length > 0 ? NotifyAddress : GmailAddress;
}

/// <summary>Một lần gửi email gắn với một đơn đăng ký, hiện ở trang chi tiết đơn.</summary>
public sealed class EmailLogEntry
{
    public const string KindReceipt = "xac-nhan";
    public const string KindOwner = "bao-don-moi";
    public const string KindResult = "ket-qua";
    public const string KindCustom = "tu-soan";

    public DateTimeOffset At { get; set; }
    public string Kind { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;

    /// <summary>Null là gửi được; có chữ là câu lỗi.</summary>
    public string? Error { get; set; }

    public string KindLabel => Kind switch
    {
        KindReceipt => "Xác nhận đã nhận đơn",
        KindOwner => "Báo bạn có đơn mới",
        KindResult => "Báo kết quả",
        _ => "Thư tự soạn"
    };
}
