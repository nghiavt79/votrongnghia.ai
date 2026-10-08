# Kế hoạch theo dõi tiến độ học và tài khoản học viên — votrongnghia.vn

> Trạng thái (08/10/2026): **giai đoạn 1 và 2 đã lên code** — xem `CHANGELOG.md`. Đã chốt: câu 1
> **không** làm điểm danh / bài tập (bỏ GĐ3); câu 2 phương án **A**; câu 4 **có** hiện link phòng học ở
> trang học viên; câu 6 tạm theo đề xuất 12 tháng. Còn mở: câu 3 (người học tự do tự tạo tài khoản),
> câu 5 (nhắc tự động — GĐ4; hiện có thư nhắc soạn tay ở `/cms/hoc-vien`).
>
> Khác với bản dự kiến: mở link đăng nhập chỉ hiện nút xác nhận, bấm mới dùng mã (máy quét thư hay mở
> trước link); API tiến độ dùng mã chống giả riêng thay antiforgery của ASP.NET (xem CLAUDE.md, điều
> dễ vấp 9); mục "Trang công khai thêm" chỉ hiện tên học viên ở header khi đã đăng nhập, người chưa
> đăng nhập thấy link "Trang học viên" ở chân trang — không thêm nút vào header làm rối người học tự do.

---

## 0. Hiện trạng

| Thứ | Hiện nay |
|---|---|
| Tiến độ học | Lưu trên **trình duyệt người học** (`localStorage` `course-progress`, khoá `"khóa/bài"`). Máy chủ không biết gì. Đổi máy, xoá dữ liệu trình duyệt là mất |
| Bài đầu vào của form đăng ký | JS đọc tiến độ trên máy rồi mở khoá form — **lách được** (CLAUDE.md, điều dễ vấp 2) |
| Người học | Không có tài khoản. Người duy nhất "có danh tính" là người gửi đơn lớp online (`registrations.json`) |
| Quản trị biết gì | Số đơn, nội dung đơn, trạng thái duyệt, lịch sử email. **Không biết** bao nhiêu người học khóa nào, bỏ ở bài nào, học viên đã duyệt có học tiếp không |
| Cam kết của lớp | "Tham gia đầy đủ", "làm bài tập sau mỗi buổi", "vắng 2 buổi không báo thì nhường chỗ" — **chưa có gì để theo dõi** |

## 1. Hướng đi: hai tầng, không bắt người học tự do đăng nhập

Nguyên tắc: trang **miễn phí, lan tỏa** — mỗi bước bắt đăng ký là rơi rụng người mới, nhất là người
không rành công nghệ. Nên:

- **Tầng 1 — Thống kê ẩn danh cho mọi người học.** Không đăng nhập, không ghi ai là ai. Đủ để biết
  khóa nào được học, **bài nào người ta bỏ dở** để sửa bài.
- **Tầng 2 — Tài khoản học viên, chỉ cho người được duyệt vào lớp online.** Đây là nhóm đã cam kết,
  cần theo dõi **từng người**: tiến độ, lần học gần nhất, điểm danh, bài tập.
  - **Không mật khẩu**: đăng nhập bằng link gửi qua email (Gmail đã thiết lập ở `/cms/email`).
    Không có mật khẩu để quên, để lộ, để lưu.
  - Người lạ **không tự tạo tài khoản được** — tài khoản sinh ra khi bạn duyệt đơn.
  - Người học tự do vẫn học như bây giờ, tiến độ trên máy.

## 2. Dữ liệu

| File | Nội dung | Git / publish | Seed |
|---|---|---|---|
| `Data/thong-ke.json` | Số lượt **mở bài** và **học xong** theo từng bài, theo tháng. Không IP, không định danh | Ngoài cả hai | Không |
| `Data/hoc-vien.json` | Học viên: email, tên, điện thoại, đơn liên quan, trạng thái, tiến độ, lần học gần nhất | Ngoài cả hai (dữ liệu cá nhân) | Không |
| `Data/dang-nhap.json` | Mã đăng nhập đang chờ dùng (chỉ lưu **băm SHA-256**, hạn 30 phút, dùng một lần) | Ngoài cả hai | Không |
| `Data/bai-tap.json` *(GĐ3)* | Bài tập của từng buổi, bài nộp, nhận xét, điểm danh | Ngoài cả hai | Không |

