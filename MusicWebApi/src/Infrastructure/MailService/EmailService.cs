using MailKit;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using MusicWebApi.src.Domain.Options;

namespace MusicWebApi.src.Infrastructure.MailService;
public class EmailService
{
    private readonly SmtpClient _smtpClient;

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
    }

    public void SendCode(int code, string to)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Joey Tribbiani", "joey@friends.com"));
        message.To.Add(new MailboxAddress("User", to));
        message.Subject = "Code";

        message.Body = new TextPart("plain")
        {
            Text = code.ToString()
        };

        _smtpClient.Send(message);

    }
}

