using StackExchange.Redis;
using Microsoft.Extensions.Options;
using MusicWebApi.src.Domain.Options;
using System.Text.RegularExpressions;
using MusicWebApi.src.Domain.Entities;
using Redis.OM;
using Redis.OM.Searching;


namespace MusicWebApi.src.Infrastructure.Redis;

public class TokenRepository
{
    private readonly RedisConnectionProvider _provider;
    private readonly IRedisCollection<UserRedis> _usersCollection;
    private readonly ILogger _logger;
    

    public TokenRepository(IOptions<TokenRepoSettings> options, ILogger<TokenRepository> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger), "Logger cannot be null.");

        ConfigurationOptions conf = new ConfigurationOptions
        {
            EndPoints = { options.Value.EndPoint },
            User = options.Value.User,
            Password = options.Value.Password
        };
        _provider = new RedisConnectionProvider(conf);
        _provider.Connection.CreateIndexAsync(typeof(UserRedis));
        _usersCollection = _provider.RedisCollection<UserRedis>();
    }

    public async Task<string> setSession(string userId, string refreshToken)
    {
        var user = new UserRedis
        {
            UserId = userId,
            RefToken = refreshToken,
        };

        var key = await _usersCollection.InsertAsync(user, TimeSpan.FromMinutes(15));

        return key;
    }

    public async Task delletSession(string key) =>
        await _provider.Connection.UnlinkAsync(key);

    public async Task<UserRedis?> getUserById(string key) => 
        await _usersCollection.FindByIdAsync(key);        
    
    public async Task delleteByRefToken(string token)
    {
        var user = await _usersCollection.Where(x => x.RefToken == token).FirstOrDefaultAsync();
        if (user is null)
        {
            _logger.LogError("Session not found: {token}", token);
            throw new Exception("Session not found");
        }
        await _usersCollection.DeleteAsync(user);
    }
}



//public class TokenRepository : RedisRepositoryBase
//{
//    private readonly IDatabase _db;
//    private readonly ILogger _logger;
//    private const string pattern = @"^(.*?);(.*?)$";

//    public TokenRepository(IOptions<TokenRepoSettings> redisSettings, ILogger logger)
//    {
//        _logger = logger ?? throw new ArgumentNullException(nameof(logger), "Logger cannot be null.");

//        if (redisSettings == null || redisSettings.Value == null)
//            throw new ArgumentNullException(nameof(redisSettings), "Redis settings cannot be null.");

//        var conf = new ConfigurationOptions
//        {
//            EndPoints = { redisSettings.Value.EndPoint },
//            User = redisSettings.Value.User,
//            Password = redisSettings.Value.Password
//        };

//        ConnectionMultiplexer redis = ConnectionMultiplexer.Connect(conf);
//        _db = redis.GetDatabase();
//    }

//    public string setSession(string id, string refreshToken)
//    {
//        string sessionId = "user:" + CreateId(id);
//        _db.StringSet(sessionId, id + ";" + refreshToken, new TimeSpan(0, 30, 0));
//        return sessionId;
//    }

//    public bool delletSession(string sessionId) =>
//        db.KeyDelete(sessionId);

//    public (string userId, string refToken) getUserInfoBySessionId(string sessionId)
//    {
//        string? dbInfo = db.StringGet(sessionId);

//        if (dbInfo == null)
//        {
//            _logger.LogError("Session not found: {sessionId}", sessionId);
//            throw new Exception("Session not found");
//        }

//        Match match = Regex.Match(dbInfo, pattern);

//        if (match.Success && match.Groups.Count == 2)
//            return (match.Groups[1].Value, match.Groups[2].Value);
//        else
//        {
//            _logger.LogError("By session id: {sessionId}. Failed to parse session data: {sessionData}", sessionId, dbInfo);
//            throw new Exception("Failed to parse session data");
//        }

//    }
//}
