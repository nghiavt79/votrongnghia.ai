using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace VoTrongNghia.Services;

/// <summary>Bỏ dấu tiếng Việt và dựng đường dẫn bài viết, khóa học.</summary>
public static partial class Slugs
{
    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$")]
    public static partial Regex Pattern();

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonSlug();

    /// <summary>"Bài Viết Đầu" → "bai viet dau". Chữ đ không tách được bằng Unicode nên thay tay.</summary>
    public static string KhongDau(string text)
    {
        var decomposed = text.ToLowerInvariant().Replace('đ', 'd').Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (var ch in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(ch);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    /// <summary>"Vibe code cho người mới" → "vibe-code-cho-nguoi-moi".</summary>
    public static string From(string text) =>
        NonSlug().Replace(KhongDau(text), "-").Trim('-');
}
