using System.Text;
using VoTrongNghia.Models;

namespace VoTrongNghia.Services;

/// <summary>
/// Nội dung các email gửi người học và người quản trị. Văn xưng "mình" – "bạn" như trên site.
///
/// <para>Mẫu không có chỗ trống kiểu <c>[điền link]</c>: thư báo kết quả gửi tự động khi đổi
/// trạng thái, không ai đọc lại trước khi đi. Thiếu thông tin (chưa có link phòng học) thì
/// câu chữ tự đổi cho đúng.</para>
/// </summary>
public static class EmailTemplates
{
    /// <summary>Tên gọi trong lời chào: chữ cuối của họ tên ("Nguyễn Văn An" → "An").</summary>
    public static string FirstName(string fullName) =>
        fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? fullName;

    /// <summary>Thư gửi người đăng ký ngay khi đơn được lưu.</summary>
    public static EmailMessage Receipt(Registration item, string siteName, string baseUrl, string? replyTo)
    {
        var body = new StringBuilder()
            .AppendLine($"Chào {FirstName(item.Name)},")
            .AppendLine()
            .AppendLine($"Mình đã nhận được đơn đăng ký lớp học online của bạn, mã đơn {item.Code}.")
            .AppendLine(item.SessionLabel.Length > 0 ? $"Buổi bạn chọn: {item.SessionLabel}." : "Lớp chưa có lịch cụ thể — mình sẽ báo ngay khi có.")
            .AppendLine()
            .AppendLine("Lớp miễn phí nhưng số chỗ có hạn, nên mình đọc kỹ từng đơn và sẽ báo kết quả qua email này trong vòng 3–5 ngày.")
            .AppendLine()
            .AppendLine("Trong lúc chờ, bạn có thể học tiếp các khóa miễn phí:")
            .AppendLine($"{baseUrl}/khoa-hoc")
            .AppendLine()
            .AppendLine("Có gì cần hỏi, bạn cứ trả lời thư này.")
            .AppendLine()
            .AppendLine(siteName)
            .AppendLine(baseUrl);

        return new EmailMessage(item.Email, $"[{siteName}] Đã nhận đơn đăng ký {item.Code}", body.ToString(), replyTo);
    }

    /// <summary>Thư báo người quản trị có đơn mới, kèm đủ câu trả lời để đọc ngay trên điện thoại.</summary>
    public static EmailMessage OwnerNotice(Registration item, string ownerAddress, string baseUrl)
    {
        var body = new StringBuilder()
            .AppendLine($"Đơn mới {item.Code} — {item.Name}")
            .AppendLine()
            .AppendLine($"Buổi: {(item.SessionLabel.Length > 0 ? item.SessionLabel : "chưa có lịch")}")
            .AppendLine($"Nghề nghiệp: {item.Job}")
            .AppendLine($"Mức dùng AI: {item.AiLevel}")
            .AppendLine($"Thời gian tự học: {item.HoursLabel} mỗi tuần")
            .AppendLine($"Email: {item.Email} · Điện thoại / Zalo: {item.Phone}")
            .AppendLine()
            .AppendLine("Mục tiêu:")
            .AppendLine(item.Goal)
            .AppendLine()
            .AppendLine("Muốn làm ra:")
            .AppendLine(item.Project)
            .AppendLine()
            .AppendLine("Duyệt đơn:")
            .AppendLine($"{baseUrl}/cms/dang-ky/{item.Code}");

        // Trả lời thư này là trả lời thẳng người đăng ký.
        return new EmailMessage(ownerAddress, $"Đơn đăng ký mới: {item.Name} ({item.Code})", body.ToString(), item.Email);
    }

