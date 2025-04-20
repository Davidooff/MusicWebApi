namespace MusicWebApi.src.Domain.Entities
{
    public class Session
    {
        public string RefreshToken = null!;

        public string Name = null!;

        public ESessionType SessionType;
    }
}
