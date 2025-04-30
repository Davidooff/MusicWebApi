using Microsoft.Extensions.Options;
using NRedisStack.RedisStackCommands;
using NRedisStack.Search.Literals.Enums;
using NRedisStack.Search;
using MusicWebApi.src.Domain.Options;


namespace MusicWebApi.src.Infrastructure.Redis;

public class TokenRepository : RedisRepositoryBase
{
    public TokenRepository(IOptions<RedisSettings> redisSettings)
        : base(redisSettings)
    {
        //var schema = new Schema();

        //bool _ = db.FT().Create(
        //    "idx:users",
        //    new FTCreateParams()
        //        .On(IndexDataType.HASH)
        //        .Prefix("user:"),
        //    schema
        //);
        //Console.WriteLine(_.ToString());
    }

    public string setSession(string id)
    {
        string sessionId = "user:" + CreateId(id);
        db.StringSet(sessionId, id); 
        db.KeyExpire(sessionId, new TimeSpan(0, 30, 0)); 
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

