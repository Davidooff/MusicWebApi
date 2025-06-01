using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Domain.Entities;
public class UserPlaylistDB : Playlist
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; } = null!;

    public bool DefaultPlaylist{ get; set; } = false;

    public UserPlaylistDB(IdNameGroup owner, string? name): base(owner, name) { }
}

