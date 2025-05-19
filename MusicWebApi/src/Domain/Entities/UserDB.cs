using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MusicWebApi.src.Domain.Entities;

namespace MusicWebApi.src.Domain.Models;

public class UserDB
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; } = null!;

    public string Username { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Password { get; set; } = null!;

    public bool IsVerified { get; set; } = false;

    public Session[] Sessions { get; set; } = [];
}