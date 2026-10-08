# Changelog

## 2026-10-08 — Mã QR chuyển khoản cho mục Ủng hộ

- Bật mục **Ủng hộ một ly cafe** (Vietcombank, đã ghi cả vào `Data/seed/site.json`).
- **Mã VietQR** (chuẩn NAPAS / EMVCo) dựng ngay trên máy chủ từ ngân hàng + số tài khoản ở
  `/cms/thong-tin` (`Services/VietQr.cs`, thư viện QRCoder — MIT). Không gọi dịch vụ ngoài, không mở
  thêm CSP. Quét bằng app ngân hàng là ra sẵn số tài khoản, app tự hiện tên chủ tài khoản; nội dung
  chuyển khoản điền sẵn "Ung ho votrongnghia", không ghi số tiền.
- `/ung-ho/qr.svg` (hiện trong thẻ chuyển khoản ở trang chủ), `/ung-ho/qr.png?tai=1` (nút "Tải ảnh QR"
  để gửi qua Zalo / Facebook). Tắt mục Ủng hộ hoặc không nhận ra ngân hàng thì không có QR (404).
- `/cms/thong-tin` xem trước mã và báo khi chưa nhận ra tên ngân hàng (nhận 18 ngân hàng phổ biến).
- Chỉ có một cách ủng hộ (chuyển khoản hoặc PayPal) thì thẻ nằm giữa, không lệch trái.

## 2026-10-08 — AI Templates xếp theo việc cần làm

Góp ý của khách: bố cục cũ trùng với nhiều trang khác (gần như chép aitmpl.com — thanh bên theo loại
kỹ thuật + lưới thẻ) và khó dùng với người chưa biết Skill / Agent / MCP khác nhau thế nào.

- Mở trang là câu hỏi **"Bạn muốn AI giúp việc gì?"** + ô tìm lớn + 3 bước dùng mẫu.
- **8 nhóm việc** (Văn phòng & tài liệu, Kinh doanh & marketing, Thiết kế & sáng tạo, Nghiên cứu & dữ
  liệu, Lập trình & làm web, Bảo mật & kiểm thử, Tự động hóa & vận hành, Tùy chỉnh Claude Code), gom từ
  104 danh mục gốc — sửa ở `TPL_GROUPS` trong `assets/js/ai-templates-vi.js`.
- **"Bắt đầu từ đây"**: 8 mẫu chọn sẵn cho việc văn phòng (Word, Excel, slide, PDF, email…), mỗi mẫu
  một câu "dùng khi nào" (`TPL_STARTERS`). Chỉ hiện khi chưa lọc gì.
- Loại kỹ thuật lùi xuống làm **bộ lọc phụ** dạng chip, kèm hộp "Nên chọn loại nào?" giải thích bằng
  lời thường (`TPL_TYPE_HELP`); bảng chi tiết của mẫu cũng có một câu giải thích loại. Chủ đề chi tiết
  chỉ hiện khi đã chọn nhóm việc.
- Danh sách **dạng hàng** thay cho tường thẻ; tìm kiếm chạy trên toàn thư viện, mẫu có từ khoá trong
  tên lên trước; gõ có dấu ("hợp đồng") khớp nguyên cụm, gõ không dấu khớp đầu chữ.
- Link cũ (`?type=…&cat=…&item=…`) vẫn mở đúng; link mới `?viec=…&loai=…&chude=…&item=loai/duong-dan`.
- `scripts/update-ai-templates.py` báo danh mục mới chưa xếp nhóm và mẫu chọn sẵn đã bị gỡ.

## 2026-10-08 — Tài khoản học viên, đăng nhập bằng link email (GĐ2)

Giai đoạn 2 của `docs/ke-hoach-hoc-vien.md`, đã chốt: không làm điểm danh / bài tập (GĐ3); tài khoản
**chỉ tạo khi duyệt đơn** (phương án A); link phòng học **có** hiện ở trang học viên.

- **Duyệt đơn là có tài khoản**: chọn "Đã duyệt" ở `/cms/dang-ky/{mã}` tạo học viên theo email (email
  đã có tài khoản thì gắn thêm đơn). Thư báo duyệt thêm phần "trang học viên" kèm **link đăng nhập
  dùng một lần, hạn 7 ngày**. Đơn đã duyệt từ trước: nút "Tạo tài khoản" ở `/cms/hoc-vien`.
- **Đăng nhập không mật khẩu**: `/hoc-vien/dang-nhap` nhập email → link hạn 30 phút. Luôn trả cùng một
  câu dù email có tài khoản hay không. Link chỉ lưu băm SHA-256, dùng một lần; mở link chỉ hiện nút
  "Vào trang học viên" (máy quét thư mở trước link không tiêu mất mã). Giới hạn 10 lần / 15 phút mỗi
  IP, 3 link / 15 phút mỗi email.
- **Cookie học viên `vn.hocvien`** (60 ngày, tự gia hạn), scheme riêng: cookie học viên không mở
  được `/cms`, cookie quản trị không mở được `/hoc-vien`. Khoá / ẩn danh hoá đổi dấu bảo mật → mọi
  phiên đang mở bị đăng xuất ngay.
- **Tiến độ trên máy chủ**: học viên đánh dấu bài là ghi lên tài khoản (`/api/hoc-vien/tien-do`, JSON
  + mã chống giả riêng). Lần đầu đăng nhập trên một trình duyệt, bài đã học trước đó được **gộp lên**
  tài khoản; sau đó máy chủ là gốc, học trên máy nào cũng cùng tiến độ. Người học tự do không đổi gì.
