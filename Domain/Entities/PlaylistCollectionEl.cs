
namespace Domain.Entities;
public class PlaylistCollectionEl : IdNameGroup
{
    public PlaylistCollectionEl(string id, string name) : base(id, name)
    {
    }

    public TrackImage[] Imgs { get; set; } = Array.Empty<TrackImage>();

    public IdNameGroup Owner { get; set; } = null!;

}