Mỗi file mới phải loại ở **cả** `.gitignore` lẫn `VoTrongNghia.csproj` (điều dễ vấp 5).

Bản ghi học viên (dự kiến):

```json
{
  "id": "hv-7f3a9c",
  "email": "hocvien@example.com",
  "name": "Trần Thị Học",
  "phone": "0912345678",
  "registrationCodes": ["DK261008-01"],
  "status": "dang-hoc",            // dang-hoc | tam-dung | hoan-thanh | khoa
  "progress": { "ai-cho-nguoi-moi/ai-la-gi": "2026-10-09T20:15:00+07:00" },
  "lastSeenAt": "2026-10-09T20:15:00+07:00",
  "createdAt": "2026-10-08T21:00:00+07:00",
  "adminNote": ""
}
```

Tiến độ lưu **thời điểm** học xong từng bài (không chỉ true/false) để vẽ được dòng thời gian và biết
ai bỏ dở bao lâu.

## 3. Giai đoạn

### Giai đoạn 1 — Thống kê ẩn danh (~0,5–1 ngày) — làm được ngay

- `POST /api/tien-do` nhận `{ khoa, bai, su_kien: "mo" | "xong" }`. Chỉ nhận JSON, giới hạn theo IP
  (như `/api/don-hang` của Dokma), kiểm `khoa/bai` có thật trong `courses.json`. **Không lưu IP.**
- `khoa-hoc.js` gửi sự kiện **một lần cho mỗi bài trên mỗi trình duyệt**: "mo" khi lần đầu mở bài,
  "xong" khi lần đầu bấm "Đánh dấu đã học" (nhớ đã gửi trong `localStorage`). Bỏ đánh dấu không trừ.
- Ghi gộp: đếm trong bộ nhớ, **ghi xuống đĩa 5 phút một lần** và khi app dừng — không ghi mỗi lượt
  bấm (JsonFileStore giữ 20 bản sao lưu mỗi lần ghi, ghi từng lượt là cuốn sạch bản sao lưu hữu ích).
  Kho thống kê dùng chế độ không sao lưu.
- `/cms/thong-ke`: mỗi khóa một **phễu**: mở bài 1 → học xong bài cuối, tỉ lệ còn lại sau từng bài,
  **bài rơi nhiều nhất** tô đỏ. Lọc theo tháng. Kèm số đơn lớp online cùng kỳ để so.
- Bảng điều khiển: thẻ "Tháng này: X người bắt đầu học, Y người học xong khóa đầu vào".

**Xong khi**: bấm qua một khóa trên máy thử, `/cms/thong-ke` hiện đúng số; mở lại bài cũ không đếm
thêm; tắt JS thì không đếm (và site vẫn chạy).

### Giai đoạn 2 — Tài khoản học viên, đăng nhập bằng link email (~2–3 ngày)

**Tạo tài khoản**
- Duyệt đơn ở `/cms/dang-ky/{mã}` → tạo học viên (cùng email đã có thì gắn thêm đơn), thư báo duyệt
  thêm nút **"Vào trang học viên"** (link đăng nhập lần đầu, hạn 7 ngày).
- `/cms/hoc-vien`: thêm tay một học viên (người bạn hướng dẫn ngoài form), khoá / mở tài khoản.

**Đăng nhập không mật khẩu**
- `/hoc-vien/dang-nhap`: nhập email → **luôn** hiện "Nếu email này là học viên, link đăng nhập đã
  được gửi" (không cho người lạ dò email nào có tài khoản).
- Link `/hoc-vien/vao?ma=…`: mã ngẫu nhiên 32 byte, lưu băm, hạn 30 phút, **dùng một lần**. Mở link →
  cookie học viên 60 ngày, tự gia hạn khi còn học.
- Cookie học viên là **scheme riêng** (`vn.hocvien`), tách hẳn cookie quản trị (`vn.admin`): học viên
  không bao giờ chạm được `/cms`, và đăng xuất bên này không ảnh hưởng bên kia.
- Giới hạn: mỗi IP 10 lần / 15 phút, mỗi email 3 link / 15 phút.

**Tiến độ trên máy chủ**
- Đã đăng nhập: "Đánh dấu đã học" ghi lên máy chủ (`POST /api/hoc-vien/tien-do`, có mã chống giả
  form vì đi bằng cookie); trang khóa học dựng dấu ✓ từ máy chủ.
