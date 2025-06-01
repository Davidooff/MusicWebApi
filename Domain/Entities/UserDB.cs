using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Domain.Entities;

public class UserDB
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; } = null!;

    public string Username { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Password { get; set; } = null!;

    public bool IsVerified { get; set; } = false;

    public string LikesId { get; set; } = null!;

    public string DislikesId { get; set; } = null!;

    public string HistoryId { get; set; } = null!;

    public Session[] Sessions { get; set; } = [];
}