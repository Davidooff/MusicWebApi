using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MusicWebApi.src.Domain.Entities;
public class UserAlbumDB
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; } = null!;

    public string OwnerId { get; set; } = null!;

    public TrackData[] Track { get; set; } = [];

    public int timesOpened { get; set; } = 0;
}

