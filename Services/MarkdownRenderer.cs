using System.Text;
using System.Text.RegularExpressions;
using Markdig;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace VoTrongNghia.Services;

/// <summary>
/// Dựng HTML cho bài viết và bài học từ Markdown. Chép từ Dokma, thêm phần mục lục.
///
/// <para>Không có trình soạn thảo kéo thả: loại đó sinh HTML tuỳ ý, mà HTML tuỳ ý trên
/// trang công khai là chỗ chèn script. Markdown đủ cho bài hướng dẫn: đề mục, in đậm,
/// danh sách, link, ảnh, bảng, khối code.</para>
///
/// <para>Ba lớp chặn:</para>
/// <list type="number">
/// <item><c>DisableHtml</c>: thẻ HTML gõ vào bài hiện ra thành chữ, không thành thẻ.</item>
/// <item>Link và ảnh chỉ nhận http(s), mailto, tel, đường dẫn trong site, neo <c>#</c>.
/// <c>[bấm](javascript:...)</c> thành link chết.</item>
/// <item>Link ra ngoài site mở tab mới, gắn <c>nofollow noopener</c>.</item>
/// </list>
/// </summary>
public static partial class MarkdownRenderer
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .DisableHtml()
        .UsePipeTables()
        .UseEmphasisExtras()
        .UseAutoLinks()
        .UseSoftlineBreakAsHardlineBreak()
        .Build();

    /// <summary>Một đề mục trong bài, cho khung mục lục.</summary>
    public sealed record Heading(string Id, string Text, int Level);

    /// <summary>HTML của bài và các đề mục cấp 2, 3 (đã gắn id) theo thứ tự trong bài.</summary>
    public sealed record Rendered(string Html, IReadOnlyList<Heading> Headings);

    [GeneratedRegex(@"^[a-z][a-z0-9+.-]*:", RegexOptions.IgnoreCase)]
    private static partial Regex SchemePattern();

    /// <summary>Chỗ trống kiểu <c>[số tài khoản]</c>; link Markdown <c>[chữ](url)</c> không tính.</summary>
    [GeneratedRegex(@"\[[^\[\]\n]{1,200}\](?![(\[:])")]
    private static partial Regex PlaceholderPattern();

    public static string ToHtml(string markdown) => Render(markdown).Html;

    public static Rendered Render(string markdown)
    {
        var document = Markdown.Parse(markdown ?? string.Empty, Pipeline);

        foreach (var link in document.Descendants<LinkInline>())
        {
            link.Url = SafeUrl(link.Url, link.IsImage);

            if (!link.IsImage && IsExternal(link.Url))
            {
                var attributes = link.GetAttributes();
                attributes.AddPropertyIfNotExist("target", "_blank");
                attributes.AddPropertyIfNotExist("rel", "nofollow noopener");
            }

            if (link.IsImage)
            {
                link.GetAttributes().AddPropertyIfNotExist("loading", "lazy");
            }
        }

        foreach (var link in document.Descendants<AutolinkInline>())
        {
            if (SafeUrl(link.Url, false) == "#")
            {
                link.Url = "#";
            }
        }

        // Gắn id cho đề mục cấp 2, 3 để làm mục lục và để chia sẻ link tới đúng đoạn. Id
        // không dấu, cùng cách tính với bản tĩnh cũ, nên link cũ dạng #cai-dat vẫn đúng.
        var headings = new List<Heading>();
        var used = new HashSet<string>(StringComparer.Ordinal);

        foreach (var block in document.Descendants<HeadingBlock>())
        {
            if (block.Level is not (2 or 3))
            {
                continue;
            }

            var text = InlineText(block.Inline);
            var id = Slugs.From(text);

            if (id.Length == 0)
            {
                continue;
            }

            // Hai đề mục trùng chữ thì đề mục sau thêm số, không thì link #id chỉ tới được cái đầu.
            var unique = id;
            for (var n = 2; !used.Add(unique); n++)
            {
                unique = $"{id}-{n}";
            }

            block.GetAttributes().Id = unique;
            headings.Add(new Heading(unique, text, block.Level));
        }

        return new Rendered(document.ToHtml(Pipeline), headings);
    }

    /// <summary>
    /// Chỗ trống đầu tiên còn sót trong bài, hoặc null. Bỏ qua khối code và code trong câu:
    /// bài hướng dẫn prompt đầy mẫu kiểu <c>Bạn là [chuyên gia]</c> — đó là nội dung thật,
    /// không phải chỗ quên điền.
    /// </summary>
    public static string? FindPlaceholder(string markdown)
    {
        var text = Regex.Replace(markdown ?? string.Empty, @"```.*?```", string.Empty, RegexOptions.Singleline);
        text = Regex.Replace(text, @"`[^`\n]*`", string.Empty);

        var match = PlaceholderPattern().Match(text);
        return match.Success ? match.Value : null;
    }

    /// <summary>Số chữ (tiếng Việt: số âm tiết) của phần chữ thuần, để tính thời gian đọc.</summary>
    public static int CountWords(string markdown) =>
        string.IsNullOrWhiteSpace(markdown)
            ? 0
            : Markdown.ToPlainText(markdown, Pipeline).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;

    /// <summary>Chữ thuần từ Markdown, cho thẻ meta khi bài chưa có tóm tắt.</summary>
    public static string ToPlainText(string markdown, int max)
    {
        var text = Markdown.ToPlainText(markdown ?? string.Empty, Pipeline);
        text = Regex.Replace(text, @"\s+", " ").Trim();

        if (text.Length <= max)
        {
            return text;
        }

        // Cắt ở dấu cách gần nhất để không đứt giữa chữ; không có dấu cách thì cắt cứng.
        var cut = text.LastIndexOf(' ', max);
        return text[..(cut > 0 ? cut : max)].TrimEnd(',', '.', ';') + "…";
    }

    private static string InlineText(ContainerInline? container)
    {
        var builder = new StringBuilder();

        if (container is null)
        {
            return string.Empty;
        }

        foreach (var inline in container.Descendants<Inline>())
        {
            switch (inline)
            {
                case LiteralInline literal:
                    builder.Append(literal.Content.ToString());
                    break;
                case CodeInline code:
                    builder.Append(code.Content);
                    break;
            }
        }

        return builder.ToString().Trim();
    }

    private static string SafeUrl(string? url, bool isImage)
    {
        var value = (url ?? string.Empty).Trim();

        if (value.Length == 0)
        {
            return "#";
        }

        if (value.StartsWith('#') && !isImage)
        {
            return value;
        }

        if (value.StartsWith("//"))
        {
            return "#";
        }

        if (SchemePattern().IsMatch(value))
        {
            var ok = value.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                     value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                     (!isImage && (value.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase) ||
                                   value.StartsWith("tel:", StringComparison.OrdinalIgnoreCase)));

            return ok ? value : "#";
        }

        // Đường dẫn tương đối đổi thành tuyệt đối: bài nằm ở /bai-viet/..., để nguyên thì
        // trình duyệt tìm ảnh ở /bai-viet/assets/... và ra ảnh vỡ.
        return value.StartsWith('/') ? value : "/" + value;
    }

    private static bool IsExternal(string url) =>
        url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
        url.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
}
