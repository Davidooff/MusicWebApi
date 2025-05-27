
using Domain.Options;
using Microsoft.Extensions.Options;
using NRedisStack.Search.Literals.Enums;
using NRedisStack.Search;
using StackExchange.Redis;
using NRedisStack.RedisStackCommands;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Redis;


public class VerifyMailRepo
{
    private readonly IDatabase _db;
    private readonly short _maxAttempts;
    private readonly Random _random = new Random();
    private readonly ILogger<VerifyMailRepo> _logger;

    public VerifyMailRepo(IOptions<VerifyRepoSettings> options, ILogger<VerifyMailRepo> logger)
    {
        _maxAttempts = options.Value.VerificationLimit;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger), "Logger cannot be null.");

        ConfigurationOptions conf = new ConfigurationOptions
        {
            EndPoints = { options.Value.EndPoint },
            User = options.Value.User,
            Password = options.Value.Password,
        };

        _db = ConnectionMultiplexer.Connect(conf).GetDatabase();

        var hashSchema = new Schema()
            .AddNumericField("code")
            .AddNumericField("attempts");

        try
        {
            _db.FT().DropIndex("hash-idx:verifyUsers");
            bool hashIndexCreated = _db.FT().Create(
                "hash-idx:verifyUsers",
                new FTCreateParams()
                    .On(IndexDataType.HASH)
                    .Prefix("hverifyUser:"),
                hashSchema
            );
        } catch(Exception ex)
        {
            _logger.LogWarning("Failed to create index for verify users: {Message}", ex.Message);
        }
    }

    public int CreateCode() =>
        _random.Next(100000, 999999);

    public async Task Create(string userId, int code)
    {
        //var userEl = await _db.KeyTypeAsync($"hverifyUser:{userId}");
        //if (userEl != RedisType.Hash)
        await _db.HashSetAsync($"hverifyUser:{userId}",
            new HashEntry[] {
                new("code", code),
                new("attempts", 0)
            });
        await _db.KeyExpireAsync($"hverifyUser:{userId}", TimeSpan.FromMinutes(5));
    }

    /// <summary>
    /// Verifies the code for the given session ID. If sesion ID is not found, it returns null.
    /// </summary>
    /// <param name="sesionId"></param>
    /// <param name="code"></param>
    /// <returns></returns>
    public async Task<bool?> Verify(string userID, int code)
    {
        var el = await _db.HashGetAllAsync($"hverifyUser:{userID}");

        if (el is null || el.Length == 0)
        {
            return null;
        }

        var attempts = (short)el.First(el => el.Name == "attempts").Value;
        var storedCode = (int)el.First(el => el.Name == "code").Value;

        if (attempts >= _maxAttempts)
        {
            await _db.KeyDeleteAsync($"hverifyUser:{userID}");
            return null;
        }

        if (storedCode == code)
        {
            await _db.KeyDeleteAsync($"hverifyUser:{userID}");
            return true;
        }

        await _db.HashIncrementAsync($"hverifyUser:{userID}", "attempts");
        return false;
    }

    public async Task<bool> RemoveByUserId(string userId)
    {
        return await _db.KeyDeleteAsync($"hverifyUser:{userId}");
    }
}
