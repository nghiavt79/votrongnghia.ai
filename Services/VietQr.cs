using System.Globalization;
using System.Text;
using QRCoder;
using VoTrongNghia.Models;

namespace VoTrongNghia.Services;

/// <summary>
/// Mã QR chuyển khoản theo chuẩn VietQR (NAPAS 247, dựa trên EMVCo): quét bằng app ngân hàng nào ở
/// Việt Nam cũng ra sẵn ngân hàng và số tài khoản, app tự hiện tên chủ tài khoản để người chuyển kiểm.
///
/// <para>Dựng ngay trên máy chủ từ thông tin ở <c>/cms/thong-tin</c> chứ không lấy ảnh của dịch vụ
/// ngoài (img.vietqr.io…): đổi tài khoản là QR đổi theo, không phải mở thêm nguồn ảnh trong CSP, và
/// không gửi số tài khoản đi đâu. Mã tĩnh, không ghi số tiền — người ủng hộ tự nhập.</para>
/// </summary>
public static class VietQr
{
    /// <summary>Nội dung chuyển khoản điền sẵn. Không dấu: nhiều app ngân hàng cắt chữ có dấu.</summary>
    public const string TransferNote = "Ung ho votrongnghia";

    /// <summary>
    /// Mã BIN (mã ngân hàng của NAPAS) của các ngân hàng hay dùng, tra theo tên gõ ở /cms. Ngân hàng
    /// không có ở đây thì không có QR — mục Ủng hộ vẫn hiện số tài khoản như cũ.
    /// </summary>
    private static readonly (string Bin, string Name, string[] Aliases)[] Banks =
    [
        ("970436", "Vietcombank", ["vietcombank", "vcb", "ngoaithuong"]),
        ("970415", "VietinBank", ["vietinbank", "ctg", "congthuong"]),
        ("970418", "BIDV", ["bidv", "dautuvaphattrien"]),
        ("970405", "Agribank", ["agribank", "nongnghiep"]),
        ("970407", "Techcombank", ["techcombank", "tcb"]),
        ("970422", "MB", ["mb", "mbbank", "quandoi"]),
        ("970416", "ACB", ["acb", "achau"]),
        ("970432", "VPBank", ["vpbank", "vietnamthinhvuong"]),
        ("970423", "TPBank", ["tpbank", "tienphong"]),
        ("970403", "Sacombank", ["sacombank", "stb"]),
        ("970441", "VIB", ["vib", "quocte"]),
        ("970437", "HDBank", ["hdbank"]),
        ("970443", "SHB", ["shb"]),
        ("970448", "OCB", ["ocb", "phuongdong"]),
        ("970426", "MSB", ["msb", "maritimebank", "hanghai"]),
        ("970440", "SeABank", ["seabank"]),
        ("970431", "Eximbank", ["eximbank", "eib"]),
        ("970449", "LPBank", ["lpbank", "lienvietpostbank", "lienviet"]),
    ];

    /// <summary>Tên các ngân hàng nhận ra được, cho câu hướng dẫn ở /cms.</summary>
    public static string SupportedBanks => string.Join(", ", Banks.Select(bank => bank.Name));

    /// <summary>Tìm ngân hàng theo tên gõ tự do: "Vietcombank", "VCB", "Ngân hàng Ngoại thương"… Null khi không nhận ra.</summary>
    public static (string Bin, string Name)? FindBank(string? name)
    {
        var key = Simplify(name);

        if (key.Length == 0)
        {
            return null;
        }

        // Bỏ chữ "ngân hàng" / "bank" ở đầu: "Ngân hàng TMCP Ngoại thương" → "tmcpngoaithuong".
        key = key.Replace("nganhang", "").Replace("tmcp", "").Replace("thuongmaicophan", "");

        foreach (var (bin, display, aliases) in Banks)
        {
            if (aliases.Any(alias => key == alias || key.StartsWith(alias, StringComparison.Ordinal) && alias.Length >= 4 ||
                                     key.Contains(alias, StringComparison.Ordinal) && alias.Length >= 6))
            {
                return (bin, display);
            }
        }

        return null;
    }

    /// <summary>Chuỗi VietQR của tài khoản ủng hộ, hoặc null khi chưa đủ thông tin / không nhận ra ngân hàng.</summary>
    public static string? Payload(SiteInfo site)
    {
        var account = new string(site.BankNumber.Where(char.IsAsciiLetterOrDigit).ToArray());

        if (account.Length is < 4 or > 19 || FindBank(site.BankName) is not { } bank)
        {
            return null;
        }

        var merchant = Tlv("00", "A000000727") +             // định danh NAPAS
                       Tlv("01", Tlv("00", bank.Bin) + Tlv("01", account)) +
                       Tlv("02", "QRIBFTTA");                  // chuyển tới số tài khoản

        var payload = Tlv("00", "01") +                        // phiên bản
                      Tlv("01", "11") +                        // mã tĩnh, dùng nhiều lần
                      Tlv("38", merchant) +
                      Tlv("53", "704") +                       // VND
                      Tlv("58", "VN") +
                      Tlv("62", Tlv("08", TransferNote)) +     // nội dung chuyển khoản
                      "6304";

        return payload + Crc16(payload).ToString("X4", CultureInfo.InvariantCulture);
    }

    /// <summary>SVG để nhúng thẳng vào trang: nét sắc ở mọi cỡ màn hình, không cần file ảnh.</summary>
    public static string Svg(string payload)
    {
        using var data = new QRCodeGenerator().CreateQrCode(payload, QRCodeGenerator.ECCLevel.M);
        return new SvgQRCode(data).GetGraphic(8, "#000000", "#ffffff", drawQuietZones: true);
    }

    /// <summary>Ảnh PNG để tải về, gửi qua Zalo / Facebook. Đủ lớn để in ra giấy vẫn quét được.</summary>
    public static byte[] Png(string payload)
    {
        using var data = new QRCodeGenerator().CreateQrCode(payload, QRCodeGenerator.ECCLevel.M);
        return new PngByteQRCode(data).GetGraphic(16);
    }

    private static string Tlv(string id, string value) => id + value.Length.ToString("00", CultureInfo.InvariantCulture) + value;

    /// <summary>CRC-16/CCITT-FALSE (đa thức 0x1021, khởi đầu 0xFFFF) — trường 63 của chuẩn EMVCo.</summary>
    public static ushort Crc16(string text)
    {
        ushort crc = 0xFFFF;

        foreach (var b in Encoding.ASCII.GetBytes(text))
        {
            crc ^= (ushort)(b << 8);

            for (var i = 0; i < 8; i++)
            {
                crc = (crc & 0x8000) != 0 ? (ushort)((crc << 1) ^ 0x1021) : (ushort)(crc << 1);
            }
        }

        return crc;
    }

    private static string Simplify(string? text) =>
        new string((text ?? string.Empty).ToLowerInvariant().Replace('đ', 'd')
            .Normalize(NormalizationForm.FormD)
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark && char.IsAsciiLetterOrDigit(c))
            .ToArray());
}
