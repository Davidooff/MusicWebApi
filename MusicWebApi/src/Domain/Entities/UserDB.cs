using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MusicWebApi.src.Domain.Models;

public class UserDB
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string[] RefreshToken { get; set; } = [];
}