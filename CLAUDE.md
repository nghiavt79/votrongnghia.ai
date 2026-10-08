# votrongnghia.vn

Trang cá nhân chia sẻ kiến thức AI và vibe code — **miễn phí, lan tỏa**. ASP.NET Core 8 Razor
Pages, không database, dữ liệu JSON. Dựng theo đúng khuôn của `D:\2026\Dokma` (kho JSON ghi
nguyên tử, `/cms` AdminLTE 4 một tài khoản, Markdown qua Markdig, deploy IIS).

Bản đầu là site tĩnh HTML/JS — xem commit đầu tiên của repo nếu cần đối chiếu.

## Bố cục

| Đường dẫn | Nội dung |
|---|---|
| `Pages/Index.cshtml` | Trang chủ, dựng phía máy chủ từ `site.json`, `posts.json`, `courses.json`, `live.json`. Mục nào chưa có nội dung thì ẩn (chưa có kênh → không có mục Video, chưa bật ủng hộ → nút nổi là "Chia sẻ") |
| `Pages/BaiViet/` | `/bai-viet` (lọc `?chuDe=`), `/bai-viet/{slug}` (mục lục tự động, JSON-LD BlogPosting, bài liên quan) |
| `Pages/KhoaHoc/` | `/khoa-hoc`, `/khoa-hoc/{khóa}`, `/khoa-hoc/{khóa}/{bài}`. Tiến độ học lưu trên trình duyệt người học (`localStorage` `course-progress`, khoá `"khóa/bài"`), không có tài khoản học viên |
| `Pages/HocVien/`, `Services/LearnerStore.cs`, `LoginLinkService.cs`, `LearnerSession.cs`, `LearnerApi.cs` | Tài khoản học viên (GĐ2 của `docs/ke-hoach-hoc-vien.md`). Chỉ sinh khi duyệt đơn ở `/cms/dang-ky/{mã}` (hoặc thêm tay ở `/cms/hoc-vien`). Đăng nhập không mật khẩu: `/hoc-vien/dang-nhap` gửi link `/hoc-vien/vao?ma=…` (băm SHA-256 trong `Data/dang-nhap.json`, dùng một lần, 30 phút; kèm thư duyệt / tạo ở /cms thì 7 ngày). Cookie `vn.hocvien` 60 ngày, scheme riêng. Tiến độ học viên lên máy chủ qua `/api/hoc-vien/*`; `site.js` gộp tiến độ trên máy lên tài khoản lần đầu, sau đó chép tiến độ máy chủ xuống `localStorage` |
| `Pages/DangKy.cshtml` | `/dang-ky`: đăng ký lớp online miễn phí có cam kết. Form khoá sẵn, `dang-ky.js` mở khi đã học xong khóa đầu vào; máy chủ kiểm lại mọi điều kiện khác ở `Services/RegistrationStore.cs` |
| `Pages/AiTemplates.cshtml` | `/ai-templates`: trang thuần JS, dữ liệu ở `wwwroot/assets/data/ai-templates.json` (sinh bằng `scripts/update-ai-templates.py`) và `ai-templates-vi.json` (bản dịch, sửa tay). Xếp theo **nhóm việc**, không theo loại kỹ thuật: nhóm việc, mẫu chọn sẵn, lời giải thích từng loại sửa tay ở `assets/js/ai-templates-vi.js` (`TPL_GROUPS`, `TPL_STARTERS`, `TPL_TYPE_HELP`); script cập nhật báo danh mục mới chưa xếp nhóm |
| `Pages/Shared/_SiteLayout.cshtml` | Header, menu, footer, nút nổi — máy chủ dựng. `wwwroot/assets/js/site.js` chỉ thêm phần bấm được |
| `Pages/Admin/` | `/cms` (đăng nhập / tạo tài khoản lần đầu), `/cms/tong-quan`, `/cms/dang-ky`, `/cms/hoc-vien`, `/cms/lop-online`, `/cms/bai-viet`, `/cms/khoa-hoc`, `/cms/thong-tin`, `/cms/email`, `/cms/doi-mat-khau` |
| `Services/EmailSender.cs`, `EmailQueue.cs`, `EmailTemplates.cs` | Gửi email qua Gmail (SMTP 587 + mật khẩu ứng dụng). Thư xác nhận và báo đơn mới đi qua hàng đợi chạy nền; thư báo kết quả và thư tự soạn ở `/cms/dang-ky/{mã}` gửi ngay. Mỗi lần gửi ghi vào `Registration.Emails` |
| `Services/VietQr.cs` | Mã QR chuyển khoản (VietQR / NAPAS) cho mục Ủng hộ, dựng từ `site.json` qua `/ung-ho/qr.svg` và `/ung-ho/qr.png?tai=1`. Ngân hàng tra theo tên gõ ở `/cms/thong-tin` → mã BIN trong bảng `Banks`; ngân hàng chưa có trong bảng thì không có QR |
| `Pages/ChinhSachDuLieu.cshtml` | `/chinh-sach-du-lieu`: chỉ ghi đúng những gì site **đang làm**. Thời hạn giữ đơn lấy từ `RegistrationStore.RetentionMonths` (12); `/cms/dang-ky` có nút ẩn danh hoá đơn quá hạn |
| `Services/LessonStatsStore.cs`, `Pages/Admin/ThongKe.cshtml` | Thống kê ẩn danh (`POST /api/tien-do` từ `khoa-hoc.js`, mỗi bài một lần mỗi trình duyệt), đếm trong bộ nhớ, ghi `Data/thong-ke.json` 5 phút một lần, không sao lưu. `/cms/thong-ke` vẽ phễu từng khóa |
| `Data/email.json` | Cấu hình Gmail, mật khẩu ứng dụng mã hoá bằng Data Protection (khoá trong `Data/keys`). Ngoài git, ngoài bản publish, không có seed |
| `Data/seed/*.json` | Dữ liệu gốc, có trong git và trong bản publish. Máy chủ chưa có file sống thì lần khởi động đầu tự chép sang |
| `Data/site.json`, `posts.json`, `courses.json`, `live.json` | Dữ liệu sống, sửa trong `/cms`. Ngoài git, ngoài bản publish |
| `wwwroot/uploads/` | Ảnh tải lên qua `/cms` (`Services/ImageService.cs`): `trang/` ảnh đại diện, `bai-viet/` ảnh bìa và ảnh trong bài, `khoa-hoc/` ảnh trong bài học. Ngoài git, ngoài bản publish |
| `Data/registrations.json` | Đơn đăng ký: họ tên, email, số điện thoại người học. Ngoài git, ngoài bản publish, không có seed |
| `Data/hoc-vien.json`, `Data/dang-nhap.json` | Học viên (email, điện thoại, tiến độ kèm thời điểm, lịch sử thư) và link đăng nhập đang chờ. Ngoài git, ngoài bản publish, không có seed |
| `Services/` | `JsonFileStore`, `AdminAccountStore`, `ChuoiRongKhongThanhNull`, `Slugs` chép nguyên từ Dokma; `ImageService` chép từ Dokma, gọn lại một hàm cho mọi loại ảnh. `MarkdownRenderer` thêm mục lục và bỏ qua khối code khi tìm chỗ trống |
| `docs/DEPLOY.md`, `scripts/kiem-tra-site.ps1` | Deploy lên IIS; script kiểm tra site sau deploy, chạy được cả vào localhost |

