Mình viết code đã 20 năm. Mình đã đi qua đủ kiểu thay đổi của nghề: framework mới, ngôn ngữ mới, chuyển từ server vật lý lên cloud. Nhưng chưa thay đổi nào làm cách mình làm việc khác đi nhanh như **vibe coding**.

Bài này là góc nhìn thật của mình: vibe code là gì, nó làm tốt việc gì, chỗ nào dễ "toang", và mình đang dùng nó trong công việc hằng ngày ra sao.

## Vibe code là gì?

Cụm từ "vibe coding" được Andrej Karpathy (đồng sáng lập OpenAI, cựu giám đốc AI của Tesla) dùng vào đầu năm 2025. Ý tưởng rất đơn giản:

> Bạn **mô tả bằng lời** thứ mình muốn, AI **viết code**, bạn chạy thử, thấy chưa đúng thì **nói tiếp** cho AI sửa.

Bạn không cần gõ từng dòng nữa. Việc chính của bạn chuyển sang **mô tả, kiểm tra và định hướng**.

Mình bắt đầu thử từ những ngày cộng đồng còn rất nhỏ, khi chưa ai chắc nó có dùng được cho việc thật hay không. Giờ thì nó đã là một phần trong công việc hằng ngày của mình.

## Phản ứng đầu tiên: hoài nghi

Ai làm nghề lâu năm cũng sẽ có phản xạ giống nhau khi nghe "AI viết code thay bạn":

- *"Code do máy viết thì ai chịu trách nhiệm?"*
- *"Chắc chỉ làm được mấy cái demo đơn giản."*
- *"Rồi lập trình viên sẽ mất việc à?"*

Sau một thời gian dùng thật, câu trả lời của mình là: **AI không thay thế kinh nghiệm, AI nhân kinh nghiệm lên.** Người hiểu hệ thống càng sâu thì càng khai thác AI được nhiều.

## 20 năm kinh nghiệm có còn giá trị không?

Còn, và theo mình còn **quan trọng hơn trước**. Chỉ có điều nó được dùng ở chỗ khác.

| Trước đây | Khi vibe code |
|---|---|
| Thời gian chủ yếu dành để gõ code | Thời gian chủ yếu dành để **suy nghĩ và mô tả** |
| Nhớ cú pháp, nhớ API | Biết **hỏi đúng** và nhận ra câu trả lời sai |
| Tự viết từng hàm | **Review** code AI viết, như review code của đồng nghiệp |
| Kiến trúc nằm trong đầu | Kiến trúc phải **viết ra rõ ràng** để AI làm theo |

AI viết code rất nhanh, nhưng nó **không biết** dự án của bạn có lịch sử gì, khách hàng thực sự cần gì, chỗ nào từng gây lỗi trên production. Những thứ đó là kinh nghiệm, và đó chính là thứ giúp bạn điều khiển AI đi đúng hướng.

Nói vui thì: trước đây mình là **thợ xây**, giờ mình là **kiến trúc sư kiêm giám sát công trình**.

## Vibe code làm tốt việc gì?

Theo trải nghiệm của mình, AI làm rất tốt:

- **Dựng khung dự án nhanh**: trang web, API, công cụ nội bộ. Ý tưởng buổi sáng, buổi chiều đã có bản chạy được.
- **Việc lặp đi lặp lại**: viết form, CRUD, chuyển đổi dữ liệu, viết test.
- **Đọc hiểu code lạ**: nhận một dự án cũ không có tài liệu, hỏi AI giải thích luồng xử lý nhanh hơn nhiều so với tự lần.
- **Làm việc với công nghệ mình chưa rành**: ngôn ngữ mới, thư viện mới. AI vừa viết vừa giải thích, mình học nhanh hơn hẳn.
- **Script tự động hóa nhỏ**: xử lý file, đổi tên hàng loạt, gom báo cáo. Trước đây hay ngại viết vì "mất công", giờ thì vài phút là xong.

## Chỗ nào dễ "toang"?

Đây là phần mình muốn các bạn đọc kỹ nhất.

### 1. Chấp nhận code mà mình không hiểu

Code chạy được **chưa chắc là code đúng**. AI có thể viết thứ trông rất ổn nhưng sai ở trường hợp biên, xử lý lỗi sơ sài, hoặc "chữa cháy" bằng cách xóa luôn đoạn đang lỗi. Với dự án thật, **mọi dòng code đưa lên production đều phải có người hiểu và chịu trách nhiệm**.

### 2. Bảo mật

AI thường ưu tiên "cho chạy được" hơn là "cho an toàn". Những lỗi mình hay gặp:

- Để lộ API key, mật khẩu ngay trong code
- Không kiểm tra dữ liệu người dùng nhập vào
- Mở quyền truy cập rộng hơn cần thiết

### 3. Dự án phình to, mất kiểm soát

Vibe code rất sướng ở giai đoạn đầu. Nhưng nếu cứ "nói tiếp, sửa tiếp" mà không có cấu trúc, sau vài tuần bạn sẽ có một mớ code mà chính AI cũng bối rối. **Nợ kỹ thuật vẫn là nợ**, chỉ là nó tích lũy nhanh hơn.