- **Lần đăng nhập đầu: gộp tiến độ đang có trên máy lên tài khoản** — học viên không mất những bài
  đã học trước khi có tài khoản.
- Học trên điện thoại hay máy khác đều thấy cùng tiến độ.

**Trang học viên `/hoc-vien`**
- Tiến độ từng khóa, bài học tiếp theo, buổi học đã được duyệt.
- **Link phòng học hiện ở đây** cho đúng người được duyệt buổi đó (vẫn không ra trang công khai —
  điều dễ vấp 4 giữ nguyên ý).
- Tự xoá tài khoản (theo Nghị định 13/2023: người dùng có quyền yêu cầu xoá dữ liệu).

**Quản trị `/cms/hoc-vien`**
- Danh sách: tên, % từng khóa, lần học gần nhất, trạng thái. Lọc **"không học quá 7 ngày"**.
- Chi tiết: dòng thời gian từng bài, các đơn đã gửi, lịch sử email, ghi chú, nút "Gửi email nhắc".
- Bảng điều khiển: số học viên đang học, số người bỏ dở cần nhắc.

**Trang công khai thêm**
- Header: nút "Vào học" (chưa đăng nhập) / tên học viên (đã đăng nhập).
- `/chinh-sach-du-lieu`: thu thập gì, để làm gì, giữ bao lâu, cách xoá.

**Xong khi**: duyệt một đơn thử → nhận thư có link → vào được `/hoc-vien`, tiến độ trên máy được
gộp lên; học trên máy khác thấy cùng tiến độ; `/cms/hoc-vien` thấy đúng; link hết hạn / dùng lại
bị từ chối; email lạ ở form đăng nhập không lộ gì.

### Giai đoạn 3 — Điểm danh và bài tập (~2 ngày) — nếu chốt ở câu hỏi 1

- **Điểm danh** từng buổi ở `/cms/lop-online`: tích người có mặt / vắng có báo / vắng không báo.
  Vắng không báo lần 2 → học viên tự gắn cờ **"cân nhắc nhường chỗ"** (đúng câu cam kết), bạn quyết.
- **Bài tập** gắn với buổi học: tiêu đề, đề bài (Markdown), hạn nộp.
- Học viên nộp ở `/hoc-vien`: **link** (Google Drive, Canva, GitHub, link sản phẩm…) + ghi chú. Không
  nhận file tải lên — tránh lưu file lạ trên máy chủ và tránh giới hạn dung lượng.
- `/cms`: danh sách bài nộp theo buổi, chấm **Đạt / Cần làm lại** + nhận xét → email cho học viên;
  học viên thấy nhận xét ở `/hoc-vien`.
- Tiến độ học viên = bài học + điểm danh + bài tập, gộp một chỗ ở trang chi tiết học viên.

### Giai đoạn 4 — Nhắc tự động và chứng nhận (~1–1,5 ngày) — tuỳ chọn

- Tác vụ nền mỗi sáng (giờ Việt Nam):
  - nhắc học viên **không học 7 ngày** (tối đa một thư mỗi tuần mỗi người);
  - nhắc **trước buổi học 1 ngày**, kèm giờ và link phòng học;
  - nhắc bài tập sắp hết hạn.
  Mọi thư nhắc ghi vào lịch sử email, có nút tắt nhắc cho từng học viên.
- **Chứng nhận hoàn thành khóa**: trang `/chung-nhan/{mã}` in được và chia sẻ được (có ảnh `og:image`
  riêng) — người học khoe lên Facebook là thêm một lần lan tỏa. Chỉ cấp khi tiến độ trên máy chủ đủ.

## 4. Bảo mật và dữ liệu cá nhân — ràng buộc phải làm, không để sau

1. Mã đăng nhập: ngẫu nhiên 32 byte, **chỉ lưu băm**, hạn ngắn, dùng một lần, so sánh thời gian cố
   định.
2. Hai scheme cookie tách biệt; trang học viên không bao giờ nhận cookie quản trị và ngược lại.
3. Form đăng nhập **không tiết lộ** email nào có tài khoản; giới hạn số lần theo IP và theo email.
4. API ghi tiến độ của học viên đi bằng cookie → bắt buộc mã chống giả form.
5. API thống kê ẩn danh: chỉ JSON, giới hạn theo IP, kiểm bài có thật, **không lưu IP**.
6. Trang chính sách dữ liệu; học viên tự xoá được; bạn xoá được từ `/cms`.
7. Giữ dữ liệu: học viên không hoạt động **12 tháng** thì ẩn danh hoá (giữ số liệu, bỏ tên / email /
   điện thoại) — con số chờ chốt ở câu hỏi 6.
