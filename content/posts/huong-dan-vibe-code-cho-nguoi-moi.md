Bạn chưa từng viết một dòng code nào? Không sao cả. Bài này sẽ dẫn bạn đi từ con số 0 đến khi **tự làm ra một công cụ nhỏ chạy được** và gửi link cho bạn bè dùng thử. Bạn chỉ cần biết gõ tiếng Việt và có một chút kiên nhẫn.

Nếu bạn chưa biết vibe code là gì, hãy đọc trước bài [Vibe code từ góc nhìn lập trình viên 20 năm](post.html?p=vibe-code-goc-nhin-lap-trinh-vien-20-nam). Còn tóm gọn trong một câu thì: **bạn mô tả bằng lời, AI viết code, bạn thử rồi nói tiếp cho AI sửa.**

## Trước khi bắt đầu: hiểu đúng 3 điều

1. **AI không đọc được suy nghĩ của bạn.** Bạn mô tả càng rõ, kết quả càng đúng. Hãy coi AI như một người thợ rất giỏi nhưng mới vào làm ngày đầu.
2. **Lần đầu hiếm khi hoàn hảo.** Vibe code là quá trình *thử, xem, sửa*, lặp lại nhiều lần. Chuyện đó hoàn toàn bình thường.
3. **Bạn không cần hiểu hết code**, nhưng nên hỏi AI "đoạn này làm gì?" khi tò mò. Bạn sẽ học nhanh hơn bạn nghĩ.

## Bước 1: Chọn công cụ

Có nhiều công cụ để vibe code. Với người mới, mình chia làm hai mức:

| Mức | Công cụ | Ưu điểm | Phù hợp khi |
|---|---|---|---|
| **Dễ nhất** | Trò chuyện với AI trên web (Claude, ChatGPT, Gemini) | Không cần cài gì, thấy kết quả ngay trong trình duyệt | Làm công cụ nhỏ gói gọn trong 1 trang |
| **Nâng cao hơn** | Trợ lý lập trình như Claude Code, Cursor | AI làm việc trực tiếp với file trên máy bạn, làm được dự án nhiều file | Bạn đã quen và muốn làm dự án lớn hơn |

**Lời khuyên của mình:** bắt đầu với mức **dễ nhất**. Claude trên web có thể hiển thị luôn trang web hoặc công cụ nó vừa làm, bạn bấm thử được ngay mà không cần cài đặt gì. Khi đã quen, hãy chuyển sang mức tiếp theo.

## Bước 2: Chọn dự án đầu tiên thật nhỏ

Sai lầm phổ biến nhất của người mới là bắt đầu bằng ý tưởng quá lớn: *"làm cho mình một app bán hàng như Shopee"*. Hãy chọn thứ **nhỏ, hữu ích cho chính bạn, và không đụng đến tiền hay dữ liệu người khác**.

Vài gợi ý:

- 🍜 Máy tính **chia tiền ăn nhóm**
- 📅 Trang **đếm ngược** đến sinh nhật, ngày cưới, ngày nghỉ Tết
- ✅ Danh sách **việc cần làm** trong ngày
- 💪 Bảng **theo dõi thói quen** (uống nước, tập thể dục)
- 🎂 **Thiệp chúc mừng** online gửi cho người thân
- 🧮 Công cụ **tính lãi tiết kiệm** đơn giản

Trong bài này, mình sẽ làm mẫu với **máy tính chia tiền ăn nhóm**.

## Bước 3: Viết yêu cầu đầu tiên

Một yêu cầu tốt cho AI nên có 4 phần:

1. **Làm cái gì**
2. **Cho ai dùng, dùng ở đâu** (điện thoại hay máy tính)
3. **Các chức năng cần có**
4. **Giao diện mong muốn**

Đây là yêu cầu mẫu, bạn có thể sao chép và dùng ngay:

```text
Hãy làm cho mình một trang web "Chia tiền ăn nhóm" bằng tiếng Việt.

Người dùng: nhóm bạn đi ăn, một người mở trên điện thoại để tính.

Chức năng:
- Nhập tổng số tiền hóa đơn
- Nhập số người
- Tùy chọn thêm tiền tip theo phần trăm (0%, 5%, 10%)
- Hiện số tiền mỗi người phải trả, làm tròn đến nghìn đồng
- Định dạng tiền kiểu Việt Nam (ví dụ: 125.000 đ)

Giao diện: đơn giản, chữ to, dễ bấm trên điện thoại, màu sắc tươi vui.

Làm tất cả trong một file HTML duy nhất.
```

Gửi đi, chờ vài giây, và bạn sẽ có phiên bản đầu tiên. 🎉

## Bước 4: Thử, xem, rồi sửa

Bây giờ hãy **dùng thử như người dùng thật**. Nhập số liệu, bấm các nút, xem trên điện thoại nếu được. Ghi lại những gì chưa ổn rồi nói với AI, **mỗi lần một hai thay đổi thôi**:

