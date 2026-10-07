<#
  Kiểm tra site sau mỗi lần deploy. Chép từ Dokma, đổi theo các trang của votrongnghia.ai. Chạy:

      powershell -ExecutionPolicy Bypass -File scripts\kiem-tra-site.ps1
      powershell -ExecutionPolicy Bypass -File scripts\kiem-tra-site.ps1 -BaseUrl http://localhost:5090 -Canonical https://votrongnghia.ai

  -BaseUrl   địa chỉ để gọi (mặc định site thật).
  -Canonical địa chỉ mà canonical / sitemap phải trỏ tới, tức Site:BaseUrl trong appsettings.json.
             Mặc định bằng -BaseUrl. Thử ở máy thì đặt https://votrongnghia.ai.

  Chỉ đọc (GET), không ghi gì lên site. Thoát với mã 1 nếu có mục hỏng.
  File lưu UTF-8 có BOM: PowerShell 5.1 đọc file không BOM theo bảng mã ANSI, chú thích thành chữ lỗi.
#>
param(
    [string]$BaseUrl = "https://votrongnghia.ai",
    [string]$Canonical = ""
)

$ErrorActionPreference = "Stop"
$BaseUrl = $BaseUrl.TrimEnd('/')
if (-not $Canonical) { $Canonical = $BaseUrl }
$Canonical = $Canonical.TrimEnd('/')
$isHttps = $BaseUrl.StartsWith("https://")
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$script:failed = 0
$script:warned = 0

function Ok($text)   { Write-Host "  [OK]   $text" -ForegroundColor Green }
function Bad($text)  { Write-Host "  [HONG] $text" -ForegroundColor Red; $script:failed++ }
function Warn($text) { Write-Host "  [XEM]  $text" -ForegroundColor Yellow; $script:warned++ }
function Check($condition, $okText, $badText) { if ($condition) { Ok $okText } else { Bad $badText } }

# Gọi một URL, không tự đi theo chuyển hướng, trả về mã, header, nội dung.
# Dùng HttpWebRequest thay Invoke-WebRequest: bản PowerShell 5.1 ném lỗi với mọi mã 3xx / 4xx.
function Get-Url([string]$url, [string]$acceptEncoding = "") {
    $request = [Net.HttpWebRequest]::Create($url)
    $request.AllowAutoRedirect = $false
    $request.UserAgent = "votrongnghia-kiem-tra/1.0"
    $request.Timeout = 30000
    if ($acceptEncoding) { $request.Headers.Add("Accept-Encoding", $acceptEncoding) }
    else { $request.AutomaticDecompression = [Net.DecompressionMethods]::GZip -bor [Net.DecompressionMethods]::Deflate }

    try { $response = $request.GetResponse() }
    catch [Net.WebException] {
        if (-not $_.Exception.Response) { return @{ Status = 0; Error = $_.Exception.Message; Headers = @{}; Body = "" } }
        $response = $_.Exception.Response
    }

    $reader = New-Object IO.StreamReader($response.GetResponseStream(), [Text.Encoding]::UTF8)
    $body = if ($acceptEncoding) { "" } else { $reader.ReadToEnd() }
    $result = @{ Status = [int]$response.StatusCode; Headers = $response.Headers; Body = $body; Length = $response.ContentLength }
    $reader.Close(); $response.Close()
    return $result
}

function Canonical-Of([string]$html) {
    $match = [regex]::Match($html, '<link rel="canonical" href="([^"]+)"')
    if ($match.Success) { return $match.Groups[1].Value } else { return "" }
}

Write-Host ""
Write-Host "Kiem tra $BaseUrl (canonical phai la $Canonical)" -ForegroundColor Cyan

# ---- Trang chủ
Write-Host "`nTrang chu"
$homePage = Get-Url "$BaseUrl/"
if ($homePage.Status -eq 0) { Bad "Khong goi duoc: $($homePage.Error)"; exit 1 }
Check ($homePage.Status -eq 200) "Tra 200" "Tra $($homePage.Status)"
$homeCanonical = Canonical-Of $homePage.Body
Check ($homeCanonical -eq "$Canonical/") "Canonical = $Canonical/" "Canonical sai ($homeCanonical) - kiem tra Site:BaseUrl trong appsettings.json"
Check ($homePage.Body -match 'id="hoc-online"') "Co muc Hoc online" "Thieu muc Hoc online"
if ($homePage.Body -match 'href="mailto:') { Ok "Co email lien he" } else { Warn "Chua co email lien he (dien o /cms/thong-tin)" }

