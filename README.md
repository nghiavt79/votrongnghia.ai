# votrongnghia.ai — Trang cá nhân

Website tĩnh (HTML/CSS/JS thuần), không cần build.

## Cấu trúc
```
index.html              Trang chủ
post.html               Trang chi tiết bài viết   (post.html?p=<slug>)
course.html             Trang khóa học / bài học  (course.html?c=<khóa>&l=<bài>)
bai-viet.html           Trang tất cả bài viết (tìm kiếm, lọc theo chủ đề)
dang-ky.html            Trang đăng ký lớp học online (bài đầu vào + form + cam kết)
ai-templates.html       Kho AI Templates cho Claude Code (ai-templates.html?type=<loại>&item=<đường dẫn>)
assets/css/style.css    Giao diện (sáng/tối, responsive)
assets/js/data.js       ★ DANH SÁCH NỘI DUNG — tên, bio, công cụ, video, khóa học, bài viết…
assets/js/common.js     Header, footer, theme, menu dùng chung
assets/js/home.js       Trang chủ
assets/js/post.js       Trang bài viết (mục lục tự động, bài liên quan, chia sẻ)
assets/js/course.js     Trang khóa học (danh sách bài, tiến độ học, bài trước/sau)
content/posts/*.md              ★ Nội dung bài viết (Markdown)
content/courses/<khóa>/*.md     ★ Nội dung bài học (Markdown)
assets/js/ai-templates.js       Trang AI Templates (tìm kiếm, lọc, bảng chi tiết, xem trước)
assets/data/ai-templates.json   Dữ liệu AI Templates (tạo tự động — không sửa tay)
assets/data/ai-templates-vi.json  ★ Mô tả tiếng Việt của từng mẫu (sửa tay được)
assets/js/ai-templates-vi.js    Tên tiếng Việt của các danh mục
tools/update-ai-templates.py    Script cập nhật dữ liệu AI Templates
assets/img/avatar.svg   Ảnh đại diện tạm
```

## Thêm bài viết mới
1. Tạo file `content/posts/ten-bai-viet.md` và viết nội dung bằng Markdown (dùng `##` cho các mục — sẽ tự hiện trong Mục lục).
2. Thêm vào **đầu** mảng `POSTS` trong `assets/js/data.js`:
   ```js
   { slug: "ten-bai-viet", title: "Tiêu đề", desc: "Mô tả ngắn", date: "2026-10-07", read: "5 phút", tags: ["AI"] },
   ```
   `slug` phải trùng với tên file `.md`.

## Thêm bài học mới
1. Tạo file `content/courses/<slug-khoa>/<slug-bai>.md`.
2. Thêm vào mảng `lessons` của khóa tương ứng trong `COURSE`:
   ```js
   { slug: "slug-bai", title: "Tên bài học", time: 6, video: "ID_YouTube" }, // video không bắt buộc
   ```

## Bật / tắt các mục
Trong `assets/js/data.js`, mục nào để trống sẽ tự ẩn:
- `SITE.youtube = ""` → ẩn nút Đăng ký kênh, mục Video. Có kênh thì điền link + thêm video vào `VIDEOS`.
- `SITE.donate = false` → ẩn mục Ủng hộ; nút nổi góc phải thành "Chia sẻ trang này".
- `SOCIALS`: mạng xã hội nào để `url: ""` thì không hiển thị.

## Lớp học online miễn phí
Cấu hình trong `LIVE` (`assets/js/data.js`):
- `sessions`: lịch các buổi học (buổi đã qua tự ẩn; trống thì hiện "Sắp khai giảng").
- `requireCourse`: khóa phải học xong mới mở form đăng ký (để trống = không yêu cầu).
- `minHoursPerWeek`, `commitments`, `pledge`: điều kiện và nội dung cam kết.
- `formEndpoint`: nơi nhận đơn. Để trống thì người đăng ký gửi qua email. Nên tạo form miễn phí tại [formspree.io](https://formspree.io) rồi dán link `https://formspree.io/f/xxxx` vào — đơn sẽ về thẳng hộp thư của bạn.

## Cập nhật AI Templates
Dữ liệu lấy từ thư viện mã nguồn mở [claude-code-templates](https://github.com/davila7/claude-code-templates) (aitmpl.com, giấy phép MIT). Thỉnh thoảng chạy lệnh sau để lấy các mẫu mới:
```bash
python tools/update-ai-templates.py
```
Script sẽ liệt kê các mẫu mới chưa có bản dịch. Thêm bản dịch vào `assets/data/ai-templates-vi.json` theo dạng `"<loại>/<đường dẫn>": "mô tả tiếng Việt"`; mẫu chưa dịch tự hiện mô tả gốc tiếng Anh. Danh mục mới thì thêm tên vào `assets/js/ai-templates-vi.js`.

Nội dung xem trước trong bảng chi tiết được tải trực tiếp từ GitHub khi người dùng bấm vào (bản gốc tiếng Anh, có link Google Dịch).

## Chạy thử
Cần chạy qua server (mở file trực tiếp sẽ không tải được nội dung Markdown):
```bash
python -m http.server 5500
```
Mở http://localhost:5500

## Đưa lên mạng (miễn phí)
- **Netlify / Vercel / Cloudflare Pages**: kéo thả thư mục hoặc kết nối GitHub repo.
- **GitHub Pages**: push lên repo → Settings → Pages → chọn nhánh `main`.
- Trỏ tên miền `votrongnghia.ai` qua DNS của nhà cung cấp hosting.
