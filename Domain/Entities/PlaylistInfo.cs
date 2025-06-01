
namespace Domain.Entities;
public class PlaylistInfo : IdNameGroup
{
    public PlaylistInfo(string id, string name) : base(id, name)
    {
    }

    public TrackImage[] Imgs { get; set; } = Array.Empty<TrackImage>();

    public IdNameGroup Owner { get; set; } = null!;

}

