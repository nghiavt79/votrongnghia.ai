# Changelog

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
- Chưa làm: tải ảnh lên qua `/cms` (ảnh đại diện chép tay vào `wwwroot/assets/img/`), gửi email tự động.

## 2026-10-07 — Bản site tĩnh

Trang chủ theo bố cục phandonggiang.com, bài viết và khóa học dạng Markdown, lớp học online miễn
phí có cam kết, AI Templates. Không YouTube, không thu phí — nút nổi là "Chia sẻ trang này".
