namespace MusicWebApi.src.Domain.Entities;

public class TrackData
{
    public string Id { get; set; } = null!;
    
    public string Name { get; set; } = null!;

    public IdNameGroup[] Artists { get; set; } = null!;

    public EPlatform EPlatform { get; set; }

    public string ImgUrl { get; set; } = string.Empty;

    public string AlbumId { get; set; } = null!;

    public int Duration { get; set; } = 0;
}

