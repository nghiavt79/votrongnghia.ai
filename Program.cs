using System.Text;
using System.Threading.RateLimiting;
using VoTrongNghia.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.ResponseCompression;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages(options =>
{
    // Cả thư mục Admin phải đăng nhập, trừ chính trang đăng nhập. Khai ở đây chứ
    // không rắc [Authorize] lên từng trang: quên một thuộc tính trên một trang mới
    // là trang đó mở toang, còn quên ở đây thì không trang nào vào được.
    options.Conventions.AuthorizeFolder("/Admin");
    options.Conventions.AllowAnonymousToPage("/Admin/DangNhap");
})
.AddMvcOptions(options =>
{
    // Xem ChuoiRongKhongThanhNull: không có dòng này thì xoá trắng một ô không
    // bắt buộc rồi bấm Lưu là trang lỗi 500.
    options.ModelMetadataDetailsProviders.Add(new ChuoiRongKhongThanhNull());
});

builder.Services.AddSingleton<ContentPaths>();
builder.Services.AddSingleton<SiteContent>();
builder.Services.AddSingleton<AdminAccountStore>();
builder.Services.AddSingleton<RegistrationStore>();
builder.Services.AddSingleton<ImageService>();
builder.Services.AddSingleton<EmailSender>();
builder.Services.AddSingleton<EmailQueue>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<EmailQueue>());

// Khoá ký cookie đăng nhập và mã chống giả form lưu ra Data/keys. Không lưu thì trên IIS
// (app pool không nạp hồ sơ người dùng) khoá chỉ sống trong bộ nhớ: mỗi lần app pool khởi
// động lại — tự động khoảng 29 giờ một lần, hoặc mỗi lần deploy — người quản trị bị đăng
// xuất và form đang mở dở (kể cả form đăng ký của người học) báo lỗi 400.
// Trên Windows khoá được mã hoá bằng DPAPI của máy: chép thư mục sang máy khác cũng vô dụng.
var dataProtection = builder.Services.AddDataProtection()
    .SetApplicationName("VoTrongNghia")
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "Data", "keys")));

if (OperatingSystem.IsWindows())
{
    dataProtection.ProtectKeysWithDpapi(protectToLocalMachine: true);
}

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/cms";
        options.LogoutPath = "/cms/dang-xuat";
        options.AccessDeniedPath = "/cms";
        options.Cookie.Name = "vn.admin";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        // Máy dev chạy http://localhost nên Always sẽ chặn luôn cookie. Trên máy
        // chủ thì bắt buộc Always: cookie quản trị không được đi qua http.
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;

        // Cookie mang theo dấu bảo mật của tài khoản. Đổi mật khẩu là đổi dấu, nên
        // mọi phiên đang mở ở máy khác bị từ chối ngay ở request kế tiếp.
        options.Events.OnValidatePrincipal = async context =>
        {
            var accounts = context.HttpContext.RequestServices.GetRequiredService<AdminAccountStore>();
            var account = await accounts.GetAsync(context.HttpContext.RequestAborted);
            var stamp = context.Principal?.FindFirst(AdminAccountStore.SecurityStampClaim)?.Value;

            if (account is null || string.IsNullOrEmpty(account.SecurityStamp) ||
                !string.Equals(stamp, account.SecurityStamp, StringComparison.Ordinal))
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(options =>
{
    // Trang đăng nhập là chỗ duy nhất của site nhận mật khẩu. Đếm theo IP chứ không
    // đếm chung: một hàng đếm chung nghĩa là kẻ dò khoá được luôn cửa của chủ site.
    // Chỉ đếm lần gửi mật khẩu; đếm cả lượt mở trang thì bấm F5 vài lần là tự khoá mình.
    options.AddPolicy("dang-nhap", context =>
        !HttpMethods.IsPost(context.Request.Method)
            ? RateLimitPartition.GetNoLimiter("mo-trang")
            : RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "khong-ro",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(5), QueueLimit = 0 }));

    // Form đăng ký học: chỗ duy nhất người lạ ghi được vào đĩa. Không có mật khẩu để dò,
    // nhưng một máy gửi liên tục là sổ đơn đầy rác. Ngưỡng đủ rộng cho người thật sửa
    // lỗi rồi gửi lại vài lần.
    options.AddPolicy("dang-ky", context =>
        !HttpMethods.IsPost(context.Request.Method)
            ? RateLimitPartition.GetNoLimiter("mo-trang")
            : RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "khong-ro",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 8, Window = TimeSpan.FromMinutes(10), QueueLimit = 0 }));

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "text/plain; charset=utf-8";
        await context.HttpContext.Response.WriteAsync(
            "Gửi quá nhiều lần trong thời gian ngắn. Chờ vài phút rồi thử lại.", cancellationToken);
    };
});

