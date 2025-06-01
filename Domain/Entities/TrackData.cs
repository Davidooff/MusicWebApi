namespace Domain.Entities;

public class TrackData
{
    public string Id { get; set; } = null!;
    
    public string Name { get; set; } = null!;

    public IdNameGroup[] Artists { get; set; } = null!;

    public EPlatform EPlatform { get; set; }

    public TrackImage[] ImgUrls { get; set; } = [];

    public string AlbumId { get; set; } = null!;

    public int Duration { get; set; } = 0;

    public TrackData() { }

    public TrackData (TrackInPlatformAlb trackInPlatformAlb, EPlatform platform, IdNameGroup[] artists)
    {
        Id = trackInPlatformAlb.Id;
        Name = trackInPlatformAlb.Name;
        Artists = artists;
        EPlatform = platform;
        //ImgUrls
    }
}

