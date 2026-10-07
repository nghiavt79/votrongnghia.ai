using System.Threading.Channels;
using VoTrongNghia.Models;

namespace VoTrongNghia.Services;

/// <summary>
/// Hàng đợi gửi email chạy nền. Thư xác nhận và thư báo đơn mới đi qua đây chứ không gửi ngay
/// trong lúc người học bấm "Gửi đơn": Gmail chậm hay lỗi thì người học vẫn thấy trang cảm ơn
/// ngay, và đơn vẫn lưu — email là phần thêm, không được làm hỏng việc chính.
///
/// <para>Kết quả từng thư ghi vào lịch sử của đơn (<see cref="Registration.Emails"/>), nên
/// gửi lỗi thì người duyệt thấy ở trang chi tiết đơn và gửi lại được.</para>
///
/// <para>Hàng đợi nằm trong bộ nhớ: app pool khởi động lại đúng lúc còn thư chưa gửi là mất thư
/// đó. Chấp nhận được — đơn vẫn còn, lịch sử đơn không có dòng gửi thì người duyệt biết.</para>
/// </summary>
public sealed class EmailQueue : BackgroundService
{
    private sealed record Item(EmailMessage Message, string Kind, string? RegistrationCode);

    // Có giới hạn để một đợt gửi rác không ăn hết bộ nhớ; đầy thì bỏ thư mới, ghi log.
    private readonly Channel<Item> _channel = Channel.CreateBounded<Item>(new BoundedChannelOptions(500)
    {
        FullMode = BoundedChannelFullMode.DropWrite
    });

    private readonly EmailSender _sender;
    private readonly IServiceProvider _services;
    private readonly ILogger<EmailQueue> _logger;

    public EmailQueue(EmailSender sender, IServiceProvider services, ILogger<EmailQueue> logger)
    {
        _sender = sender;
        // Lấy RegistrationStore lúc cần chứ không nhận qua hàm dựng: RegistrationStore cũng
        // dùng hàng đợi này, nhận qua hàm dựng cả hai phía là vòng lặp phụ thuộc.
        _services = services;
        _logger = logger;
    }

    public void Enqueue(EmailMessage message, string kind, string? registrationCode)
    {
        if (!_channel.Writer.TryWrite(new Item(message, kind, registrationCode)))
        {
            _logger.LogError("Hàng đợi email đầy, bỏ thư \"{Subject}\" tới {To}", message.Subject, message.To);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var item in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            string? error;

            try
            {
                error = await _sender.SendAsync(item.Message, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                // Không để một thư lỗi lạ làm chết cả hàng đợi.
                _logger.LogError(ex, "Lỗi lạ khi gửi email tới {To}", item.Message.To);
                error = "Gửi email lỗi: " + ex.Message;
            }

            if (item.RegistrationCode is not null)
            {
                await _services.GetRequiredService<RegistrationStore>().LogEmailAsync(item.RegistrationCode, new EmailLogEntry
                {
                    At = SiteTime.Now,
                    Kind = item.Kind,
                    To = item.Message.To,
                    Subject = item.Message.Subject,
                    Error = error
                }, stoppingToken);
            }

            // Giãn nhịp một chút: Gmail không ưa một tài khoản bắn thư dồn dập.
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }
}
