using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using Wardrobe.Application.Common.Interfaces;

namespace Wardrobe.Infrastructure.Services;

public class EmailSenderService : IEmailSenderService
{
    private readonly IConfiguration _configuration;

    public EmailSenderService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendEmailAsync(
        string recipientEmail, 
        string subject, 
        string htmlBody, 
        CancellationToken cancellationToken = default)
    {
        var host = _configuration["Smtp:Host"] ?? "localhost";
        var port = int.TryParse(_configuration["Smtp:Port"], out var parsedPort) ? parsedPort : 1025;
        var senderEmail = _configuration["Smtp:SenderEmail"] ?? "noreply@wardrobe.local";
        var senderName = _configuration["Smtp:SenderName"] ?? "Virtual Wardrobe";

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(senderName, senderEmail));
        message.To.Add(MailboxAddress.Parse(recipientEmail));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = htmlBody
        };

        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(host, port, SecureSocketOptions.None, cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}
