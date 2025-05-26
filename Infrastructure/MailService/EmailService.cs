using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using Domain.Options;

namespace Infrastructure.MailService;

public class EmailService
{
    private readonly SmtpClient _smtpClient;
    private readonly MailboxAddress _mailBoxAddress;

    public EmailService(IOptions<MailServiceSettings> mailServiceOptions)
    {
        if (mailServiceOptions == null)
        {
            throw new ArgumentNullException(nameof(mailServiceOptions));
        }
        var settings = mailServiceOptions.Value;
        _smtpClient = new SmtpClient();
        _smtpClient.Connect(settings.Url, settings.Port, false);
        _smtpClient.Authenticate(settings.Username, settings.Password);
        _mailBoxAddress = new MailboxAddress(mailServiceOptions.Value.DefaultFromName, mailServiceOptions.Value.DefaultFromAddress);
    }

    public void SendCode(int code, string to)
    {
        var message = new MimeMessage();
        message.From.Add(_mailBoxAddress);
        message.To.Add(new MailboxAddress("User", to));
        message.Subject = "Code";

        message.Body = new TextPart("plain")
        {
            Text = code.ToString()
        };

        _smtpClient.Send(message);

    }
}

