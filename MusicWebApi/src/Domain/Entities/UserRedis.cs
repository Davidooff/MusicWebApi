using Redis.OM.Modeling;

namespace MusicWebApi.src.Domain.Entities;

[Document(StorageType = StorageType.Hash, Prefixes = new[] { "User" })]
public class UserRedis
{
    [RedisIdField] // Marks this property as the primary key for the Redis document.
    [Indexed]      // Allows searching/filtering by this field.
    public string Id { get; set; } = null!;

    [Indexed] public string UserId { get; set; } = null!;

    [Indexed] public string RefToken { get; set; } = null!;

}

