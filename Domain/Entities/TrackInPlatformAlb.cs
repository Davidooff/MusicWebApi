namespace Domain.Entities
{

    public class TrackInPlatformAlb : IdNameGroup
    {
        public int Duration { get; set; } = 0;
        public ListeningStats TimesListened = new ListeningStats(0);

        public TrackInPlatformAlb(string id, string name) : base(id, name)
        {
        }
    }
    
}
