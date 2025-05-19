using Redis.OM;
using Redis.OM.Searching;
using Microsoft.Extensions.Options;
using MusicWebApi.src.Domain.Options;
using MusicWebApi.src.Domain.Entities;

namespace MusicWebApi.src.Infrastructure.Redisk;
public class VerifyMailRepo
{
    private readonly short _maxAttempts;
    private readonly RedisConnectionProvider _provider;
    private readonly IRedisCollection<VerifyUserRedis> _usersCollection;
    Random random = new Random();

    public VerifyMailRepo(IOptions<RedisSettings> options)
    {
        string connectionStringWithDb = options.Value.EndPoint + ",db=" + options.Value.VerifyUserDbIndex;
        _provider = new RedisConnectionProvider(connectionStringWithDb) ?? throw new Exception("Unable to connect to db");
        _usersCollection = _provider.RedisCollection<VerifyUserRedis>();
        InitializeAsync();
        _maxAttempts = options.Value.VerificationLimit;
    }

    // It's good practice to explicitly create indexes on startup
    private async Task InitializeAsync()
    {
        await _provider.Connection.CreateIndexAsync(typeof(VerifyUserRedis));
    }

    public short CreateCode()
    {         // Generate a random 4-digit code
        short code = (short)random.Next(1000, 9999);
        return code;
    }

    public async Task Create(string token, string userId, short code)
    {
        var user = new VerifyUserRedis
        {
            Token = token,
            UserId = userId,
            Code = code,
        };
        await _usersCollection.InsertAsync(user, TimeSpan.FromMinutes(15));
    }

    public async Task<VerifyUserRedis?> Get(string sesionId)
    {
        var query = _usersCollection.Where(x => x.Id == sesionId);
        var result = await query.FirstOrDefaultAsync();
        return result;
    }

    /// <summary>
    /// Verifies the code for the given session ID. If sesion ID is not found, it returns null.
    /// </summary>
    /// <param name="sesionId"></param>
    /// <param name="code"></param>
    /// <returns></returns>
    public async Task<bool?> Verify(string sesionId, short code)
    {
        var query = _usersCollection.Where(x => x.Id == sesionId);
        var result = await query.FirstOrDefaultAsync();
        
        if (result is null)
            return null;

        if (result.Attempts >= _maxAttempts)
        {
            _usersCollection.DeleteAsync(result);
            return null;
        }

        if (result.Code == code)
        {
            _usersCollection.DeleteAsync(result);
            return true;
        }
        result.Attempts++;
        await _usersCollection.UpdateAsync(result);
        return false;
    }
}

