using System.Net;
using System.Net.Mail;

namespace SecondHandMarketplace.Web.Services.Auth;

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default);
}

public sealed class SmtpEmailSender(IConfiguration configuration) : IEmailSender
{
    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        var section = configuration.GetSection("Email:Smtp");
        var host = section["Host"] ?? "smtp.gmail.com";
        var username = section["Username"];
        var from = section["From"] ?? username;
        var password = section["Password"];
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(from))
            throw new InvalidOperationException("Thiếu cấu hình Email:Smtp:Username hoặc Email:Smtp:Password. Hãy dùng địa chỉ Gmail và Mật khẩu ứng dụng, không dùng mật khẩu đăng nhập Gmail.");

        using var message = new MailMessage
        {
            From = new MailAddress(from, section["DisplayName"] ?? "Re:Market"),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(to);
        using var client = new SmtpClient(host, int.TryParse(section["Port"], out var port) ? port : 587)
        {
            EnableSsl = bool.TryParse(section["EnableSsl"], out var ssl) ? ssl : true,
            DeliveryMethod = SmtpDeliveryMethod.Network
        };
        client.Credentials = new NetworkCredential(username, password);
        await client.SendMailAsync(message, cancellationToken);
    }
}