    /// <summary>
    /// Thư báo kết quả theo trạng thái hiện tại của đơn. Null khi trạng thái là "Mới" (chưa có
    /// gì để báo).
    ///
    /// <para>Thư duyệt có thêm phần trang học viên: <paramref name="loginUrl"/> là link đăng nhập dùng
    /// một lần (hạn 7 ngày); null (bản nháp, thư soạn bằng ứng dụng email) thì chỉ dẫn tới trang đăng
    /// nhập — bản nháp không được mang link thật, mở trang chi tiết đơn không được sinh mã.</para>
    /// </summary>
    public static EmailMessage? Result(Registration item, LiveSession? session, string siteName, string baseUrl, string? replyTo, string? loginUrl = null)
    {
        var first = FirstName(item.Name);
        var body = new StringBuilder().AppendLine($"Chào {first},").AppendLine();
        string subject;

        switch (item.Status)
        {
            case Registration.StatusApproved:
                subject = $"[{siteName}] Chúc mừng {first} — bạn đã được nhận vào lớp online";
                body.AppendLine("Cảm ơn bạn đã đăng ký và chia sẻ rất cụ thể về mục tiêu của mình. Mình rất vui báo bạn đã được nhận vào lớp.")
                    .AppendLine();

                if (session is not null)
                {
                    body.AppendLine($"Buổi học: {session.Title}")
                        .AppendLine($"Thời gian: {session.Date:dd/MM/yyyy}, {session.Time}")
                        .AppendLine($"Hình thức: {session.Platform}");
                    body.AppendLine(session.MeetingLink.Length > 0
                        ? $"Link vào lớp: {session.MeetingLink}"
                        : "Link vào lớp mình sẽ gửi riêng trước buổi học.");
                    body.AppendLine("Link chỉ dành cho bạn — đừng chia sẻ công khai giúp mình nhé.");
                }
                else
                {
                    body.AppendLine("Mình sẽ gửi lịch và link vào lớp ngay khi chốt buổi học đầu tiên.");
                }

                body.AppendLine()
                    .AppendLine("Bạn có trang học viên riêng — xem lịch học, link phòng học và tiến độ các khóa, học trên máy nào cũng giữ được tiến độ:");

                if (loginUrl is not null)
                {
                    body.AppendLine(loginUrl)
                        .AppendLine($"Link trên đăng nhập luôn, dùng được một lần trong {LoginLinkService.LongLifetime.Days} ngày. Lần sau vào {baseUrl}/hoc-vien/dang-nhap và nhập email này để nhận link mới.");
                }
                else
                {
                    body.AppendLine($"{baseUrl}/hoc-vien/dang-nhap — nhập email này, mình gửi link đăng nhập (không cần mật khẩu).");
                }

                body.AppendLine()
                    .AppendLine("Nhắc lại cam kết của lớp: tham gia đầy đủ và đúng giờ, làm bài tập sau mỗi buổi, báo trước nếu vắng.")
                    .AppendLine()
                    .AppendLine("Hẹn gặp bạn ở lớp!");
                break;

            case Registration.StatusWaiting:
                subject = $"[{siteName}] Đơn đăng ký của {first} — hẹn bạn đợt sau";
                body.AppendLine("Cảm ơn bạn đã đăng ký. Đợt này lớp đã đủ chỗ nên mình xin giữ đơn của bạn cho đợt kế tiếp — mình sẽ báo ngay khi có lịch.")
                    .AppendLine()
                    .AppendLine("Trong lúc chờ, bạn cứ học tiếp các khóa miễn phí trên trang nhé:")
                    .AppendLine($"{baseUrl}/khoa-hoc");
                break;

            case Registration.StatusRejected:
                subject = $"[{siteName}] Về đơn đăng ký lớp online của {first}";
                body.AppendLine("Cảm ơn bạn đã quan tâm tới lớp học. Lần này mình chưa thể nhận bạn vào lớp — số chỗ có hạn nên mình ưu tiên những đơn có mục tiêu thật cụ thể và thời gian tự học phù hợp.")
                    .AppendLine()
                    .AppendLine("Bạn hoàn toàn có thể đăng ký lại ở đợt sau. Trong lúc đó, các khóa miễn phí trên trang vẫn luôn mở cho bạn:")
                    .AppendLine($"{baseUrl}/khoa-hoc");
                break;

            default:
                return null;
        }

        body.AppendLine().AppendLine(siteName).AppendLine(baseUrl);

        return new EmailMessage(item.Email, subject, body.ToString(), replyTo);
    }

    /// <summary>Thư mang link đăng nhập trang học viên.</summary>
    public static EmailMessage LoginLink(Learner learner, string loginUrl, TimeSpan lifetime, string siteName, string baseUrl, string? replyTo)
    {
        var expires = lifetime.TotalDays >= 1 ? $"{lifetime.TotalDays:0} ngày" : $"{lifetime.TotalMinutes:0} phút";

        var body = new StringBuilder()
            .AppendLine($"Chào {FirstName(learner.Name)},")
            .AppendLine()
            .AppendLine("Bấm link dưới đây để vào trang học viên:")
            .AppendLine(loginUrl)
            .AppendLine()
            .AppendLine($"Link dùng được một lần và hết hạn sau {expires}. Cần vào lại thì xin link mới ở {baseUrl}/hoc-vien/dang-nhap.")
            .AppendLine()
            .AppendLine("Nếu bạn không yêu cầu đăng nhập, cứ bỏ qua thư này — không ai vào được tài khoản của bạn nếu không có link trên.")
            .AppendLine()
            .AppendLine(siteName)
            .AppendLine(baseUrl);

        return new EmailMessage(learner.Email, $"[{siteName}] Link đăng nhập trang học viên", body.ToString(), replyTo);
    }

    /// <summary>Thư nhắc học viên lâu không học — chỉ là bản nháp điền sẵn ở /cms/hoc-vien, người quản trị sửa rồi mới gửi.</summary>
    public static EmailMessage Reminder(Learner learner, string nextLesson, string siteName, string baseUrl, string? replyTo)
    {
        var body = new StringBuilder()
            .AppendLine($"Chào {FirstName(learner.Name)},")
            .AppendLine()
            .AppendLine("Mấy hôm nay mình chưa thấy bạn vào học. Bạn có gặp khó ở bài nào không? Cứ trả lời thư này, mình gỡ cùng bạn.")
            .AppendLine();

        if (nextLesson.Length > 0)
        {
            body.AppendLine("Bài tiếp theo của bạn:").AppendLine(nextLesson).AppendLine();
        }

        body.AppendLine("Mỗi ngày 15 phút là đủ để giữ nhịp. Hẹn gặp bạn ở lớp!")
            .AppendLine()
            .AppendLine(siteName)
            .AppendLine(baseUrl);

        return new EmailMessage(learner.Email, $"[{siteName}] {FirstName(learner.Name)} ơi, học tiếp nhé", body.ToString(), replyTo);
    }
}
