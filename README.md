# votrongnghia.vn

Trang cá nhân chia sẻ kiến thức AI và vibe code — miễn phí, cho mọi người.
ASP.NET Core 8 Razor Pages, dữ liệu JSON, quản trị ở `/cms`.

## Chạy ở máy

```powershell
dotnet run --launch-profile http
```

Mở http://localhost:5090. Lần đầu mở http://localhost:5090/cms sẽ hiện form tạo tài khoản quản trị.
Hoặc mở `VoTrongNghia.sln` bằng Visual Studio 2022 rồi bấm F5.

## Sửa nội dung

Mọi thứ sửa trong `/cms`, không cần đụng code:

| Việc | Trang |
|---|---|
| Đọc và duyệt đơn đăng ký lớp online | `/cms/dang-ky` |
| Lịch buổi học (kèm link phòng học riêng tư), điều kiện đăng ký, bản cam kết | `/cms/lop-online` |
| Thiết lập Gmail để site tự gửi email (có hướng dẫn từng bước) | `/cms/email` |
| Viết bài | `/cms/bai-viet` |
| Soạn khóa học, bài học | `/cms/khoa-hoc` |
| Tên, giới thiệu, link mạng xã hội, công cụ AI, video, ủng hộ | `/cms/thong-tin` |

Ô link nào để trống thì phần đó tự ẩn trên site (chưa có kênh YouTube → không có nút Đăng ký kênh,
không có mục Video).

## Cập nhật AI Templates

Dữ liệu lấy từ thư viện mã nguồn mở [claude-code-templates](https://github.com/davila7/claude-code-templates)
(aitmpl.com, giấy phép MIT). Thỉnh thoảng chạy lệnh sau để lấy các mẫu mới:

```powershell
python scripts/update-ai-templates.py
```

Script liệt kê các mẫu mới chưa có bản dịch. Thêm bản dịch vào `wwwroot/assets/data/ai-templates-vi.json`
theo dạng `"<loại>/<đường dẫn>": "mô tả tiếng Việt"`; mẫu chưa dịch tự hiện mô tả gốc tiếng Anh.
Danh mục mới thì thêm tên vào `wwwroot/assets/js/ai-templates-vi.js`.

Nội dung xem trước trong bảng chi tiết được tải trực tiếp từ GitHub khi người dùng bấm vào (bản gốc
tiếng Anh, có link Google Dịch).

## Đưa lên mạng

Xem [docs/DEPLOY.md](docs/DEPLOY.md) (IIS). Sau mỗi lần deploy chạy `scripts/kiem-tra-site.ps1`.

Ghi chú cho người phát triển (và cho Claude): [CLAUDE.md](CLAUDE.md). Lịch sử thay đổi: [CHANGELOG.md](CHANGELOG.md).
