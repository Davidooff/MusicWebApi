using StackExchange.Redis;
using Microsoft.Extensions.Options;
using MusicWebApi.src.Domain.Options;


namespace MusicWebApi.src.Infrastructure.Redis;

public class TokenRepository : RedisRepositoryBase
{
    private readonly IDatabase _db;  

    public TokenRepository(IOptions<RedisSettings> redisSettings)
    {
        if (redisSettings == null || redisSettings.Value == null)
            throw new ArgumentNullException(nameof(redisSettings), "Redis settings cannot be null.");

        var conf = new ConfigurationOptions
        {
            EndPoints = { redisSettings.Value.EndPoint },
            DefaultDatabase = redisSettings.Value.TokenDbIndex,
            User = redisSettings.Value.User,
            Password = redisSettings.Value.Password
        };

        ConnectionMultiplexer redis = ConnectionMultiplexer.Connect(conf);
        _db = redis.GetDatabase();
    }

    public string setSession(string id)
    {
        string sessionId = "user:" + CreateId(id);
        _db.StringSet(sessionId, id, new TimeSpan(0, 30, 0));
        return sessionId;
    }

    public void delletSession(string sessionId) =>
        db.KeyDelete(sessionId);

    public string? getSession(string sessionId)
    {
        var dbId = db.StringGet(sessionId);
        return dbId;
    }
}