8. `Data/hoc-vien.json` có trong bản sao lưu máy chủ, **không** có trong nút tải bản sao lưu nội dung
   ở `/cms/tong-quan` (giống đơn đăng ký).

## 5. Bố cục repo dự kiến

```
Models/Learner.cs                 học viên, tiến độ, trạng thái
Models/LessonStats.cs             thống kê ẩn danh
Services/LessonStatsStore.cs      đếm trong bộ nhớ, ghi 5 phút một lần
Services/LearnerStore.cs          kho học viên, gộp tiến độ
Services/LoginLinkService.cs      tạo / kiểm mã đăng nhập
Pages/HocVien/DangNhap.cshtml     /hoc-vien/dang-nhap
Pages/HocVien/Vao.cshtml          /hoc-vien/vao?ma=…
Pages/HocVien/Index.cshtml        /hoc-vien
Pages/ChinhSachDuLieu.cshtml      /chinh-sach-du-lieu
Pages/Admin/ThongKe.cshtml        /cms/thong-ke
Pages/Admin/HocVien/Index.cshtml  /cms/hoc-vien
Pages/Admin/HocVien/ChiTiet.cshtml
(GĐ3) Models/Assignment.cs, Pages/Admin/BaiTap/…, phần nộp bài trong /hoc-vien
```

## 6. Những điểm nên cân nhắc

- **Dữ liệu JSON đủ dùng tới vài nghìn học viên.** Mỗi lần học viên đánh dấu một bài là ghi lại cả
  `hoc-vien.json`; 500 học viên × 30 bài vẫn dưới 1MB, ghi tức thì. Quá khoảng 2.000 học viên hoạt
  động đồng thời thì nên tách tiến độ sang SQLite — đã tính sẵn để đổi kho mà không đổi giao diện.
- **Thống kê ẩn danh là xấp xỉ**: một người học trên hai máy đếm thành hai; người xoá dữ liệu trình
  duyệt rồi học lại đếm thêm. Đủ để thấy xu hướng và bài rơi, không phải số tuyệt đối.
- **Thư đăng nhập phụ thuộc Gmail.** Gmail lỗi thì học viên không đăng nhập được; `/cms/hoc-vien` có
  nút **chép link đăng nhập** để bạn gửi tay qua Zalo khi cần.
- **Bài đầu vào vẫn lách được với người chưa có tài khoản** (người gửi đơn lần đầu chưa là học viên) —
  trừ khi chốt phương án B ở câu hỏi 2.
- Mỗi giai đoạn ghi một mục `CHANGELOG.md` và cập nhật bảng bố cục trong `CLAUDE.md`.

## 7. Câu hỏi cần chốt trước khi làm

1. **Điểm danh và bài tập** (giai đoạn 3): có làm không? Bài nộp chỉ là link (đề xuất) hay cần tải
   file lên?
2. **Tài khoản tạo lúc nào?**
   - **A (đề xuất)**: chỉ khi bạn duyệt đơn. Ít dữ liệu cá nhân nhất, người học tự do không bị làm phiền.
   - **B**: ngay khi gửi đơn — người đăng ký xác minh email trước, **bài đầu vào thành kiểm tra thật**
     trên máy chủ, lọc luôn email giả. Đổi lại: thêm một bước trước khi gửi đơn.
3. Người học tự do có được **tự tạo tài khoản** để đồng bộ tiến độ giữa các máy không? (Đề xuất:
   chưa — mở sau nếu nhiều người hỏi.)
4. **Link phòng học** có hiện ở trang học viên không? (Đề xuất: có, chỉ cho người được duyệt buổi đó.)
5. **Nhắc tự động** (giai đoạn 4): có làm không, và nhắc sau bao nhiêu ngày không học (đề xuất 7)?
6. **Giữ dữ liệu bao lâu** sau khi học viên ngừng hoạt động (đề xuất 12 tháng rồi ẩn danh hoá)?

Thứ tự đề xuất: **GĐ1 ngay** (độc lập) → chốt câu 1, 2, 4 → **GĐ2** trước buổi học online đầu tiên →
GĐ3 cùng lúc mở lớp nếu chốt có → GĐ4 sau khi lớp đầu chạy ổn.
