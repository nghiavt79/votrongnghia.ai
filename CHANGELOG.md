# Changelog

## 2026-10-08 — Gửi email tự động qua Gmail

- **`/cms/email`** (menu "Gửi email (Gmail)"): hướng dẫn 5 bước có nhãn Xong / Chưa — chọn Gmail riêng
  cho trang, bật xác minh 2 bước, tạo mật khẩu ứng dụng, dán vào site, gửi thư thử. Mật khẩu ứng
  dụng mã hoá bằng Data Protection, không bao giờ hiện lại; dán nguyên cả dấu cách cũng được.
- **Tự gửi khi có đơn mới**: thư xác nhận cho người đăng ký (mã đơn, buổi đã chọn) và thư báo cho
  bạn (đủ câu trả lời, link duyệt; bấm Trả lời là trả lời thẳng người đăng ký). Đi qua hàng đợi nền:
  Gmail lỗi thì đơn vẫn lưu, trang cảm ơn vẫn hiện ngay.
- **Báo kết quả khi duyệt**: ô "Gửi email báo kết quả" ở trang chi tiết đơn (bật sẵn). Thư duyệt kèm
  lịch học và **link phòng học** — trường mới, riêng tư, ở form buổi học (`/cms/lop-online`), không
  hiện trên site. Thêm khung soạn thư tuỳ ý (điền sẵn theo trạng thái) và **lịch sử email** từng đơn.
- Lỗi Gmail đổi thành câu dễ hiểu: sai mật khẩu ứng dụng, vượt giới hạn ngày, máy chủ chặn cổng 587.
- Bảng điều khiển nhắc khi chưa thiết lập Gmail / chưa gửi thử thành công / buổi học chưa có link.
- Chưa thiết lập Gmail thì vẫn còn nút soạn email bằng ứng dụng thư như trước.

## 2026-10-08 — Tải ảnh lên qua /cms

- `Services/ImageService.cs` chép từ Dokma: giải mã thật (đổi đuôi file không qua được), xoay ảnh
  dọc chụp điện thoại về đúng chiều, **xoá EXIF và vị trí GPS**, thu nhỏ, lưu JPG tên mới. Ảnh iPhone
  HEIC báo rõ phải đổi sang JPG.
- **Ảnh đại diện** ở `/cms/thong-tin`: tự cắt vuông 512px, thay ảnh là xoá file cũ.
- **Bài viết**: ảnh bìa (tự cắt 1200×630 — khổ ảnh xem trước khi chia sẻ Facebook / Zalo) kèm mô tả
  ảnh (bắt buộc khi đăng); ảnh trong bài có nút "Chèn" vào chỗ con trỏ. Ảnh bìa hiện ở thẻ bài, đầu
  bài, `og:image` và JSON-LD.
- **Bài học**: ảnh trong bài, cùng cách chèn.
- Xoá ảnh còn nằm trong bài thì bị chặn; xoá bài / bài học / khóa thì xoá luôn ảnh của nó.
- Mọi trang có `og:image` (mặc định là ảnh đại diện). Ảnh nằm ở `wwwroot/uploads/`, ngoài git và ngoài
  bản publish; `web.config` nới giới hạn request lên 64MB (5 ảnh × 12MB một lần).
- Deploy: thêm quyền ghi cho `wwwroot\uploads` (xem `docs/DEPLOY.md`).

## 2026-10-08 — Chuyển sang ASP.NET Core 8 theo khuôn Dokma

Bản tĩnh HTML/JS (commit đầu tiên) chuyển sang Razor Pages + dữ liệu JSON + `/cms`, cùng khuôn
với Dokma.

- **Trang công khai dựng phía máy chủ**, đường dẫn gọn: `/bai-viet/{slug}`, `/khoa-hoc/{khóa}/{bài}`,
  `/dang-ky`, `/ai-templates`. Có canonical, JSON-LD BlogPosting, `sitemap.xml`, `robots.txt`, trang
  404. Giao diện, màu, chữ giữ nguyên bản tĩnh.
- **Nội dung chuyển vào `Data/seed/`**: 6 bài viết, 4 khóa / 10 bài học, thông tin trang, điều kiện
  lớp online. Link nội bộ kiểu cũ trong bài (`post.html?p=…`, `./#…`) đổi sang đường dẫn mới. Link
  mẫu (`your-profile`, số tài khoản `0000…`, email tạm) để trống — phần đó tự ẩn tới khi điền thật.
- **Đơn đăng ký lớp online lưu trên máy chủ** (`Data/registrations.json`), thay cho gửi qua email /
  Formspree. Máy chủ kiểm lại mọi điều kiện (độ dài câu trả lời, giờ tự học tối thiểu, đủ cam kết,
  câu cam kết), chặn trùng email trong cùng buổi, ô bẫy chống rác, giới hạn 8 lần gửi / 10 phút / IP.
- **`/cms`** (AdminLTE 4, một tài khoản, tạo ngay lần đầu mở):
  - Bảng điều khiển: số đơn mới, việc cần làm tự đọc từ dữ liệu, tải bản sao lưu nội dung.
  - Đơn đăng ký: lọc theo trạng thái / buổi, đọc từng đơn, duyệt / chờ đợt sau / không phù hợp,
    ghi chú riêng, soạn sẵn email báo kết quả, đếm chỗ đã duyệt, xuất CSV (mở được bằng Excel).
  - Lịch & điều kiện: thêm / sửa / xoá buổi học, chọn khóa đầu vào, giờ tối thiểu, cam kết.
  - Bài viết, khóa học, bài học: soạn Markdown, xem trước, nháp / đăng, chủ đề, sắp xếp bài.
  - Thông tin trang: giới thiệu, hành trình, công cụ AI, video, mạng xã hội, ủng hộ.
- `docs/DEPLOY.md`, `scripts/kiem-tra-site.ps1` chép từ Dokma, đổi theo site này.
- Chưa làm: tải ảnh lên qua `/cms`, gửi email tự động.

## 2026-10-07 — Bản site tĩnh

Trang chủ theo bố cục phandonggiang.com, bài viết và khóa học dạng Markdown, lớp học online miễn
phí có cam kết, AI Templates. Không YouTube, không thu phí — nút nổi là "Chia sẻ trang này".
