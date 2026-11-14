using System.Net;
using System.Net.Mail;

namespace SecondHandMarketplace.Web.Services.Auth;

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default);
}

public sealed class SmtpEmailSender(
    IConfiguration configuration,
    ILogger<SmtpEmailSender> logger,
    IHostEnvironment env) : IEmailSender
{
    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        var section = configuration.GetSection("Email:Smtp");
        var host = section["Host"] ?? "smtp.gmail.com";
        var username = section["Username"];
        var from = section["From"] ?? username;
        var rawPassword = section["Password"];
        // Mật khẩu ứng dụng của Gmail thường được Google hiển thị có dấu cách dạng "abcd efgh ijkl mnop", cần loại bỏ dấu cách
        var password = rawPassword?.Replace(" ", "").Trim();

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(from))
        {
            if (env.IsDevelopment())
            {
                logger.LogWarning("[DEV-MODE] Chưa cấu hình Gmail SMTP (Email:Smtp:Username / Password). Giả lập gửi email thành công tới {To}. Tiêu đề: {Subject}", to, subject);
                return;
            }
            throw new InvalidOperationException("Thiếu cấu hình Email:Smtp:Username hoặc Email:Smtp:Password. Hãy dùng địa chỉ Gmail và Mật khẩu ứng dụng (App Password), không dùng mật khẩu đăng nhập Gmail.");
        }

        using var message = new MailMessage
        {
            From = new MailAddress(from.Trim(), section["DisplayName"] ?? "Re:Market"),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(to.Trim());

        using var client = new SmtpClient(host, int.TryParse(section["Port"], out var port) ? port : 587)
        {
            EnableSsl = bool.TryParse(section["EnableSsl"], out var ssl) ? ssl : true,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(username.Trim(), password),
            Timeout = 15000 // 15 giây tránh treo request nếu kết nối bị firewall chặn
        };

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        linkedCts.CancelAfter(TimeSpan.FromSeconds(10));

        try
        {
            await client.SendMailAsync(message, linkedCts.Token);
            logger.LogInformation("Đã gửi email thành công qua Gmail SMTP tới {To}", to);
        }
        catch (Exception ex)
        {
            if (env.IsDevelopment())
            {
                logger.LogWarning(ex, "[DEV-FALLBACK] Không thể kết nối tới Gmail SMTP ({Host}:{Port}) hoặc thao tác gửi email tới {To} bị gián đoạn/quá thời gian chờ. Trong môi trường phát triển (Development), hệ thống cho phép tiếp tục luồng và mã OTP đã được ghi lại trong Console log.", host, port, to);
                return;
            }

            logger.LogError(ex, "Lỗi khi gửi email qua Gmail SMTP ({Host}:{Port}) tới {To}", host, port, to);
            throw new InvalidOperationException("Không thể gửi email qua máy chủ Gmail SMTP. Vui lòng kiểm tra cấu hình kết nối mạng hoặc thử lại sau.", ex);
        }
    }
}