## Những điều dễ vấp

1. **Ô link để trống là ẩn, không phải lỗi.** Mọi link (YouTube, Facebook, PayPal…) trống thì phần
   đó không hiện. Đừng bao giờ để link mẫu kiểu `your-profile` lọt ra site — `/cms/thong-tin` chặn.
2. **Bài đầu vào không phải khoá thật.** Tiến độ học nằm trên máy người học, ai sửa `localStorage`
   là qua. Đó là bộ lọc người chỉ tò mò; lớp lọc thật là người duyệt đọc đơn ở `/cms/dang-ky`.
   Tài khoản học viên không đổi điều này: người gửi đơn lần đầu chưa có tài khoản (phương án A).
3. **Email là phần thêm, không được làm hỏng việc chính.** Gmail lỗi / chậm thì đơn vẫn lưu và
   người học vẫn thấy trang cảm ơn ngay (hàng đợi nền); lỗi ghi vào lịch sử email của đơn. Hàng đợi
   nằm trong bộ nhớ — app pool khởi động lại đúng lúc còn thư là mất thư đó. Mẫu thư
   (`EmailTemplates`) không được có chỗ trống `[…]`: thư báo kết quả đi tự động, không ai đọc lại.
4. **`LiveSession.MeetingLink` không bao giờ ra trang công khai** — chỉ vào thư báo duyệt và trang
   `/hoc-vien` (cho đúng học viên có đơn **đã duyệt** vào buổi đó). Thêm chỗ hiện lịch học mới thì
   kiểm lại. Trường `Platform` mới là chữ hiện công khai.
   Thử gửi email trên máy dev: sửa tay `host`/`port`/`useSsl` trong `Data/email.json` trỏ sang một
   máy chủ SMTP giả (form `/cms/email` cố ý không có các ô này). `SmtpClient` đăng nhập hỏng vẫn
   gửi tiếp không đăng nhập — máy chủ giả phải đòi đăng nhập trước `MAIL FROM` như Gmail mới thử đúng.
