Claude Code là trợ lý lập trình AI của Anthropic. Khác với việc trò chuyện với AI trên web rồi sao chép code về, Claude Code **làm việc trực tiếp trong dự án của bạn**: đọc hiểu toàn bộ mã nguồn, sửa nhiều file cùng lúc, chạy lệnh, chạy test, thậm chí commit Git, tất cả chỉ bằng cách bạn mô tả bằng lời.

Bài này hướng dẫn từ lúc cài đặt đến quy trình làm việc mình dùng hằng ngày. Nếu bạn chưa biết nên chọn công cụ nào, hãy đọc bài [So sánh các công cụ vibe code](post.html?p=so-sanh-cong-cu-vibe-code) trước.

> ⚠️ *Claude Code cập nhật rất thường xuyên. Bài viết cập nhật đến tháng 10/2026, nếu có chỗ khác với những gì bạn thấy, hãy xem [tài liệu chính thức](https://code.claude.com/docs).*

## Claude Code làm được gì?

- **Đọc và giải thích dự án**: nhận một dự án lạ, hỏi "dự án này làm gì?" là có câu trả lời sau vài giây
- **Viết tính năng mới**: mô tả tính năng, Claude tự tìm đúng file, sửa, rồi kiểm tra lại
- **Sửa lỗi**: dán thông báo lỗi, Claude tìm nguyên nhân và sửa
- **Viết test, viết tài liệu, tái cấu trúc code**
- **Làm việc với Git**: xem thay đổi, viết commit, tạo nhánh, giải quyết xung đột
- **Kết nối công cụ bên ngoài** qua MCP: cơ sở dữ liệu, GitHub, trình duyệt, Figma…

## Chuẩn bị trước khi cài

Bạn cần:

1. **Tài khoản Claude trả phí** (Pro, Max, Team hoặc Enterprise), hoặc tài khoản Claude Console dùng API trả trước. **Gói miễn phí của claude.ai không dùng được Claude Code.**
2. Máy tính chạy **Windows 10 (bản 1809 trở lên), macOS 13 trở lên**, hoặc Linux phổ biến (Ubuntu, Debian…), RAM từ 4 GB.
3. Kết nối Internet.
4. (Nên có) **Git**: để lưu lịch sử thay đổi, lỡ hỏng còn quay lại. Trên Windows, nên cài [Git for Windows](https://git-scm.com/downloads/win).

### Không quen dùng terminal?

Claude Code có **ứng dụng desktop** cho Windows và macOS (tải tại [claude.com/download](https://claude.com/download), dùng tab **Code**). Giao diện đồ họa, không cần gõ lệnh, rất hợp với người mới. Ngoài ra còn có tiện ích cho **VS Code** và **JetBrains**, và bản chạy trên web tại claude.ai/code.

Phần còn lại của bài hướng dẫn bản **terminal**, nhưng các khái niệm và cách làm việc dùng chung cho mọi phiên bản.

## Cài đặt

Mở terminal và chạy lệnh tương ứng với máy của bạn.

**Windows (PowerShell):**

```powershell
irm https://claude.ai/install.ps1 | iex
```

**macOS, Linux:**

```bash
curl -fsSL https://claude.ai/install.sh | bash
```

Cài xong, **mở một cửa sổ terminal mới** và kiểm tra:

```bash
claude --version
```

Thấy hiện số phiên bản là thành công. Bản cài theo cách này sẽ **tự động cập nhật**.

> 💡 Mẹo trên Windows: dòng lệnh bắt đầu bằng `PS C:\` là PowerShell. Nếu không có chữ `PS` thì bạn đang ở CMD, hãy mở PowerShell để chạy lệnh trên.

Nếu gặp lỗi, chạy `claude doctor` để Claude Code tự kiểm tra và gợi ý cách sửa.

## Phiên làm việc đầu tiên

Di chuyển vào thư mục dự án và khởi động:

```bash
cd duong-dan/den/du-an
claude
```

Lần đầu chạy, Claude Code sẽ mở trình duyệt để bạn **đăng nhập**. Đăng nhập xong là dùng được, những lần sau không cần đăng nhập lại.

Hãy bắt đầu bằng vài câu hỏi để Claude làm quen với dự án:

```text
Dự án này làm gì? Giải thích cấu trúc thư mục cho mình.
```

```text
Dự án dùng những công nghệ gì? Muốn chạy thử thì làm thế nào?
```

Bạn **không cần** chỉ cho Claude file nào, nó tự đọc những gì cần thiết. Rồi thử một thay đổi nhỏ:

```text
Thêm nút "Lên đầu trang" ở góc dưới bên phải trang chủ.
```

Claude sẽ tìm file, sửa, và cho bạn xem thay đổi.

## Các khái niệm quan trọng

### Chế độ phân quyền

Claude Code có nhiều **chế độ phân quyền** quyết định khi nào Claude được tự sửa file, chạy lệnh và khi nào phải hỏi bạn trước. Bấm **Shift + Tab** để chuyển qua lại giữa các chế độ. Đáng nhớ nhất:

| Chế độ | Ý nghĩa | Khi nào dùng |
|---|---|---|
| **Hỏi trước khi sửa** | Mỗi lần sửa file, chạy lệnh đều hỏi bạn | Khi mới làm quen, muốn xem kỹ từng bước |
| **Tự động (auto)** | Một bộ lọc an toàn tự duyệt các thao tác thông thường, việc rủi ro mới hỏi bạn | Làm việc hằng ngày khi đã quen |
| **Lập kế hoạch (plan)** | Claude chỉ đọc và đề xuất kế hoạch, **không sửa gì** | Trước mọi việc lớn |

Dù ở chế độ nào, hãy **đọc kỹ trước khi đồng ý** những lệnh có thể xóa dữ liệu hoặc tác động ra bên ngoài (đẩy code lên server, gửi dữ liệu…).

### CLAUDE.md: bộ nhớ của dự án

`CLAUDE.md` là file ghi chú đặt ở thư mục gốc dự án. Claude đọc file này **mỗi lần bắt đầu phiên làm việc**, nên đây là chỗ ghi những điều bạn không muốn phải nhắc đi nhắc lại:

- Cách chạy dự án, cách chạy test
- Quy ước đặt tên, phong cách code
- Những việc **không được làm** (ví dụ: "không sửa thư mục `legacy/`")

Gõ `/init` để Claude tự đọc dự án và tạo file `CLAUDE.md` ban đầu, sau đó bạn chỉnh thêm. Giữ file này **ngắn gọn và cụ thể**: chỉ ghi những gì Claude không tự suy ra được từ code.

### Nhắc đến file bằng @

Gõ `@` rồi tên file để chỉ thẳng cho Claude file cần xem:

```text
Trong @src/components/LoginForm.tsx, nút đăng nhập không bị vô hiệu hóa khi đang gửi. Sửa giúp mình.
```

### Slash command: các lệnh bắt đầu bằng `/`

Gõ `/` để xem danh sách đầy đủ. Những lệnh mình dùng nhiều nhất:

| Lệnh | Tác dụng |
|---|---|
| `/init` | Tạo file `CLAUDE.md` cho dự án |
| `/clear` | Xóa cuộc trò chuyện, bắt đầu việc mới |
| `/compact` | Tóm tắt cuộc trò chuyện dài để giải phóng bộ nhớ |
| `/resume` | Mở lại một phiên làm việc cũ |
| `/rewind` | Quay lại thời điểm trước đó, hoàn tác thay đổi của Claude |
| `/model` | Đổi mô hình AI |
| `/help` | Xem trợ giúp |

Từ terminal, bạn cũng có thể dùng `claude -c` để **tiếp tục phiên gần nhất** trong thư mục hiện tại.

### MCP: kết nối Claude với công cụ khác

MCP (Model Context Protocol) cho phép Claude dùng thêm công cụ bên ngoài: đọc cơ sở dữ liệu, thao tác GitHub, điều khiển trình duyệt, đọc thiết kế Figma… Mỗi kết nối gọi là một **MCP server**, thêm bằng lệnh `claude mcp add` hoặc cấu hình trong file `.mcp.json` của dự án. Gõ `/mcp` để xem các kết nối đang có.

Lời khuyên: **chỉ cài MCP server từ nguồn đáng tin cậy**, vì chúng có quyền truy cập dữ liệu của bạn.

### Skills, hooks, subagents

Khi đã quen, bạn có thể tìm hiểu thêm:

- **Skills**: đóng gói quy trình hay dùng (ví dụ "quy trình viết bài blog", "checklist review code") để Claude gọi lại khi cần.
- **Hooks**: tự động chạy lệnh tại những thời điểm nhất định, ví dụ tự format code mỗi khi Claude sửa file.
- **Subagents**: các "trợ lý con" chuyên một việc, giúp xử lý việc lớn mà không làm rối cuộc trò chuyện chính.

Người mới chưa cần đụng đến, nhưng nên biết là có.

## Quy trình làm việc mình dùng hằng ngày

Đây là quy trình rút ra sau thời gian dài dùng Claude Code cho công việc thật:

1. **Khám phá trước**: với việc lạ, hỏi Claude giải thích phần code liên quan trước khi sửa.
2. **Lập kế hoạch**: chuyển sang **chế độ plan** (Shift + Tab), mô tả mục tiêu, để Claude đề xuất kế hoạch. Đọc kỹ, sửa chỗ chưa đúng.
3. **Thực hiện**: duyệt kế hoạch rồi mới cho Claude làm.
4. **Kiểm tra**: yêu cầu Claude chạy test, hoặc tự chạy thử. Đọc lại các thay đổi.
5. **Commit**: bảo Claude commit với mô tả rõ ràng. Mỗi việc một commit.
6. **Bắt đầu lại sạch sẽ**: xong việc thì `/clear` trước khi sang việc mới.

Một yêu cầu mẫu ở bước 2:

```text
Mình muốn thêm chức năng tìm kiếm bài viết theo từ khóa ở trang chủ.
- Tìm theo tiêu đề và mô tả, không phân biệt dấu tiếng Việt
- Kết quả lọc ngay khi gõ
- Không dùng thêm thư viện ngoài
Hãy đọc code hiện tại và đề xuất kế hoạch. Chưa sửa gì vội.
```

## Mẹo để Claude làm việc hiệu quả hơn

- **Cụ thể thay vì chung chung.** Thay vì *"sửa lỗi đăng nhập"*, hãy viết *"sửa lỗi người dùng thấy màn hình trắng sau khi nhập sai mật khẩu"*.
- **Cho Claude cách tự kiểm tra.** *"Viết test trước, chạy cho đến khi test qua"* cho kết quả tốt hơn nhiều so với chỉ *"viết chức năng này"*.
- **Chia nhỏ việc lớn.** Liệt kê các bước 1, 2, 3 để Claude làm lần lượt.
- **Dán ảnh chụp màn hình.** Claude đọc được ảnh, rất hữu ích khi sửa giao diện.
- **Dừng sớm khi Claude đi sai hướng.** Bấm **Esc** để dừng giữa chừng, nói lại cho rõ. Đừng để nó đi tiếp rồi mới sửa.
- **Hoàn tác khi cần.** Gõ `/rewind` để quay về thời điểm trước khi Claude sửa.
- **Cuộc trò chuyện quá dài thì làm mới.** Khi Claude bắt đầu lặp lỗi hoặc quên yêu cầu, hãy `/clear` và mô tả lại gọn gàng.

## Checklist an toàn

| Việc cần làm | Lý do |
|---|---|
| Không dán mật khẩu, API key vào khung chat | Tránh lộ thông tin |
| Không ghi API key thẳng vào code, dùng file `.env` và thêm vào `.gitignore` | Tránh lộ khi đưa code lên GitHub |
| Đọc kỹ trước khi đồng ý lệnh xóa, lệnh đẩy code lên server | Tránh mất dữ liệu |
| Dùng Git và commit thường xuyên | Luôn có đường quay lại |
| Chỉ cài MCP server, plugin từ nguồn tin cậy | Chúng có quyền truy cập dữ liệu của bạn |
| Review code trước khi đưa lên production | Code chạy được chưa chắc đã đúng và an toàn |

## Bước tiếp theo

- Thử dùng Claude Code cho **một dự án nhỏ của chính bạn** trong tuần này: một trang web cá nhân, một script tự động hóa việc lặp đi lặp lại.
- Đọc bài [Vibe code từ góc nhìn lập trình viên 20 năm](post.html?p=vibe-code-goc-nhin-lap-trinh-vien-20-nam) để hiểu thêm tư duy làm việc cùng AI.
- Học miễn phí thêm tại [Claude Academy](https://academy.claude.com/) và [tài liệu chính thức](https://code.claude.com/docs).

Bạn gặp khó khăn khi cài đặt hay sử dụng? Gửi câu hỏi cho mình qua phần [Liên hệ](./#lien-he) nhé!
