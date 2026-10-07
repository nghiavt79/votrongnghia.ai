using System.Globalization;
using VoTrongNghia.Models;
using VoTrongNghia.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace VoTrongNghia.Pages.Admin.BaiViet;

/// <summary>Viết / sửa bài. Chép từ /cms/tin-tuc của Dokma, thêm chủ đề.</summary>
[RequestSizeLimit(64 * 1024 * 1024)]
public class SuaModel : PageModel
{
    private readonly SiteContent _content;
    private readonly ImageService _images;
    private readonly ILogger<SuaModel> _logger;

    public SuaModel(SiteContent content, ImageService images, ILogger<SuaModel> logger)
    {
        _content = content;
        _images = images;
        _logger = logger;
    }

    [BindProperty]
    public PostInput Input { get; set; } = new();

    /// <summary>Bài đang sửa; null khi viết bài mới.</summary>
    public Post? Post { get; private set; }

    public bool IsNew => Post is null;

    /// <summary>Đường dẫn đã khoá vì bài từng lên site.</summary>
    public bool SlugLocked => Post?.FirstPublishedAt is not null;

    /// <summary>HTML xem trước khi bấm "Xem trước", null khi không xem.</summary>
    public string? PreviewHtml { get; private set; }

    /// <summary>Các chủ đề đã dùng ở bài khác, gợi ý để gõ cho thống nhất.</summary>
    public IReadOnlyList<string> KnownTags { get; private set; } = [];

    [TempData]
    public string? Message { get; set; }

    public sealed class PostInput
    {
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;

        public string CoverAlt { get; set; } = string.Empty;

        /// <summary>Chủ đề, cách nhau bằng dấu phẩy: "Vibe code, Người mới".</summary>
        public string Tags { get; set; } = string.Empty;

        /// <summary>Ô date: "2026-10-07".</summary>
        public string Date { get; set; } = string.Empty;

