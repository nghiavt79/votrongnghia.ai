# Deploy votrongnghia.ai lên IIS

Cùng khuôn với Dokma và kimthyland: Windows Server + IIS, repo có sẵn `web.config`. Máy chủ khác
(Linux, hosting dùng chung) thì báo lại để sửa hướng dẫn.

Nguyên tắc: **bản publish không bao giờ mang dữ liệu sống.** Bài viết, khóa học, lịch lớp, thông
tin trang, đơn đăng ký, tài khoản quản trị, khoá đăng nhập đều sinh ra và nằm lại trên máy chủ
(`VoTrongNghia.csproj` loại chúng khỏi publish). Nhờ vậy deploy lần nào cũng chỉ việc chép đè.

## Lần đầu

### 1. Máy chủ

- Cài **ASP.NET Core 8 Hosting Bundle** (bản Runtime 8.0.x mới nhất, mục "Hosting Bundle"), rồi
  chạy `iisreset`.
- IIS có sẵn module **URL Rewrite** nếu muốn chuyển `www.` về tên miền gốc (mục 5).
- Bật **Static Content Compression** của IIS: `ai-templates.json` hơn 1MB, nén là còn khoảng một phần năm.
- DNS: bản ghi A của `votrongnghia.ai` (và `www`) trỏ về IP máy chủ.

### 2. Bản publish (ở máy dev)

```powershell
dotnet publish VoTrongNghia.csproj -c Release -o publish
```

Thư mục `publish\` có `VoTrongNghia.dll`, `web.config` (SDK tự chèn khối `<aspNetCore>`),
`wwwroot\` và `Data\seed\`. Không có `Data\*.json`, `Data\keys\` — đúng như mong muốn.

Chép cả thư mục lên máy chủ, ví dụ `D:\sites\votrongnghia`.

### 3. Site và app pool trong IIS

- **App pool** tên `votrongnghia`: .NET CLR version = **No Managed Code**, Managed pipeline =
  Integrated, Identity = ApplicationPoolIdentity.
- Advanced Settings của app pool: **Start Mode = AlwaysRunning**, **Idle Time-out = 0** — app ngủ
  thì người đầu tiên vào trang phải chờ vài giây.
- **Site** tên `votrongnghia`, physical path `D:\sites\votrongnghia`, app pool `votrongnghia`, binding:
  - `https` cổng 443, host `votrongnghia.ai`, chứng chỉ SSL (Let's Encrypt qua **win-acme**, tự gia hạn);
  - `http` cổng 80, host `votrongnghia.ai` — app tự chuyển sang https (mã 308).

Không cần đặt `ASPNETCORE_ENVIRONMENT`: không đặt thì là **Production** — đúng cái cần (bật
HTTPS, HSTS). Đừng đặt `Development` trên máy chủ.

### 4. Quyền ghi

App pool phải **ghi được** thư mục `Data` (nội dung, đơn đăng ký, bản sao lưu, khoá đều nằm ở đây):

```powershell
icacls "D:\sites\votrongnghia\Data" /grant "IIS AppPool\votrongnghia:(OI)(CI)M"
```

Thiếu quyền thì `/cms/tong-quan` hiện cảnh báo đỏ, và người học bấm gửi đơn sẽ gặp lỗi.

### 5. `www.` về tên miền gốc (nên làm)

Canonical của site luôn là `https://votrongnghia.ai` (`Site:BaseUrl` trong `appsettings.json`). Để
`www.votrongnghia.ai` không thành bản sao, thêm quy tắc URL Rewrite ở cấp site (IIS Manager → URL
Rewrite → Add Rule → Canonical domain name), hoặc chuyển hướng ở nhà cung cấp DNS.

### 6. Chạy lần đầu

1. Mở `https://votrongnghia.ai` — lần khởi động đầu, site tự chép `Data\seed\` sang dữ liệu sống.
2. Mở `https://votrongnghia.ai/cms` — hiện form **tạo tài khoản quản trị**. Tạo ngay, trước khi
   báo địa chỉ cho ai: chưa đặt thì ai mở trang này trước người đó cầm chìa khoá.
3. `/cms/tong-quan` không có cảnh báo quyền ghi; làm các dòng "Việc cần làm" (email liên hệ, ảnh
   đại diện, link Facebook…).
4. Chạy kiểm tra (mục dưới).

Quên mật khẩu quản trị: trên máy chủ chạy `dotnet VoTrongNghia.dll --dat-mat-khau` trong thư mục site.

## Các lần deploy sau

1. Ở máy dev: `dotnet publish VoTrongNghia.csproj -c Release -o publish`.
2. Trên máy chủ, thả file `app_offline.htm` (trang "Đang bảo trì" một dòng) vào thư mục site —
   IIS dừng app, nhả khoá file DLL.
3. Chép đè nội dung `publish\` vào thư mục site.
4. Xoá `app_offline.htm`.
5. Chạy kiểm tra.

## Kiểm tra sau deploy

```powershell
powershell -ExecutionPolicy Bypass -File scripts\kiem-tra-site.ps1
```

Script chỉ đọc (GET): trang chủ, canonical, header bảo mật (CSP, HSTS), http → https, robots.txt,
sitemap, một bài viết (JSON-LD), một bài học, `/dang-ky`, `/ai-templates` và nén, `/cms`, trang 404.
`[HONG]` là phải sửa; `[XEM]` thường là việc còn thiếu trong `/cms` (email liên hệ…).

Thử ở máy trước khi deploy:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\kiem-tra-site.ps1 -BaseUrl http://localhost:5090 -Canonical https://votrongnghia.ai
```

## Sao lưu

- `D:\sites\votrongnghia\Data\` (trừ `keys\`) — sao lưu hằng ngày bằng lịch của máy chủ.
  `registrations.json` có họ tên, email, số điện thoại người học: không gửi qua kênh công khai.
- `Data\keys\` **không** chép sang máy khác: khoá được mã hoá theo máy, sang máy mới thì vô dụng.
  Máy mới tự tạo khoá mới, chỉ phải đăng nhập lại.
- Nút **Tải bản sao lưu nội dung** ở `/cms/tong-quan` lấy nội dung (không có đơn đăng ký) để chép
  về `Data\seed\` trong repo khi muốn seed phản ánh dữ liệu thật.
