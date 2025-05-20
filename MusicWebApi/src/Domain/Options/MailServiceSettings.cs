namespace MusicWebApi.src.Domain.Options
{
    public class MailServiceSettings
    {
        public string Url { get; set; } = string.Empty;
        public int Port { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string DefaultFromAddress { get; set; } = string.Empty;
        public string DefaultFromName { get; set; } = string.Empty; 

    }
}