builder.Services.AddHttpsRedirection(options =>
{
    options.RedirectStatusCode = StatusCodes.Status308PermanentRedirect;
});

builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = true;
});

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    // ai-templates.json hơn 1MB: nén là còn khoảng một phần năm.
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(["application/json"]);
});

var app = builder.Build();

app.Services.GetRequiredService<ContentPaths>().EnsureInitialized();

// `dotnet VoTrongNghia.dll --dat-mat-khau` đặt lại mật khẩu quản trị rồi thoát — lối
// thoát khi quên mật khẩu. Hỏi qua bàn phím chứ không nhận làm tham số dòng lệnh, vì
// tham số nằm lại trong lịch sử shell.
if (args.Contains("--dat-mat-khau"))
{
    await SetAdminPasswordAsync(app.Services.GetRequiredService<AdminAccountStore>());
    return;
}

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseResponseCompression();

// Lỗi 404 không có thân (trang không tồn tại, bài nháp) thì dựng lại bằng
// Pages/KhongTimThay.cshtml: khách có đường về trang chủ thay vì trang trắng. ReExecute
// chứ không Redirect: URL và mã 404 giữ nguyên, Google không coi là trang thật.
app.UseStatusCodePagesWithReExecute("/khong-tim-thay");

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context =>
    {
        var headers = context.Context.Response.Headers;
        var file = context.File.Name;

        // JS, CSS và dữ liệu không mang dấu phiên bản trong tên, nên bắt hỏi lại mỗi lần —
        // sửa xong deploy là khách thấy ngay (bản tĩnh cũ từng dính: trình duyệt giữ
        // common.js cũ, trang mới gọi hàm không có). Ảnh thì giữ 30 ngày: ảnh tải qua /cms
        // luôn mang tên mới (ImageService), thay ảnh là đổi tên.
        headers.CacheControl = file.EndsWith(".js", StringComparison.OrdinalIgnoreCase) ||
                               file.EndsWith(".css", StringComparison.OrdinalIgnoreCase) ||
                               file.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
            ? "no-cache"
            : "public,max-age=2592000";

        // Ảnh tải lên không bao giờ được chạy như trang. web.config khai cho IIS; khai thêm
        // ở đây cho lúc chạy Kestrel trần.
        headers.XContentTypeOptions = "nosniff";
    }
});

app.UseRouting();

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

// robots.txt: cho index hết, trừ trang quản trị. Khai luôn sitemap.
app.MapGet("/robots.txt", (IConfiguration configuration) =>
    Results.Text(
        $"User-agent: *\nDisallow: /cms\nSitemap: {(configuration["Site:BaseUrl"] ?? "").TrimEnd('/')}/sitemap.xml\n",
        "text/plain"));

