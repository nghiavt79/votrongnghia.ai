# Deploy votrongnghia.vn lên IIS

Cùng khuôn với Dokma và kimthyland: Windows Server + IIS, repo có sẵn `web.config`. Máy chủ khác
(Linux, hosting dùng chung) thì báo lại để sửa hướng dẫn.

Nguyên tắc: **bản publish không bao giờ mang dữ liệu sống.** Bài viết, khóa học, lịch lớp, thông
tin trang, đơn đăng ký, tài khoản quản trị, khoá đăng nhập đều sinh ra và nằm lại trên máy chủ
(`VoTrongNghia.csproj` loại chúng khỏi publish). Nhờ vậy deploy lần nào cũng chỉ việc chép đè.

## Lần đầu

### 1. Máy chủ

- Cài **ASP.NET Core 8 Hosting Bundle** (bản Runtime 8.0.x mới nhất, mục "Hosting Bundle"), rồi
  chạy `iisreset`.
- IIS có sẵn module **URL Rewrite** để chuyển `www.` và tên miền phụ (`votrongnghia.ai`) về tên miền chính (mục 5).
- Bật **Static Content Compression** của IIS: `ai-templates.json` hơn 1MB, nén là còn khoảng một phần năm.
- DNS: bản ghi A của `votrongnghia.vn` (và `www`) trỏ về IP máy chủ.

### 2. Bản publish (ở máy dev)

```powershell
dotnet publish VoTrongNghia.csproj -c Release -o publish
```

Thư mục `publish\` có `VoTrongNghia.dll`, `web.config` (SDK tự chèn khối `<aspNetCore>`),
`wwwroot\` và `Data\seed\`. Không có `Data\*.json`, `Data\keys\`, `wwwroot\uploads\` — đúng như mong muốn.

Chép cả thư mục lên máy chủ, ví dụ `D:\sites\votrongnghia`.

### 3. Site và app pool trong IIS

- **App pool** tên `votrongnghia`: .NET CLR version = **No Managed Code**, Managed pipeline =
  Integrated, Identity = ApplicationPoolIdentity.
- Advanced Settings của app pool: **Start Mode = AlwaysRunning**, **Idle Time-out = 0** — app ngủ
  thì người đầu tiên vào trang phải chờ vài giây.
- **Site** tên `votrongnghia`, physical path `D:\sites\votrongnghia`, app pool `votrongnghia`, binding:
  - `https` cổng 443, host `votrongnghia.vn`, chứng chỉ SSL (Let's Encrypt qua **win-acme**, tự gia hạn);
  - `http` cổng 80, host `votrongnghia.vn` — app tự chuyển sang https (mã 308).

Không cần đặt `ASPNETCORE_ENVIRONMENT`: không đặt thì là **Production** — đúng cái cần (bật
HTTPS, HSTS). Đừng đặt `Development` trên máy chủ.

### 4. Quyền ghi

App pool phải **ghi được** hai thư mục — `Data` (nội dung, đơn đăng ký, bản sao lưu, khoá) và
`wwwroot\uploads` (ảnh tải lên qua /cms):

```powershell
mkdir "D:\sites\votrongnghia\wwwroot\uploads"
icacls "D:\sites\votrongnghia\Data" /grant "IIS AppPool\votrongnghia:(OI)(CI)M"
icacls "D:\sites\votrongnghia\wwwroot\uploads" /grant "IIS AppPool\votrongnghia:(OI)(CI)M"
```

Thiếu quyền thì `/cms/tong-quan` hiện cảnh báo đỏ, và người học bấm gửi đơn sẽ gặp lỗi.

### 5. Các tên miền phụ về tên miền chính (nên làm)

Canonical của site luôn là `https://votrongnghia.vn` (`Site:BaseUrl` trong `appsettings.json`). Mọi
tên miền khác trỏ vào site phải **chuyển hướng 301** về đó, không thì Google thấy nhiều bản sao:

- `www.votrongnghia.vn`
- `votrongnghia.ai` và `www.votrongnghia.ai`, nếu mua thêm để giữ tên.

Cách làm trên IIS: thêm các tên miền phụ vào binding của site (cả http và https; chứng chỉ win-acme
cấp chung được cho nhiều tên), rồi thêm một quy tắc URL Rewrite ở cấp site:

```xml
<rewrite>
  <rules>
    <rule name="Ve ten mien chinh" stopProcessing="true">
      <match url="(.*)" />
      <conditions>
        <add input="{HTTP_HOST}" pattern="^votrongnghia\.vn$" negate="true" />
      </conditions>
      <action type="Redirect" url="https://votrongnghia.vn/{R:1}" redirectType="Permanent" />
    </rule>
  </rules>
</rewrite>
```

Khối này đặt trong `<system.webServer>` của `web.config` **trên máy chủ** (IIS Manager → URL Rewrite
ghi vào đó). Không đưa vào `web.config` trong repo: chạy ở máy dev (localhost) sẽ bị chuyển hướng đi
mất. Hoặc đơn giản hơn: cấu hình chuyển hướng tên miền ngay ở nhà cung cấp tên miền `.ai`, nếu họ có.

Sau khi đổi tên miền chính trong `appsettings.json`, chạy lại `scripts/kiem-tra-site.ps1`: canonical và
sitemap phải ra đúng `https://votrongnghia.vn`.

### 6. Chạy lần đầu

1. Mở `https://votrongnghia.vn` — lần khởi động đầu, site tự chép `Data\seed\` sang dữ liệu sống.
2. Mở `https://votrongnghia.vn/cms` — hiện form **tạo tài khoản quản trị**. Tạo ngay, trước khi
   báo địa chỉ cho ai: chưa đặt thì ai mở trang này trước người đó cầm chìa khoá.
3. `/cms/tong-quan` không có cảnh báo quyền ghi; làm các dòng "Việc cần làm" (email liên hệ, ảnh
   đại diện, link Facebook…).
4. Thiết lập Gmail ở `/cms/email` và bấm **Gửi thư thử**. Báo lỗi "không kết nối được
   smtp.gmail.com:587" là máy chủ đang chặn cổng 587 đi ra ngoài — mở trong tường lửa / hỏi bên cho
   thuê máy chủ (nhiều nhà cung cấp chặn sẵn cổng thư để chống spam).
5. Chạy kiểm tra (mục dưới).

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
powershell -ExecutionPolicy Bypass -File scripts\kiem-tra-site.ps1 -BaseUrl http://localhost:5090 -Canonical https://votrongnghia.vn
```

## Sao lưu

- `D:\sites\votrongnghia\Data\` (trừ `keys\`) và `wwwroot\uploads\` — sao lưu hằng ngày bằng lịch
  của máy chủ. Bản sao lưu tự động trong `Data\backups\` chỉ có JSON, không có ảnh.
  `registrations.json` có họ tên, email, số điện thoại người học: không gửi qua kênh công khai.
- `Data\keys\` **không** chép sang máy khác: khoá được mã hoá theo máy, sang máy mới thì vô dụng.
  Máy mới tự tạo khoá mới, chỉ phải đăng nhập lại — và **nhập lại mật khẩu ứng dụng Gmail** ở
  `/cms/email` (mật khẩu trong `email.json` mã hoá bằng khoá cũ, máy mới không đọc được).
- Nút **Tải bản sao lưu nội dung** ở `/cms/tong-quan` lấy nội dung (không có đơn đăng ký) để chép
  về `Data\seed\` trong repo khi muốn seed phản ánh dữ liệu thật.
