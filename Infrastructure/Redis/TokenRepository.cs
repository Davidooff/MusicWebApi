using Microsoft.Extensions.Options;
using Domain.Options;
using NRedisStack.Search.Literals.Enums;
using NRedisStack.Search;
using StackExchange.Redis;
using NRedisStack.RedisStackCommands;
using System.Text;
using Domain.Entities;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Infrastructure.Database;


namespace Infrastructure.Redis;

public class TokenRepository
{
    private readonly IDatabase _db;
    private readonly UserRedisRepository _userRepository;
    private readonly ILogger _logger;
    private readonly static Regex regex = new Regex(@";([^;]+)");

    public TokenRepository(
        IOptions<TokenRepoSettings> options, 
        ILogger<TokenRepository> logger, 
        UserRedisRepository userRepository
        )
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger), "Logger cannot be null.");
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository), "UserRepository cannot be null.");

        ConfigurationOptions conf = new ConfigurationOptions
        {
            EndPoints = {options.Value.EndPoint},
            User = options.Value.User,
            Password = options.Value.Password,
        };

        ConnectionMultiplexer muxer = ConnectionMultiplexer.Connect(conf);
        _db = muxer.GetDatabase();

        var hashSchema = new Schema()
            .AddTextField("userId")
            .AddTextField("sessionId")
            .AddTextField("ref");

        try
        {
            _db.FT().DropIndex("hash-idx:tokens");
            bool hashIndexCreated = _db.FT().Create(
                "hash-idx:tokens",
                new FTCreateParams()
                    .On(IndexDataType.HASH)
                    .Prefix("htokens:"),
                hashSchema
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to create index for verify users: {Message}", ex.Message);
        }
    }

    /// <summary>
    /// Generates a deterministic, unique string ID from an input string using SHA256.
    /// The same input string will always produce the same ID.
    /// </summary>
    /// <param name="inputString">The string to generate an ID for.</param>
    /// <returns>A 64-character hexadecimal string representing the SHA256 hash, or null if input is null.</returns>
    private static string GenerateDeterministicId(string inputString)
    {
        // Use SHA256 for hashing
        using (SHA256 sha256Hash = SHA256.Create())
        {
            // Convert the input string to a byte array and compute the hash.
            // UTF-8 is a common and recommended encoding.
            byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(inputString));

            // Convert byte array to a hexadecimal string
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2")); // "x2" formats as a two-character lowercase hex string
            }
            return builder.ToString();
        }
    }

    private async Task<string> setSession(string userId, string refreshToken)
    {
        var sessionId = GenerateDeterministicId(refreshToken);

        await _db.HashSetAsync($"htokens:{sessionId};{userId}" , new HashEntry[]
        {
            new("userId", userId),
            new("sessionId", sessionId),
            new("ref", refreshToken),
        });
        await _db.KeyExpireAsync("htokens:" + sessionId, TimeSpan.FromMinutes(60));
        return sessionId;
    }

    public async Task<string?> getUserIdBySeesion(string sessionId)
    {
        var el = await _db.FT().SearchAsync("hash-idx:tokens", new Query($"@sessionId:{sessionId}"));

        if (el.TotalResults == 0)
            return null;

        Console.WriteLine($"Found {el.TotalResults} results for sessionId: {sessionId}. Key: {el.Documents[0].Id}");
        Match match = regex.Match(el.Documents[0].Id);

        return match.Groups[1].Value;
    }

    public async Task<bool> RemoveSession(string sessionId)
    {
        var el = await _db.FT().SearchAsync("hash-idx:tokens", new Query($"@sessionId:{sessionId}"));

        if (el.TotalResults == 0)
            return false;

        await _db.KeyDeleteAsync(el.Documents[0].Id);
        return true;
    }

    public async Task<bool> RemoveSessionByToken(string refreshToken)
    {
        var el = await _db.FT().SearchAsync("hash-idx:tokens", new Query($"@ref:{refreshToken}"));

        if (el.TotalResults == 0)
            return false;

        await _db.KeyDeleteAsync(el.Documents[0].Id);
        return true;
    }


    private async Task<TokenRedis?> GetElBySeessionId(string sessionId)
    {
        var el = await _db.HashGetAllAsync("htokens:" + sessionId);
        if (el.Length == 0)
            return null;

        string userId = el.FirstOrDefault(x => x.Name == "userId").Value!;
        string refreshToken = el.FirstOrDefault(x => x.Name == "ref").Value!;

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(refreshToken))
            return null;

        return new TokenRedis
        {
            UserId = userId,
            RefreshToken = refreshToken
        };
    }

    public async Task<bool> removeSessionByRefreshToken(string token)
    {
        var el = await _db.FT().SearchAsync(
            "hash-idx:tokens",
            new Query($"@ref:{token}")
        );

        if (el.TotalResults == 0)
            return false;

        // Extract the key from search results
        var key = el.Documents[0].Id;

        // Delete the key
        await _db.KeyDeleteAsync(key);

        return true;
    }

    public async Task<string> CreateNewSession(string userId, string userName, string refreshToken)
    {
        await _userRepository.CreateOrExtendTtl(userId, userName);
        return await setSession(userId, refreshToken);
    }
}