# ---- Header bảo mật (IIS gắn từ web.config)
Write-Host "`nHeader"
$csp = $homePage.Headers["Content-Security-Policy"]
if ($csp) {
    Ok "Co Content-Security-Policy"
    Check ($csp -match 'cdn\.jsdelivr\.net') "CSP mo cho marked/DOMPurify (AI Templates)" "CSP chua mo cdn.jsdelivr.net - web.config cu?"
} elseif ($BaseUrl -match 'localhost') {
    Warn "Khong co CSP (binh thuong khi chay Kestrel o may - CSP nam trong web.config cua IIS)"
} else {
    Bad "Khong co Content-Security-Policy - web.config khong duoc chep len may chu?"
}
if ($homePage.Headers["X-Content-Type-Options"] -eq "nosniff") { Ok "X-Content-Type-Options: nosniff" } else { Warn "Thieu X-Content-Type-Options" }
if ($isHttps) {
    if ($homePage.Headers["Strict-Transport-Security"]) { Ok "Co HSTS" } else { Bad "Thieu HSTS - ASPNETCORE_ENVIRONMENT dang la Development?" }
    $http = Get-Url (($BaseUrl -replace '^https://', 'http://') + "/")
    Check ($http.Status -in 301, 308 -and $http.Headers["Location"] -like "https://*") `
        "http:// chuyen huong sang https ($($http.Status))" "http:// khong chuyen sang https (tra $($http.Status))"
}

# ---- robots.txt, sitemap
Write-Host "`nrobots.txt, sitemap"
$robots = Get-Url "$BaseUrl/robots.txt"
Check ($robots.Body -match [regex]::Escape("Sitemap: $Canonical/sitemap.xml")) "robots.txt khai sitemap dung dia chi" "robots.txt khai sitemap sai: $($robots.Body -replace '\s+', ' ')"
Check ($robots.Body -match 'Disallow: /cms') "robots.txt chan /cms" "robots.txt khong chan /cms"

$sitemap = Get-Url "$BaseUrl/sitemap.xml"
$locs = [regex]::Matches($sitemap.Body, '<loc>([^<]+)</loc>') | ForEach-Object { $_.Groups[1].Value }
Check ($sitemap.Status -eq 200 -and $locs.Count -gt 0) "sitemap.xml co $($locs.Count) dia chi" "sitemap.xml loi hoac rong (tra $($sitemap.Status))"
$wrong = @($locs | Where-Object { -not $_.StartsWith("$Canonical/") })
Check ($wrong.Count -eq 0) "Moi dia chi trong sitemap bat dau bang $Canonical" "$($wrong.Count) dia chi sai goc, vi du $($wrong[0])"

# ---- Một bài viết và một bài học (lấy từ sitemap)
Write-Host "`nBai viet, bai hoc"
$postLoc = $locs | Where-Object { $_ -match '/bai-viet/[^/]+$' } | Select-Object -First 1
if ($postLoc) {
    $path = $postLoc.Substring($Canonical.Length)
    $post = Get-Url "$BaseUrl$path"
    Check ($post.Status -eq 200) "$path tra 200" "$path tra $($post.Status)"
    Check ((Canonical-Of $post.Body) -eq "$Canonical$path") "Canonical dung" "Canonical bai viet sai"
    Check ($post.Body -match '"@type":"BlogPosting"') "Co JSON-LD BlogPosting" "Thieu JSON-LD BlogPosting"
} else { Warn "Sitemap chua co bai viet nao da dang" }
$lessonLoc = $locs | Where-Object { $_ -match '/khoa-hoc/[^/]+/[^/]+$' } | Select-Object -First 1
if ($lessonLoc) {
    $path = $lessonLoc.Substring($Canonical.Length)
    $lesson = Get-Url "$BaseUrl$path"
    Check ($lesson.Status -eq 200) "$path tra 200" "$path tra $($lesson.Status)"
} else { Warn "Sitemap chua co bai hoc nao" }

# ---- Đăng ký, AI Templates, nén
Write-Host "`nDang ky, AI Templates"
$signup = Get-Url "$BaseUrl/dang-ky"
Check ($signup.Status -eq 200 -and $signup.Body -match '__RequestVerificationToken') "/dang-ky co form (kem ma chong gia form)" "/dang-ky loi (tra $($signup.Status))"
$templates = Get-Url "$BaseUrl/ai-templates"
Check ($templates.Status -eq 200) "/ai-templates tra 200" "/ai-templates tra $($templates.Status)"
$compressed = Get-Url "$BaseUrl/assets/data/ai-templates.json" "br, gzip"
$encoding = $compressed.Headers["Content-Encoding"]
if ($encoding) { Ok "ai-templates.json co nen: $encoding" } else { Warn "ai-templates.json khong nen (hon 1MB - bat IIS static compression)" }

# ---- Các đường dẫn khác
Write-Host "`nDuong dan khac"
$cms = Get-Url "$BaseUrl/cms"
Check ($cms.Status -eq 200) "/cms mo duoc" "/cms tra $($cms.Status)"
$missing = Get-Url "$BaseUrl/khong-co-trang-nay-$(Get-Random)"
Check ($missing.Status -eq 404) "Trang khong ton tai tra 404" "Trang khong ton tai tra $($missing.Status) - Google se index trang rac"
$noPost = Get-Url "$BaseUrl/bai-viet/khong-co-bai-nay-$(Get-Random)"
Check ($noPost.Status -eq 404) "Bai viet khong ton tai tra 404" "Bai viet khong ton tai tra $($noPost.Status)"

# ---- Kết quả
Write-Host ""
if ($script:failed -eq 0) {
    Write-Host "Xong: khong co muc hong, $($script:warned) muc can xem." -ForegroundColor Green
    exit 0
}
Write-Host "Xong: $($script:failed) muc hong, $($script:warned) muc can xem." -ForegroundColor Red
exit 1
