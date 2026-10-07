using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace VoTrongNghia.Services;

public sealed class ImageUploadException : Exception
{
    public ImageUploadException(string message) : base(message)
    {
    }
}

/// <summary>
/// Nhận ảnh tải lên từ /cms. Chép từ Dokma, gọn lại thành một hàm cho mọi loại ảnh.
///
/// <para>Ba việc bắt buộc làm ở đây chứ không phó thác cho người tải lên:</para>
/// <list type="number">
/// <item>Giải mã thật để biết đây có phải ảnh không. Đuôi file ai cũng đổi được.</item>
/// <item>Xoá EXIF. Ảnh chụp bằng điện thoại mang theo toạ độ GPS của nhà riêng. Không có lý
/// do gì để thứ đó lên web.</item>
/// <item>Đặt tên file. Tên do người tải lên chọn có thể chứa đường dẫn
/// (<c>../../appsettings.json</c>) hoặc ký tự làm hỏng URL.</item>
/// </list>
///
/// <para>Mọi ảnh lưu JPG nền trắng: ảnh chụp màn hình PNG nặng gấp nhiều lần mà không đẹp
/// hơn bao nhiêu trên trang bài viết.</para>
/// </summary>
public sealed class ImageService
{
    private const int JpegQuality = 84;

    public const long MaxUploadBytes = 12 * 1024 * 1024;

    /// <summary>Số ảnh tối đa trong một lần tải, để một request không vượt giới hạn dung lượng.</summary>
    public const int MaxFilesPerUpload = 5;

    /// <summary>Loại ảnh: quyết định thư mục, cỡ tối đa và cách cắt.</summary>
    public enum Kind
    {
        /// <summary>Ảnh đại diện: cắt vuông 512px, hiện tròn ở header, phần giới thiệu, tác giả bài.</summary>
        Avatar,

        /// <summary>Ảnh bìa bài viết: thẻ bài, đầu bài, ảnh khi chia sẻ Facebook / Zalo (og:image).</summary>
        Cover,

        /// <summary>Ảnh trong thân bài viết. Khung bài rộng khoảng 760px, gấp đôi cho màn hình nét.</summary>
        Post,

        /// <summary>Ảnh trong bài học, cùng khổ với ảnh trong bài viết.</summary>
        Lesson
    }

    private static readonly IReadOnlyDictionary<Kind, (string Folder, string Prefix)> Folders = new Dictionary<Kind, (string, string)>
    {
        [Kind.Avatar] = ("trang", "dai-dien"),
        [Kind.Cover] = ("bai-viet", "bia"),
        [Kind.Post] = ("bai-viet", "anh"),
        [Kind.Lesson] = ("khoa-hoc", "anh"),
    };

    /// <summary>Tiền tố mọi đường dẫn ảnh tải lên, tương đối với gốc site, không gạch chéo đầu.</summary>
    public const string PublicPrefix = "uploads/";

    private readonly ContentPaths _paths;
    private readonly ILogger<ImageService> _logger;

    public ImageService(ContentPaths paths, ILogger<ImageService> logger)
    {
        _paths = paths;
        _logger = logger;
    }

    /// <summary>Xử lý một ảnh, trả về đường dẫn tương đối kiểu <c>uploads/bai-viet/anh-….jpg</c>.</summary>
    public async Task<string> AddAsync(Kind kind, IFormFile upload, CancellationToken cancellationToken = default)
    {
        using var image = await PrepareAsync(upload, cancellationToken);

        switch (kind)
        {
            case Kind.Avatar:
                // Cắt giữa thành hình vuông: ảnh đại diện hiện trong khung tròn, ảnh chữ nhật
                // mà co vào là méo mặt.
                image.Mutate(context => context.Resize(new ResizeOptions { Mode = ResizeMode.Crop, Size = new Size(512, 512) }));
                break;
            case Kind.Cover:
                // 1200×630 là khổ Facebook / Zalo dùng cho ảnh xem trước link.
                image.Mutate(context => context.Resize(new ResizeOptions { Mode = ResizeMode.Crop, Size = new Size(1200, 630) }));
                break;
            default:
                Shrink(image, 1600);
                break;
        }

        image.Mutate(context => context.BackgroundColor(Color.White));

        var (folder, prefix) = Folders[kind];
        var directory = Path.Combine(_paths.UploadsDirectory, folder);
        Directory.CreateDirectory(directory);

        // Tên theo thời điểm chứ không theo tên gốc: mỗi lần thay ảnh là một tên mới, trình
        // duyệt không bao giờ hiện nhầm ảnh cũ đã giữ trong bộ nhớ đệm.
        var fileName = $"{prefix}-{DateTime.Now:yyyyMMdd-HHmmss}-{Random.Shared.Next(1000, 9999)}.jpg";
        await image.SaveAsJpegAsync(Path.Combine(directory, fileName), new JpegEncoder { Quality = JpegQuality }, cancellationToken);

        _logger.LogInformation("Đã thêm ảnh {Folder}/{File}", folder, fileName);

        return $"{PublicPrefix}{folder}/{fileName}";
    }