```text
Chạy tốt rồi! Sửa giúp mình 2 chỗ:
1. Nút "Tính" hơi nhỏ, làm to hơn và để màu cam.
2. Thêm chức năng nhập tên từng người, hiện danh sách ai trả bao nhiêu.
```

Một vài câu lệnh "thần chú" mình hay dùng:

- *"Giải thích cho mình nghe như người không biết lập trình: đoạn này làm gì?"*
- *"Trước khi sửa, hãy cho mình biết bạn định thay đổi những gì."*
- *"Chỉ sửa đúng phần mình nói, giữ nguyên phần còn lại."*
- *"Có trường hợp nào người dùng nhập sai làm trang bị lỗi không? Xử lý giúp mình."*

## Bước 5: Khi gặp lỗi (và chắc chắn bạn sẽ gặp)

Lỗi là chuyện **hằng ngày** của lập trình, kể cả với người làm 20 năm như mình. Điều quan trọng là biết cách báo lỗi cho AI.

**Cách báo lỗi chưa tốt:**

```text
Nó không chạy.
```

**Cách báo lỗi tốt:**

```text
Khi mình nhập 500000 và 3 người rồi bấm "Tính" thì không có gì xảy ra.
Mình mong đợi nó hiện "Mỗi người: 167.000 đ".
Mình đang dùng Chrome trên điện thoại Android.
```

Công thức rất đơn giản: **mình đã làm gì → mình thấy gì → mình mong đợi gì**.

Nếu có thông báo lỗi màu đỏ hiện lên, hãy **sao chép nguyên văn** và dán cho AI. Bạn cũng có thể chụp màn hình gửi kèm.

**Nếu AI sửa mãi không được** (sau 3–4 lần), đừng cố tiếp. Hãy thử:

- Bảo AI: *"Hãy dừng lại, suy nghĩ lại từ đầu xem nguyên nhân có thể là gì, liệt kê các khả năng."*
- Hoặc mở **cuộc trò chuyện mới**, dán phiên bản code chạy tốt gần nhất, rồi mô tả lại yêu cầu.

## Bước 6: Lưu lại và chia sẻ

