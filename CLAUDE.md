# votrongnghia.ai

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
| `Pages/DangKy.cshtml` | `/dang-ky`: đăng ký lớp online miễn phí có cam kết. Form khoá sẵn, `dang-ky.js` mở khi đã học xong khóa đầu vào; máy chủ kiểm lại mọi điều kiện khác ở `Services/RegistrationStore.cs` |
| `Pages/AiTemplates.cshtml` | `/ai-templates`: trang thuần JS, dữ liệu ở `wwwroot/assets/data/ai-templates.json` (sinh bằng `scripts/update-ai-templates.py`) và `ai-templates-vi.json` (bản dịch, sửa tay) |
| `Pages/Shared/_SiteLayout.cshtml` | Header, menu, footer, nút nổi — máy chủ dựng. `wwwroot/assets/js/site.js` chỉ thêm phần bấm được |
| `Pages/Admin/` | `/cms` (đăng nhập / tạo tài khoản lần đầu), `/cms/tong-quan`, `/cms/dang-ky`, `/cms/lop-online`, `/cms/bai-viet`, `/cms/khoa-hoc`, `/cms/thong-tin`, `/cms/doi-mat-khau` |
| `Data/seed/*.json` | Dữ liệu gốc, có trong git và trong bản publish. Máy chủ chưa có file sống thì lần khởi động đầu tự chép sang |
| `Data/site.json`, `posts.json`, `courses.json`, `live.json` | Dữ liệu sống, sửa trong `/cms`. Ngoài git, ngoài bản publish |
| `wwwroot/uploads/` | Ảnh tải lên qua `/cms` (`Services/ImageService.cs`): `trang/` ảnh đại diện, `bai-viet/` ảnh bìa và ảnh trong bài, `khoa-hoc/` ảnh trong bài học. Ngoài git, ngoài bản publish |
| `Data/registrations.json` | Đơn đăng ký: họ tên, email, số điện thoại người học. Ngoài git, ngoài bản publish, không có seed |
| `Services/` | `JsonFileStore`, `AdminAccountStore`, `ChuoiRongKhongThanhNull`, `Slugs` chép nguyên từ Dokma; `ImageService` chép từ Dokma, gọn lại một hàm cho mọi loại ảnh. `MarkdownRenderer` thêm mục lục và bỏ qua khối code khi tìm chỗ trống |
| `docs/DEPLOY.md`, `scripts/kiem-tra-site.ps1` | Deploy lên IIS; script kiểm tra site sau deploy, chạy được cả vào localhost |

## Những điều dễ vấp

1. **Ô link để trống là ẩn, không phải lỗi.** Mọi link (YouTube, Facebook, PayPal…) trống thì phần
   đó không hiện. Đừng bao giờ để link mẫu kiểu `your-profile` lọt ra site — `/cms/thong-tin` chặn.
2. **Bài đầu vào không phải khoá thật.** Tiến độ học nằm trên máy người học, ai sửa `localStorage`
   là qua. Đó là bộ lọc người chỉ tò mò; lớp lọc thật là người duyệt đọc đơn ở `/cms/dang-ky`.
3. **Site không gửi email.** Đổi trạng thái đơn chỉ ghi vào sổ; báo kết quả bằng nút "Soạn email"
   (mở ứng dụng email của người duyệt). Muốn gửi tự động thì phải thêm SMTP — chưa làm.
4. **Không dán link phòng học (Meet/Zoom) vào lịch buổi học** — lịch hiện công khai trên trang chủ.
   Link chỉ gửi riêng cho người được duyệt.
5. **File dữ liệu sống mới thì phải loại ở 2 chỗ**: `.gitignore` và `VoTrongNghia.csproj`
   (`CopyToPublishDirectory="Never"`). Chỉ `.gitignore` là chưa đủ: `dotnet publish` đọc đĩa,
   bản publish từ máy dev sẽ đè dữ liệu thật trên máy chủ. `Data/keys/` là khoá ký cookie — không
   bao giờ chép đi đâu.
6. **`[BindProperty] Slug` trùng tên tham số route `slug`** (bẫy Dokma đã dính): tham số handler
   phải khai `[FromRoute]` — xem `Pages/Admin/KhoaHoc/Sua.cshtml.cs`.
7. **Trong .cshtml đừng đặt biến tên `page`**: `@page.Title` bị hiểu là chỉ thị `@page`.
8. **Đổi mã buổi học là mồ côi đơn.** `LiveSession.Id` cố định lúc tạo; đơn trỏ tới mã này và giữ
   thêm `SessionLabel` để đọc được kể cả khi buổi bị xoá.
9. **CSP nằm trong `web.config`** (chỉ IIS gắn). Thêm nguồn ngoài (CDN, video, ảnh) thì mở thêm ở
   đó, rồi chạy `scripts/kiem-tra-site.ps1` vào site thật.
10. **Ảnh tải lên luôn ra JPG tên mới** (`ImageService`): giải mã thật, xoay theo EXIF rồi xoá EXIF
    (GPS), thu nhỏ; ảnh bìa cắt 1200×630, ảnh đại diện cắt vuông 512. Tên mới mỗi lần nên ảnh được
    cache 30 ngày. Xoá ảnh còn nằm trong thân bài thì bị chặn — gỡ dòng `![…](…)` trước. Trang có tải
    ảnh khai `[RequestSizeLimit(64MB)]`, khớp `maxAllowedContentLength` trong `web.config`.
11. **Máy chủ và repo trôi khỏi nhau** khi sửa trong `/cms`. Tải bản sao lưu ở `/cms/tong-quan`
    rồi chép vào `Data/seed/` khi muốn seed mới phản ánh nội dung thật.

## Quy ước

- Nội dung site xưng "mình" – "bạn". Comment và tài liệu tiếng Việt, giải thích *vì sao*.
- Mỗi thay đổi đáng kể ghi một mục ở đầu `CHANGELOG.md`.
- Chạy local: `dotnet run --launch-profile http` → http://localhost:5090, hoặc mở `VoTrongNghia.sln`
  bằng Visual Studio 2022 rồi F5. Lần đầu mở `/cms` sẽ hiện form tạo tài khoản.