- **`/hoc-vien`**: buổi học đã được duyệt kèm nút "Vào phòng học" (link phòng học chỉ hiện ở đây và
  trong thư duyệt), tiến độ từng khóa, đăng xuất, **tự xoá tài khoản**. Header hiện tên học viên khi đã
  đăng nhập; chân trang có link "Trang học viên".
- **`/cms/hoc-vien`**: danh sách với % từng khóa, lần học gần nhất, lọc "không học quá 7 ngày", thêm tay
  học viên. Chi tiết: dòng thời gian từng bài, các đơn, lịch sử thư, ghi chú, trạng thái (đang học /
  tạm dừng / hoàn thành / khoá), gửi link đăng nhập qua email hoặc **tạo link để gửi tay qua Zalo**,
  soạn thư nhắc (điền sẵn bài kế tiếp), xoá tài khoản.
- Bảng điều khiển: dòng "Học viên: X đang học · Y không học quá 7 ngày", việc cần làm khi có người
  lâu không học hoặc tài khoản quá hạn giữ.
- **`/chinh-sach-du-lieu`** thêm mục 3 "Khi bạn là học viên lớp online": thu gì, cookie, giữ 12 tháng
  kể từ lần hoạt động cuối rồi ẩn danh hoá (nút ở `/cms/hoc-vien`), tự xoá.
- Dữ liệu mới `Data/hoc-vien.json`, `Data/dang-nhap.json`: loại khỏi git và bản publish. `robots.txt`
  chặn `/hoc-vien`; `scripts/kiem-tra-site.ps1` kiểm thêm trang học viên.

## 2026-10-08 — Đợt 1 trước ra mắt: định vị, khung bài học, chính sách dữ liệu, thống kê ẩn danh

Theo bản rà soát "điểm cần hoàn thiện trước khi ra mắt". Nội dung nháp (đối tượng, tiêu chí lớp, khung
10 bài) viết theo hướng người đi làm chưa rành công nghệ — sửa hết được trong /cms.

- **Tiêu chí lớp online** (`/cms/lop-online`): "Lớp dành cho ai", "Lớp chưa hợp nếu bạn", "Học xong bạn
  sẽ" — hiện ở trang đăng ký và trang chủ. Trang đăng ký thêm **lịch các buổi kèm số chỗ còn lại**
  (tính theo đơn đã duyệt; hết chỗ thì mời đăng ký chờ đợt sau).
- **"Trang này dành cho ai"** ngay dưới đầu trang chủ: 4 nhóm, mỗi nhóm một link "Bắt đầu từ đây".
- **"Mình dùng AI để làm…"** (việc thật + kết quả đo được) và **"Học viên nói gì"** — sửa ở
  `/cms/thong-tin`, trống thì ẩn. Seed để trống: chỉ đăng chuyện thật, cảm nhận thật đã xin phép.
- **Khung bài học**: mỗi bài có "Học xong bạn làm được" (đầu bài), "Bài tập thực hành" (khung riêng,
  ngay trên nút đánh dấu đã học), "Bước tiếp theo". Nháp cho cả 10 bài, ví dụ theo nghề (văn phòng,
  chủ shop, giáo viên); mục "Bài tập" cũ trong thân bài chuyển ra khung riêng. `/cms` nhắc bài thiếu khung.
- **`/chinh-sach-du-lieu`**: thu gì, dùng làm gì, ai xem, giữ bao lâu, dịch vụ bên ngoài, quyền xoá.
  Link ở chân trang và form đăng ký. Cam kết giữ đơn **12 tháng** có cách thực hiện: `/cms/dang-ky`
  báo đơn quá hạn và nút **ẩn danh hoá** (xoá tên, email, điện thoại, câu trả lời; giữ số liệu).
- **Thống kê ẩn danh** (GĐ1 của `docs/ke-hoach-hoc-vien.md`): `POST /api/tien-do`, mỗi bài trên mỗi
  trình duyệt đếm một lần "mở" và "học xong". Không cookie, không IP, bỏ qua bot. Đếm trong bộ nhớ,
  ghi `Data/thong-ke.json` 5 phút một lần (không sao lưu — `JsonFileStore` thêm `keepBackups`).
  `/cms/thong-ke`: phễu từng khóa theo tháng, tô đỏ **bài người học dừng trước nhiều nhất**; bảng điều
  khiển có dòng tóm tắt tháng này.

## 2026-10-08 — Tên miền chính: votrongnghia.vn

- `Site:BaseUrl` = `https://votrongnghia.vn`: canonical, `og:url`, `og:image`, sitemap, robots.txt, link
  trong email đều theo tên miền mới. Tên hiện trong /cms, script kiểm tra site, hướng dẫn deploy đổi theo.
- `docs/DEPLOY.md` mục 5: chuyển hướng 301 `www.` và `votrongnghia.ai` (nếu mua thêm) về `.vn` bằng URL
  Rewrite trên máy chủ.
- Thư mục dự án và repo GitHub giữ tên `votrongnghia.ai`.
- **Zalo** vào `/cms/thong-tin`: ô Zalo liên hệ (gõ số điện thoại hay dán link đều được, máy đổi thành
  `https://zalo.me/…`) và ô nhóm Zalo cộng đồng. Hiện ở mục Liên hệ (nút "Nhắn Zalo"), Cộng đồng,
  Theo dõi mình, phần giới thiệu và chân trang; trống thì ẩn. Mục Liên hệ giờ hiện khi có email
  **hoặc** Zalo. Chân trang thêm link TikTok.

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