    /// <summary>
    /// Xoá file của một ảnh đã gỡ. Chỉ xoá ảnh trong <c>uploads/</c>: ảnh trong <c>assets/</c>
    /// (ảnh đại diện mặc định) là một phần mã nguồn, đi theo mỗi lần deploy.
    /// </summary>
    public void Delete(string? url)
    {
        if (string.IsNullOrEmpty(url) || !url.StartsWith(PublicPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var segments = url[PublicPrefix.Length..].Split('/');

        // Đường dẫn này đi vòng qua form nên vẫn là dữ liệu người dùng gửi lên, dù chính site
        // vừa sinh ra nó: chỉ nhận đúng "thư-mục-đã-biết/tên-file", không có ../
        if (segments.Length != 2 || Folders.Values.All(item => item.Folder != segments[0]) || !IsPlainName(segments[1]))
        {
            return;
        }

        var path = Path.Combine(_paths.UploadsDirectory, segments[0], segments[1]);

        if (File.Exists(path))
        {
            File.Delete(path);
            _logger.LogInformation("Đã xoá ảnh {Url}", url);
        }
    }

    public void Delete(IEnumerable<string> urls)
    {
        foreach (var url in urls)
        {
            Delete(url);
        }
    }

    private static bool IsPlainName(string name) =>
        name.Length > 0 && !name.Contains("..") && Path.GetFileName(name) == name;

    private static void Shrink(Image image, int longEdge)
    {
        if (image.Width > longEdge || image.Height > longEdge)
        {
            image.Mutate(context => context.Resize(new ResizeOptions { Mode = ResizeMode.Max, Size = new Size(longEdge, longEdge) }));
        }
    }

    /// <summary>Kiểm dung lượng, giải mã, xoay đúng chiều, xoá metadata.</summary>
    private static async Task<Image<Rgba32>> PrepareAsync(IFormFile upload, CancellationToken cancellationToken)
    {
        if (upload.Length == 0)
        {
            throw new ImageUploadException("File rỗng, không có gì để tải lên.");
        }

        if (upload.Length > MaxUploadBytes)
        {
            throw new ImageUploadException(
                $"Ảnh \"{Path.GetFileName(upload.FileName)}\" nặng {upload.Length / 1024d / 1024d:0.#}MB, " +
                $"vượt mức {MaxUploadBytes / 1024 / 1024}MB cho một tấm.");
        }

        Image<Rgba32> image;

        await using (var stream = upload.OpenReadStream())
        {
            try
            {
                image = await Image.LoadAsync<Rgba32>(stream, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                // Nhắc HEIC vì đó là nguyên nhân thường gặp nhất: iPhone mặc định chụp HEIC
                // và ImageSharp không đọc định dạng đó.
                throw new ImageUploadException(
                    $"Không đọc được ảnh \"{Path.GetFileName(upload.FileName)}\". " +
                    "Nếu đây là ảnh iPhone định dạng HEIC thì đổi sang JPG rồi tải lại.");
            }
        }

        // Xoay ảnh dọc chụp bằng điện thoại về đúng chiều, rồi mới xoá EXIF — làm ngược thứ
        // tự là mất luôn thông tin chiều xoay và ảnh nằm ngang.
        image.Mutate(context => context.AutoOrient());

        image.Metadata.ExifProfile = null;
        image.Metadata.IptcProfile = null;
        image.Metadata.XmpProfile = null;

        return image;
    }
}
