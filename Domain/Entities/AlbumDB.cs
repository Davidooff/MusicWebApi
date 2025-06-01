using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Domain.Entities;

namespace Domain.Entities;

public class AlbumDB
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; } = null!;

    public string BrowseId { get; set; } = null!;

    public string AlbumId { get; set; } = null!;

    public string Name { get; set; } = null!;

    public TrackImage[] TrackImage { get; set; }

    public IdNameGroup[] Author { get; set; } = null!;

    public TrackInPlatformAlb[] Trackes { get; set; } = null!; 

    public ListeningStats TimesListened { get; set; } = new ListeningStats(0);

    public string? ImgUrl { get; set; }
}