### 4. Cuộc trò chuyện quá dài

Khi trao đổi quá lâu trong một phiên, AI bắt đầu "quên" yêu cầu ban đầu hoặc lặp lại lỗi cũ. Mẹo của mình: **mỗi việc một phiên**, xong việc thì bắt đầu phiên mới.

## Quy trình vibe code của mình

Đây là quy trình mình dùng mỗi ngày. Bạn có thể áp dụng ngay.

1. **Mô tả mục tiêu thật rõ.** Ai dùng, dùng để làm gì, thế nào là "xong".
2. **Bắt AI lên kế hoạch trước, chưa cho viết code.** Đọc kế hoạch, sửa chỗ sai, rồi mới cho làm.
3. **Chia nhỏ.** Mỗi lần chỉ một tính năng. Đừng bảo AI "làm cả hệ thống".
4. **Đọc lại thay đổi (diff).** Không cần đọc kỹ từng dòng như ngày xưa, nhưng phải hiểu AI đã sửa những gì.
5. **Chạy thử và kiểm tra.** Tự bấm thử, hoặc nhờ AI viết test.
6. **Commit thường xuyên bằng Git.** Đây là "nút quay lại" của bạn. AI làm hỏng thì quay về bản trước, không mất gì.
7. **Ghi lại quy ước dự án** vào một file (ví dụ `CLAUDE.md` với Claude Code). AI sẽ đọc file này mỗi lần làm việc nên không phải nhắc lại từ đầu.

Một prompt mẫu mình hay dùng ở bước 1 và 2:

```text
Mình muốn thêm chức năng [mô tả chức năng] cho dự án này.
Người dùng là [ai], họ cần [làm gì].
Yêu cầu:
- [yêu cầu 1]
- [yêu cầu 2]
Chưa viết code vội. Hãy đọc dự án, đề xuất kế hoạch từng bước
và chỉ ra những chỗ có thể gặp rủi ro.
```

Chỉ riêng câu *"chưa viết code vội, hãy lên kế hoạch"* đã giúp mình tránh được rất nhiều lần AI đi sai hướng.

## Người không biết lập trình có vibe code được không?

**Được**, và đây là điều làm mình hào hứng nhất. Khi mình hướng dẫn cho bạn bè, người thân, kể cả những người chưa từng viết một dòng code, nhiều người đã tự làm được công cụ nhỏ phục vụ công việc của chính họ.

Nhưng mình luôn dặn mấy điều:

- **Bắt đầu từ việc nhỏ, phục vụ chính bạn**: một trang web cá nhân, một bảng tính tự động, một công cụ tính toán. Đừng bắt đầu bằng một ứng dụng có thanh toán hay dữ liệu khách hàng.
- **Hỏi AI "tại sao"** mỗi khi nó làm gì đó bạn không hiểu. Vibe code là cách học lập trình nhanh nhất mình từng thấy, nếu bạn chịu hỏi.
- **Không dán mật khẩu, API key, thông tin cá nhân** vào khung chat.
- **Khi làm thứ cho người khác dùng** (có đăng nhập, có tiền, có dữ liệu thật), hãy nhờ người có kinh nghiệm xem qua.

👉 Muốn bắt tay vào làm ngay? Xem bài [Hướng dẫn vibe code cho người mới bắt đầu](post.html?p=huong-dan-vibe-code-cho-nguoi-moi).

## Lời khuyên cho anh em lập trình viên

**Nếu bạn đã làm nghề lâu năm:** đừng đứng ngoài. Kinh nghiệm của bạn là lợi thế lớn nhất khi dùng AI, nhưng chỉ khi bạn chịu thay đổi cách làm. Hãy thử một tuần giao hết những việc nhàm chán cho AI, bạn sẽ thấy khác.

**Nếu bạn mới vào nghề:** dùng AI, nhưng **đừng để AI nghĩ thay bạn**. Hãy bắt nó giải thích, tự sửa lại, tự viết lại vài phần. Nền tảng vững vẫn là thứ phân biệt người dùng AI giỏi với người chỉ copy-paste.

## Kết

Sau 20 năm, mình vẫn thích lập trình như ngày đầu, có khi còn thích hơn. Vì giờ khoảng cách từ **ý tưởng** đến **sản phẩm chạy được** đã ngắn hơn bao giờ hết.

Mình làm trang này để chia sẻ lại những gì mình học được, cho anh em trong nghề, và cả những người chưa từng nghĩ mình có thể tự làm ra phần mềm. Nếu bạn muốn bắt đầu, hãy xem [khóa học AI miễn phí](./#khoa-hoc) hoặc bài [Claude Code 101](post.html?p=claude-code-101).

Có câu hỏi hay muốn chia sẻ trải nghiệm vibe code của bạn? Để lại lời nhắn ở phần [Liên hệ](./#lien-he), mình đọc hết.
