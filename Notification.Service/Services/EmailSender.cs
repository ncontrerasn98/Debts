using MailKit.Net.Smtp;
using MimeKit;

namespace Notification.Service.Services;

public class EmailSender : IEmailSender
{
    private readonly string _host;
    private readonly int _port;
    private readonly string _from;

    public EmailSender(IConfiguration configuration)
    {
        _host = configuration["Email:Host"] ?? "localhost";
        _port = int.Parse(configuration["Email:Port"] ?? "1025");
        _from = configuration["Email:From"] ?? "notifications@debts.com";
    }

    public async Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        var email = new MimeMessage();
        email.From.Add(MailboxAddress.Parse(_from));
        email.To.Add(MailboxAddress.Parse(to));
        email.Subject = subject;
        email.Body = new TextPart("html") { Text = body };

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(_host, _port, false, cancellationToken);
        await smtp.SendAsync(email, cancellationToken);
        await smtp.DisconnectAsync(true, cancellationToken);
    }
}