        public bool Published { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(string? slug, CancellationToken cancellationToken)
    {
        if (slug is not null)
        {
            if (!await LoadAsync(slug, cancellationToken))
            {
                return NotFound();
            }

            FillInput(Post!);
        }
        else
        {
            await LoadTagsAsync(cancellationToken);
            Input.Date = SiteTime.Now.ToString("yyyy-MM-dd");
        }

        return Page();
    }

    /// <summary>Dựng thân bài ra HTML để xem, không lưu gì.</summary>
    public async Task<IActionResult> OnPostXemTruocAsync(string? slug, CancellationToken cancellationToken)
    {
        if (slug is not null ? !await LoadAsync(slug, cancellationToken) : !await LoadTagsAsync(cancellationToken))
        {
            return NotFound();
        }

        ModelState.Clear();
        PreviewHtml = MarkdownRenderer.ToHtml(Input.Body);

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? slug, CancellationToken cancellationToken)
    {
        if (slug is not null ? !await LoadAsync(slug, cancellationToken) : !await LoadTagsAsync(cancellationToken))
        {
            return NotFound();
        }

        var posts = await _content.Posts.ReadAsync(cancellationToken);
        var title = Input.Title.Trim();
        var targetSlug = SlugLocked ? Post!.Slug : Input.Slug.Trim().ToLowerInvariant();

        if (title.Length == 0)
        {
            ModelState.AddModelError("Input.Title", "Nhập tiêu đề bài.");
        }
        else if (title.Length > 150)
        {
            ModelState.AddModelError("Input.Title", "Tiêu đề không quá 150 ký tự — Google cắt bớt tiêu đề dài.");
        }

        if (targetSlug.Length == 0)
        {
            targetSlug = Slugs.From(title);
        }

        if (!Slugs.Pattern().IsMatch(targetSlug))
        {
            ModelState.AddModelError("Input.Slug", "Đường dẫn chỉ gồm chữ thường không dấu, số và dấu gạch ngang.");
        }
        else if (posts.Any(item => item.Slug == targetSlug && item.Slug != Post?.Slug))
        {
            ModelState.AddModelError("Input.Slug", "Đường dẫn này đã có bài khác dùng.");
        }

        if (Input.Summary.Trim().Length > 300)
        {
            ModelState.AddModelError("Input.Summary", "Tóm tắt không quá 300 ký tự — đây là đoạn hiện dưới tiêu đề trên Google.");
        }

        if (!DateTime.TryParseExact(Input.Date.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            ModelState.AddModelError("Input.Date", "Chọn ngày đăng.");
        }

        var tags = Input.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(5)
            .ToList();

        // Luật đăng bài: bản nháp thì lưu thoải mái, lên site thì phải đủ.
        if (Input.Published)
        {
            if (Input.Body.Trim().Length < 200)
            {
                ModelState.AddModelError("Input.Body",
                    "Thân bài quá ngắn để đăng (dưới 200 ký tự). Bài vài dòng thì Google coi là trang mỏng và người đọc cũng không được gì.");
            }

            if (MarkdownRenderer.FindPlaceholder(Input.Body) is { } placeholder)
            {
                ModelState.AddModelError("Input.Body", $"Bài còn chỗ trống chưa điền: {placeholder}. Điền hoặc xoá rồi mới đăng.");
            }

            if (Input.Summary.Trim().Length == 0)
            {
                ModelState.AddModelError("Input.Summary", "Viết tóm tắt trước khi đăng — đó là đoạn hiện ở thẻ bài và trên Google.");
            }

            if (!string.IsNullOrEmpty(Post?.Cover) && Input.CoverAlt.Trim().Length == 0)
            {
                ModelState.AddModelError("Input.CoverAlt", "Ảnh bìa chưa có mô tả. Tả ngắn nội dung ảnh, ví dụ: Màn hình Claude Code đang chạy trong terminal.");
            }
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var publishedAt = new DateTimeOffset(date.AddHours(9), SiteTime.VietnamOffset);

        void Apply(Post target)
        {
            target.Title = title;
            target.Slug = targetSlug;
            target.Summary = Input.Summary.Trim();
            target.Body = Input.Body.Replace("\r\n", "\n").Trim();
            target.Tags = tags;
            target.CoverAlt = Input.CoverAlt.Trim();
            target.PublishedAt = target.PublishedAt?.Date == publishedAt.Date ? target.PublishedAt : publishedAt;
            target.Published = Input.Published;
            target.UpdatedAt = SiteTime.Now;

            if (target.Published)
            {
                target.FirstPublishedAt ??= target.UpdatedAt;
            }
        }

        var originalSlug = Post?.Slug;
        var saved = await _content.Posts.UpdateAsync(list =>
        {
            if (list.Any(item => item.Slug == targetSlug && item.Slug != originalSlug))
            {
                return false;
            }

            if (originalSlug is null)
            {
                var post = new Post { CreatedAt = SiteTime.Now };
                Apply(post);
                list.Add(post);
                return true;
            }

            var existing = list.FirstOrDefault(item => item.Slug == originalSlug);

            if (existing is null)
            {
                return false;
            }

            Apply(existing);
            return true;
        }, cancellationToken);

        if (!saved)
        {
            ModelState.AddModelError(string.Empty, "Không lưu được — đường dẫn vừa bị bài khác dùng, hoặc bài vừa bị xoá ở tab khác.");
            return Page();
        }

        Message = Input.Published
            ? (Post?.Published == true ? "Đã lưu bài. Người đọc tải lại trang là thấy." : "Đã đăng bài lên site.")
            : IsNew ? "Đã lưu bản nháp. Giờ tải ảnh bìa và ảnh trong bài được rồi." : "Đã lưu bản nháp — người đọc chưa thấy bài này.";

        return RedirectToPage(new { slug = targetSlug });
    }

    public async Task<IActionResult> OnPostXoaBaiAsync(string slug, string? confirm, CancellationToken cancellationToken)
    {
        if (!await LoadAsync(slug, cancellationToken))
        {
            return NotFound();
        }

        ModelState.Clear();

        if (!string.Equals(confirm?.Trim(), slug, StringComparison.Ordinal))
        {
            ModelState.AddModelError(string.Empty, $"Muốn xoá thì gõ đúng \"{slug}\" vào ô xác nhận.");
            FillInput(Post!);
            return Page();
        }

        await _content.Posts.UpdateAsync(list => list.RemoveAll(item => item.Slug == slug) > 0, cancellationToken);
        _images.Delete(Post!.BodyImages.Append(Post.Cover));

        _logger.LogWarning("Đã xoá bài {Slug}", slug);
        TempData["Message"] = $"Đã xoá bài \"{Post!.Title}\".";

        return RedirectToPage("/Admin/BaiViet/Index");
    }

    /// <summary>Tải ảnh: <paramref name="loai"/> là "bia" (thay ảnh bìa) hoặc "trong-bai".</summary>
    public async Task<IActionResult> OnPostAnhAsync(string slug, string loai, List<IFormFile> files, CancellationToken cancellationToken)
    {
        if (!await LoadAsync(slug, cancellationToken))
        {
            return NotFound();
        }

        ModelState.Clear();
        var isCover = loai == "bia";
        var added = new List<string>();

        foreach (var file in files.Where(file => file.Length > 0).Take(isCover ? 1 : ImageService.MaxFilesPerUpload))
        {
            try
            {
                added.Add(await _images.AddAsync(isCover ? ImageService.Kind.Cover : ImageService.Kind.Post, file, cancellationToken));
            }
            catch (ImageUploadException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
            }
        }

        if (added.Count == 0 && ModelState.IsValid)
        {
            ModelState.AddModelError(string.Empty, "Chưa chọn ảnh nào.");
        }

        if (added.Count > 0)
        {
            string? replaced = null;

            await UpdatePostAsync(slug, post =>
            {
                if (isCover)
                {
                    replaced = post.Cover;
                    post.Cover = added[0];
                }
                else
                {
                    post.BodyImages.AddRange(added);
                }
            }, cancellationToken);

            _images.Delete(replaced);

            Message = isCover
                ? "Đã thay ảnh bìa. Nhớ viết mô tả ảnh bìa rồi bấm Lưu."
                : $"Đã tải {added.Count} ảnh. Bấm \"Chèn\" để đưa ảnh vào chỗ con trỏ trong bài.";
        }

        if (!ModelState.IsValid)
        {
            await LoadAsync(slug, cancellationToken);
            FillInput(Post!);
            return Page();
        }

        return RedirectToPage(new { slug });
    }

    public async Task<IActionResult> OnPostXoaAnhAsync(string slug, string url, CancellationToken cancellationToken)
    {
        if (!await LoadAsync(slug, cancellationToken))
        {
            return NotFound();
        }

        ModelState.Clear();

        // Ảnh còn nằm trong thân bài mà xoá file thì bài hiện ảnh vỡ.
        if (Post!.Body.Contains(url, StringComparison.Ordinal))
        {
            ModelState.AddModelError(string.Empty,
                "Ảnh này đang được dùng trong thân bài. Xoá dòng ![...](/" + url + ") khỏi bài, lưu, rồi mới xoá ảnh.");
            FillInput(Post);
            return Page();
        }

        var removed = false;

        await UpdatePostAsync(slug, post =>
        {
            if (post.Cover == url)
            {
                post.Cover = string.Empty;
                post.CoverAlt = string.Empty;
                removed = true;
            }

            removed |= post.BodyImages.Remove(url);
        }, cancellationToken);

        if (removed)
        {
            _images.Delete(url);
            Message = "Đã xoá ảnh.";
        }

        return RedirectToPage(new { slug });
    }

    private Task<bool> UpdatePostAsync(string slug, Action<Post> mutate, CancellationToken cancellationToken) =>
        _content.Posts.UpdateAsync(list =>
        {
            var post = list.FirstOrDefault(item => item.Slug == slug);

            if (post is null)
            {
                return false;
            }

            mutate(post);
            post.UpdatedAt = SiteTime.Now;
            return true;
        }, cancellationToken);

    private async Task<bool> LoadAsync(string slug, CancellationToken cancellationToken)
    {
        var posts = await _content.Posts.ReadAsync(cancellationToken);
        Post = posts.FirstOrDefault(item => item.Slug == slug);
        KnownTags = posts.SelectMany(post => post.Tags).Distinct().Order().ToList();

        return Post is not null;
    }

    private async Task<bool> LoadTagsAsync(CancellationToken cancellationToken)
    {
        KnownTags = (await _content.Posts.ReadAsync(cancellationToken)).SelectMany(post => post.Tags).Distinct().Order().ToList();
        return true;
    }

    private void FillInput(Post post)
    {
        Input = new PostInput
        {
            Title = post.Title,
            Slug = post.Slug,
            Summary = post.Summary,
            Body = post.Body,
            Tags = string.Join(", ", post.Tags),
            CoverAlt = post.CoverAlt,
            Date = (post.PublishedAt ?? SiteTime.Now).ToOffset(SiteTime.VietnamOffset).ToString("yyyy-MM-dd"),
            Published = post.Published
        };
    }
}