// Sitemap: chỉ trang đang hiện với khách (bài, khóa học đã đăng). lastmod lấy từ mốc sửa
// thật của từng bản ghi; bản ghi không có mốc thì bỏ trống — thiếu lastmod tốt hơn lastmod bịa.
app.MapGet("/sitemap.xml", async (SiteContent content, IConfiguration configuration, CancellationToken cancellationToken) =>
{
    var baseUrl = (configuration["Site:BaseUrl"] ?? string.Empty).TrimEnd('/');
    var posts = await content.PublishedPostsAsync(cancellationToken);
    var courses = await content.PublishedCoursesAsync(cancellationToken);

    static string? Day(DateTimeOffset? value) => value?.ToString("yyyy-MM-dd");

    var urls = new List<(string Loc, string? LastMod)>
    {
        ($"{baseUrl}/", null),
        ($"{baseUrl}/dang-ky", null),
        ($"{baseUrl}/ai-templates", null)
    };

    if (posts.Count > 0)
    {
        urls.Add(($"{baseUrl}/bai-viet", Day(posts.Max(post => post.UpdatedAt))));
        urls.AddRange(posts.Select(post => ($"{baseUrl}{post.PublicUrl}", Day(post.UpdatedAt ?? post.PublishedAt))));
    }

    if (courses.Count > 0)
    {
        urls.Add(($"{baseUrl}/khoa-hoc", Day(courses.Max(course => course.UpdatedAt))));
    }

    foreach (var course in courses)
    {
        urls.Add(($"{baseUrl}{course.PublicUrl}", Day(course.UpdatedAt)));
        urls.AddRange(course.Lessons.Select(lesson => ($"{baseUrl}{course.LessonUrl(lesson)}", Day(course.UpdatedAt))));
    }

    var xml = new StringBuilder();
    xml.AppendLine("""<?xml version="1.0" encoding="UTF-8"?>""");
    xml.AppendLine("""<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">""");

    foreach (var (loc, lastMod) in urls)
    {
        xml.Append("  <url><loc>").Append(System.Security.SecurityElement.Escape(loc)).Append("</loc>");

        if (lastMod is not null)
        {
            xml.Append("<lastmod>").Append(lastMod).Append("</lastmod>");
        }

        xml.AppendLine("</url>");
    }

    xml.AppendLine("</urlset>");

    return Results.Content(xml.ToString(), "application/xml", Encoding.UTF8);
});

app.Run();

// Đặt mật khẩu quản trị từ dòng lệnh trên máy chủ.
static async Task SetAdminPasswordAsync(AdminAccountStore store)
{
    Console.OutputEncoding = Encoding.UTF8;
    Console.WriteLine("Đặt tài khoản quản trị votrongnghia.ai");
    Console.Write("Tên đăng nhập: ");
    var userName = Console.ReadLine()?.Trim();

    if (string.IsNullOrWhiteSpace(userName))
    {
        Console.WriteLine("Bỏ trống tên đăng nhập. Không đổi gì cả.");
        return;
    }

    var password = ReadSecret("Mật khẩu: ");
    var confirmation = ReadSecret("Nhập lại: ");

    if (!string.Equals(password, confirmation, StringComparison.Ordinal))
    {
        Console.WriteLine("Hai lần nhập không giống nhau. Không đổi gì cả.");
        return;
    }

    if (password.Length < AdminAccountStore.MinimumPasswordLength)
    {
        Console.WriteLine(
            $"Mật khẩu phải từ {AdminAccountStore.MinimumPasswordLength} ký tự trở lên. Không đổi gì cả.");
        return;
    }

    await store.SetPasswordAsync(userName, password);
    Console.WriteLine($"Xong. Đăng nhập bằng tài khoản \"{userName}\" tại /cms.");
}

// Đọc mật khẩu mà không hiện ra màn hình.
static string ReadSecret(string prompt)
{
    Console.Write(prompt);

    if (Console.IsInputRedirected)
    {
        return Console.ReadLine() ?? string.Empty;
    }

    var secret = new StringBuilder();

    while (true)
    {
        var key = Console.ReadKey(intercept: true);

        if (key.Key == ConsoleKey.Enter)
        {
            Console.WriteLine();
            return secret.ToString();
        }

        if (key.Key == ConsoleKey.Backspace)
        {
            if (secret.Length > 0)
            {
                secret.Length--;
            }

            continue;
        }

        if (!char.IsControl(key.KeyChar))
        {
            secret.Append(key.KeyChar);
        }
    }
}