Khi đã hài lòng, bạn có thể đưa công cụ lên mạng để gửi link cho bạn bè, **hoàn toàn miễn phí**. Dưới đây là cách làm trên máy tính. Nếu bạn chỉ có điện thoại, xem phần [Làm hoàn toàn trên điện thoại](#lam-hoan-toan-tren-dien-thoai) ngay bên dưới.

1. Sao chép toàn bộ code AI viết, lưu thành file tên `index.html` trên máy (dùng Notepad cũng được, nhớ chọn lưu kiểu "All files").
2. Tạo một thư mục, bỏ file `index.html` vào.
3. Vào **Netlify Drop** (app.netlify.com/drop), kéo thả thư mục vào trang.
4. Vài giây sau bạn sẽ có một đường link. Gửi cho bạn bè thôi!

Không biết làm bước nào, cứ hỏi AI: *"Hướng dẫn mình từng bước đưa file này lên mạng, mình dùng Windows và không biết gì về kỹ thuật."*

## Làm hoàn toàn trên điện thoại

Không có máy tính? Bạn vẫn vibe code được. Với những công cụ nhỏ như máy chia tiền ăn nhóm, chỉ cần điện thoại là đủ.

### 1. Cài ứng dụng AI

Tải ứng dụng **Claude**, **ChatGPT** hoặc **Gemini** từ App Store (iPhone) hay Google Play (Android), rồi đăng nhập. Bạn cũng có thể dùng ngay trên trình duyệt điện thoại mà không cần cài.

Mình khuyên dùng **Claude** cho người mới vì nó hiển thị luôn trang web vừa làm ngay trong cuộc trò chuyện, bạn bấm thử được liền.

### 2. Nói thay vì gõ

Gõ một yêu cầu dài trên điện thoại khá mệt. Hãy dùng **nhập bằng giọng nói**:

- Bấm biểu tượng **micro 🎤** trên bàn phím (có sẵn trên cả iPhone và Android), rồi nói tiếng Việt bình thường.
- Nói chậm, rõ, từng ý một. Nói xong thì đọc lại và sửa vài chữ sai trước khi gửi.
- Mẹo: lưu sẵn **yêu cầu mẫu** ở Bước 3 vào ứng dụng Ghi chú, lần sau chỉ cần sao chép và sửa vài chỗ.

### 3. Thử ngay trên điện thoại

Đây là lợi thế lớn: bạn đang thử **đúng trên thiết bị mà bạn bè sẽ dùng**. Khi AI làm xong, mở phần xem trước và kiểm tra:

- Chữ có đủ to để đọc không?
- Nút có dễ bấm bằng ngón tay không?
- Khi nhập số, bàn phím có hiện **bàn phím số** không?
- Xoay ngang điện thoại thì giao diện có bị vỡ không?

Thấy chỗ nào chưa ổn thì **chụp màn hình** gửi cho AI kèm mô tả, ví dụ: *"Trong ảnh này, nút Tính bị che mất một nửa trên điện thoại của mình, sửa giúp mình."* AI đọc được ảnh chụp màn hình và hiểu vấn đề nhanh hơn nhiều so với chỉ mô tả bằng chữ.

### 4. Chia sẻ link cho bạn bè

Đây là cách đơn giản nhất trên điện thoại: **chia sẻ thẳng từ ứng dụng AI**.

- Với Claude, ở phần xem trước của trang web vừa làm, hãy tìm nút **chia sẻ** hoặc **Publish** để tạo một đường link công khai. Ai có link đều mở và dùng được, không cần tài khoản.
- Các ứng dụng AI khác cũng thường có chức năng tương tự. Giao diện mỗi ứng dụng thay đổi theo phiên bản, nên nếu không tìm thấy nút, cứ hỏi thẳng AI: *"Làm sao để mình chia sẻ trang này cho bạn bè bằng một đường link?"*

Gửi link qua Zalo, Messenger là bạn bè dùng được ngay.

> 💡 **Lưu ý:** link chia sẻ kiểu này phù hợp với công cụ nhỏ, dùng cho vui hoặc cho nhóm bạn. Khi muốn có trang web riêng, tên miền riêng, bạn nên chuyển sang làm trên máy tính theo cách ở Bước 6.

### Những hạn chế khi làm trên điện thoại

Nói thật để bạn chuẩn bị tinh thần:

- **Màn hình nhỏ**, khó xem code dài. Nhưng với người mới thì cũng không cần xem code nhiều.
- **Khó làm dự án nhiều file**. Điện thoại hợp nhất với công cụ gói gọn trong một trang.
- **Sao chép đoạn code dài dễ bị sót**. Nếu cần lưu code, hãy bấm nút **sao chép (copy)** mà ứng dụng AI cung cấp thay vì bôi đen bằng tay.

Lời khuyên của mình: **bắt đầu trên điện thoại cũng hoàn toàn ổn**. Khi bạn thấy hứng thú và muốn làm thứ lớn hơn, lúc đó hãy chuyển sang máy tính.

## Những điều tuyệt đối nên tránh

Vibe code rất vui, nhưng có vài nguyên tắc an toàn bạn nên nhớ:

- ❌ **Không dán mật khẩu, mã OTP, số tài khoản, API key** vào khung chat với AI.
- ❌ **Không dùng công cụ tự làm để thu thập thông tin cá nhân** của người khác (số điện thoại, CCCD…) khi chưa hiểu cách bảo vệ dữ liệu.
- ❌ **Không làm chức năng thanh toán, chuyển tiền** khi mới bắt đầu.
- ✅ **Lưu lại các phiên bản chạy tốt** (ví dụ `index-v1.html`, `index-v2.html`) để lỡ hỏng còn quay lại.
- ✅ Khi làm thứ cho **nhiều người dùng thật**, hãy nhờ người có kinh nghiệm xem qua.

## Từ điển nhỏ cho người mới

Trong lúc vibe code, AI sẽ hay nhắc đến những từ này:

| Thuật ngữ | Nghĩa đơn giản |
|---|---|
| **HTML** | Khung xương của trang web: chữ, nút, ô nhập |
| **CSS** | Quần áo của trang web: màu sắc, kích thước, bố cục |
| **JavaScript (JS)** | Bộ não của trang web: tính toán, xử lý khi bấm nút |
| **Bug** | Lỗi, chỗ chạy không đúng ý |
| **Debug** | Tìm và sửa lỗi |
| **Prompt** | Câu lệnh, yêu cầu bạn gửi cho AI |
| **Deploy** | Đưa lên mạng cho người khác truy cập |
| **Responsive** | Hiển thị đẹp trên cả điện thoại lẫn máy tính |

## Bước tiếp theo

Khi đã làm xong dự án đầu tiên, bạn có thể:

1. **Làm thêm 2–3 công cụ nhỏ khác** trong danh sách gợi ý ở trên. Mỗi lần bạn sẽ mô tả giỏi hơn.
2. **Học cách viết prompt tốt hơn** qua [khóa học AI miễn phí](./#khoa-hoc) trên trang.
3. Khi muốn làm dự án lớn hơn, nhiều file hơn, hãy thử **Claude Code**. Mình có bài [Claude Code 101](post.html?p=claude-code-101) hướng dẫn từ đầu.

Mình đã hướng dẫn cho nhiều người thân, bạn bè chưa từng biết lập trình, và điều mình thấy rõ nhất là: **rào cản lớn nhất không phải kỹ thuật, mà là dám bắt đầu**. Hãy mở AI lên và thử yêu cầu đầu tiên ngay hôm nay.

Làm xong công cụ đầu tiên rồi? Gửi link cho mình qua phần [Liên hệ](./#lien-he) nhé, mình rất muốn xem bạn làm được gì! 🚀
