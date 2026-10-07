namespace VoTrongNghia.Models;

/// <summary>
/// Thông tin chung của trang, <c>Data/site.json</c>, sửa ở <c>/cms/thong-tin</c>.
///
/// <para>Quy ước chung: ô link nào để trống thì phần đó tự ẩn trên site (nút Đăng ký
/// kênh, mục Video, mạng xã hội, mục Ủng hộ…). Không bao giờ hiện link mẫu kiểu
/// "your-profile" cho khách bấm vào.</para>
/// </summary>
public sealed class SiteInfo
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Dòng chức danh dưới tên ở phần giới thiệu.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Viên nhãn nhỏ phía trên tiêu đề lớn ở đầu trang chủ.</summary>
    public string Badge { get; set; } = string.Empty;

    /// <summary>Dòng đầu của tiêu đề lớn; dòng sau là tên, tô màu gradient.</summary>
    public string HeroLead { get; set; } = string.Empty;
    public string HeroDescription { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;

    /// <summary>Ảnh đại diện, đường dẫn tương đối không gạch chéo đầu như Dokma.</summary>
    public string Avatar { get; set; } = "assets/img/avatar.svg";

    /// <summary>Email liên hệ hiện công khai. Trống thì ẩn mục Liên hệ.</summary>
    public string Email { get; set; } = string.Empty;

    public string Youtube { get; set; } = string.Empty;
    public string Facebook { get; set; } = string.Empty;
    public string FacebookGroup { get; set; } = string.Empty;
    public string Tiktok { get; set; } = string.Empty;
    public string Linkedin { get; set; } = string.Empty;
    public string X { get; set; } = string.Empty;
    public string Threads { get; set; } = string.Empty;

    public string BankName { get; set; } = string.Empty;
    public string BankNumber { get; set; } = string.Empty;
    public string BankOwner { get; set; } = string.Empty;
    public string Paypal { get; set; } = string.Empty;

    /// <summary>
    /// Bật mục "Ủng hộ một ly cafe". Mặc định tắt: tinh thần của trang là miễn phí, lan
    /// tỏa; tắt thì nút nổi góc phải thành "Chia sẻ trang này".
    /// </summary>
    public bool Donate { get; set; }

    public List<StoryStep> Story { get; set; } = [];
    public List<ToolLink> Tools { get; set; } = [];
    public List<VideoLink> Videos { get; set; } = [];

    /// <summary>Mục Video chỉ hiện khi đã có kênh và có ít nhất một video.</summary>
    public bool ShowVideos => Youtube.Length > 0 && Videos.Count > 0;

    public bool ShowDonate => Donate && (BankNumber.Length > 0 || Paypal.Length > 0);

    /// <summary>
    /// Các mạng xã hội đã điền, theo thứ tự cố định. Danh sách nền tảng cố định trong mã
    /// chứ không cho thêm bớt: mỗi nền tảng cần icon và câu mô tả riêng.
    /// </summary>
    public IEnumerable<(string Name, string Description, string Icon, string Url)> Socials()
    {
        var all = new[]
        {
            ("YouTube", "Video hướng dẫn AI", "▶️", Youtube),
            ("Facebook", "Cập nhật hằng ngày", "📘", Facebook),
            ("TikTok", "Video AI ngắn, mẹo nhanh", "🎵", Tiktok),
            ("LinkedIn", "Kết nối chuyên môn", "💼", Linkedin),
            ("X (Twitter)", "Tin AI nhanh mỗi ngày", "✖️", X),
            ("Threads", "Trò chuyện & chia sẻ", "🧵", Threads),
        };

        return all.Where(item => item.Item4.Length > 0);
    }
}

/// <summary>Một mốc trong "Hành trình của mình".</summary>
public sealed class StoryStep
{
    public string Icon { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

/// <summary>Một công cụ AI trong mục "Công cụ AI tôi sử dụng".</summary>
public sealed class ToolLink
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}

/// <summary>Một video YouTube. Chỉ cần mã video (phần sau "v=" trong link).</summary>
public sealed class VideoLink
{
    public string Title { get; set; } = string.Empty;
    public string YoutubeId { get; set; } = string.Empty;
}
