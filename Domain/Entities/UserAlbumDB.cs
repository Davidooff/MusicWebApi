using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Domain.Entities;
public class UserAlbumDB
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; } = null!;

    public string Name { get; set; } = "New album";

    public IdNameGroup Owner { get; set; } = null!;

    public TrackData[] Track { get; set; } = Array.Empty<TrackData>();

    public int timesOpened { get; set; } = 0;


    public UserAlbumDB(IdNameGroup owner, string? name)
    {
        Owner = owner;
        if (name is not null) Name = name;
    }
}

