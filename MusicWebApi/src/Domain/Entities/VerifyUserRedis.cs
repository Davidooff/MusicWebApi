using Redis.OM.Modeling;

namespace MusicWebApi.src.Domain.Entities;
[Document(StorageType = StorageType.Hash, Prefixes = new[] { "UnverifedUser" })]
public class VerifyUserRedis
{
    [RedisIdField] // Marks this property as the primary key for the Redis document.
    [Indexed]      // Allows searching/filtering by this field.
    public string Id { get; set; } = null!;

    [Indexed] public string UserId { get; set; } = null!;

    public short Code { get; set; }

    public short Attempts { get; set; } = 0;
}