5. **File dữ liệu sống mới thì phải loại ở 2 chỗ**: `.gitignore` và `VoTrongNghia.csproj`
   (`CopyToPublishDirectory="Never"`). Chỉ `.gitignore` là chưa đủ: `dotnet publish` đọc đĩa,
   bản publish từ máy dev sẽ đè dữ liệu thật trên máy chủ. `Data/keys/` là khoá ký cookie — không
   bao giờ chép đi đâu.
6. **`[BindProperty] Slug` trùng tên tham số route `slug`** (bẫy Dokma đã dính): tham số handler
   phải khai `[FromRoute]` — xem `Pages/Admin/KhoaHoc/Sua.cshtml.cs`.
7. **Trong .cshtml đừng đặt biến tên `page`**: `@page.Title` bị hiểu là chỉ thị `@page`.
8. **Đổi mã buổi học là mồ côi đơn.** `LiveSession.Id` cố định lúc tạo; đơn trỏ tới mã này và giữ
   thêm `SessionLabel` để đọc được kể cả khi buổi bị xoá.
9. **Ở trang công khai `User` vẫn là quản trị, không phải học viên.** Scheme mặc định là cookie quản
   trị; trang bài viết / khóa học dùng `User.Identity.IsAuthenticated` để cho xem bản nháp. Đừng gán
   học viên vào `HttpContext.User` — lấy qua `LearnerSession.CurrentAsync`. Chỉ trang trong
   `Pages/HocVien` (chính sách `HocVien`) có `User` là học viên. Vì vậy API tiến độ dùng mã chống giả
   riêng (`LearnerSession.RequestToken`), không dùng antiforgery của ASP.NET (gắn với `User`).
10. **CSP nằm trong `web.config`** (chỉ IIS gắn). Thêm nguồn ngoài (CDN, video, ảnh) thì mở thêm ở
   đó, rồi chạy `scripts/kiem-tra-site.ps1` vào site thật.
11. **Ảnh tải lên luôn ra JPG tên mới** (`ImageService`): giải mã thật, xoay theo EXIF rồi xoá EXIF
    (GPS), thu nhỏ; ảnh bìa cắt 1200×630, ảnh đại diện cắt vuông 512. Tên mới mỗi lần nên ảnh được
    cache 30 ngày. Xoá ảnh còn nằm trong thân bài thì bị chặn — gỡ dòng `![…](…)` trước. Trang có tải
    ảnh khai `[RequestSizeLimit(64MB)]`, khớp `maxAllowedContentLength` trong `web.config`.
12. **`/chinh-sach-du-lieu` phải khớp với code.** Trang hứa: không IP, không cookie theo dõi, không
    Google Analytics, giữ đơn và tài khoản học viên 12 tháng (tính từ lần hoạt động cuối), học viên tự xoá được. Thêm công cụ đo lường, thu thêm dữ liệu (tài khoản học viên…)
    thì sửa trang này **trước**, và đổi ngày cập nhật đầu trang.
13. **Seed đổi không tự vào máy đang chạy.** Máy chủ chỉ chép seed khi chưa có file sống; sửa
    `Data/seed/` xong thì dữ liệu đang chạy vẫn là bản cũ — sửa qua `/cms`, hoặc chép tay khi chắc
    file sống chưa ai sửa.
14. **Máy chủ và repo trôi khỏi nhau** khi sửa trong `/cms`. Tải bản sao lưu ở `/cms/tong-quan`
    rồi chép vào `Data/seed/` khi muốn seed mới phản ánh nội dung thật.

## Repo

- GitHub: https://github.com/nghiavt79/votrongnghia.ai (private), nhánh `main`.
- Tên miền chính là **votrongnghia.vn** (chốt 08/10/2026, `Site:BaseUrl`). Thư mục máy dev và tên repo
  vẫn là `votrongnghia.ai` — tên cũ, cố ý không đổi. `votrongnghia.ai` nếu mua thêm thì chỉ chuyển
  hướng 301 về `.vn` (xem `docs/DEPLOY.md` mục 5).
- Dữ liệu sống trên máy chủ (`Data/*.json` ngoài `seed/`, `admin.json`, `registrations.json`, `hoc-vien.json`,
  `Data/keys/`, `wwwroot/uploads/`) không bao giờ vào git — xem `.gitignore`.

## Quy ước

- Nội dung site xưng "mình" – "bạn". Comment và tài liệu tiếng Việt, giải thích *vì sao*.
- Mỗi thay đổi đáng kể ghi một mục ở đầu `CHANGELOG.md`.
- Chạy local: `dotnet run --launch-profile http` → http://localhost:5090, hoặc mở `VoTrongNghia.sln`
  bằng Visual Studio 2022 rồi F5. Lần đầu mở `/cms` sẽ hiện form tạo tài khoản.
