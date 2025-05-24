using StackExchange.Redis;
using NRedisStack.Search;
using NRedisStack.Search.Literals.Enums;
using Microsoft.Extensions.Options;
using MusicWebApi.src.Domain.Options;
using NRedisStack.RedisStackCommands;

namespace MusicWebApi.src.Infrastructure.Redis;

public class UserRedisRepository
{
    // key: accessToken. val: userId;refreshToken
    private readonly IDatabase _usersDb;
    private readonly ILogger _logger;

    public UserRedisRepository(IOptions<UserRedisRepoSettings> options, ILogger<UserRedisRepository> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger), "Logger cannot be null.");


        ConfigurationOptions conf = new ConfigurationOptions
        {
            EndPoints = { options.Value.EndPoint },
            User = options.Value.User,
            Password = options.Value.Password,
        };

        ConnectionMultiplexer muxer = ConnectionMultiplexer.Connect(conf);
        _usersDb = muxer.GetDatabase(0);

        var hashSchema = new Schema()
            .AddNumericField("timesListned");

        bool hashIndexCreated = _usersDb.FT().Create(
            "hash-idx:users",
            new FTCreateParams()
                .On(IndexDataType.HASH)
                .Prefix("huser:"),
            hashSchema
        );
    }

    private async Task SetSession(string userId)
    {
        var userEl = await _usersDb.KeyTypeAsync($"huser:{userId}");

        if (userEl != RedisType.Hash)
            await _usersDb.HashSetAsync($"huser:{userId}", 
                new HashEntry[] {
                    new("timesListned", 0)
                });
    }

    public async Task delletSession(string userId) =>
        await _usersDb.KeyDeleteAsync($"huser:{userId}");

    public async Task CreateOrExtendTtl(string userId)
    {
        var userEl = await _usersDb.KeyTypeAsync($"huser:{userId}");
        if (userEl != RedisType.Hash)
            await SetSession(userId);
        else 
            await _usersDb.KeyExpireAsync($"huser:{userId}", TimeSpan.FromMinutes(30));
    }
}




