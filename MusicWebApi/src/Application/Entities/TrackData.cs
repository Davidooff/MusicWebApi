namespace MusicWebApi.src.Application.Entities;

public class TrackData
{
    public string Name { get; set; } = null!;

    public string Id { get; set; } = null!;

    public IdNameGroup[] Artists { get; set; } = Array.Empty<IdNameGroup>();

    public IdNameGroup Album { get; set; } = null!;
}

