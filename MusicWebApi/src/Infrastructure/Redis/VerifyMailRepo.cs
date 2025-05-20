using Redis.OM;
using Redis.OM.Searching;
using Microsoft.Extensions.Options;
using MusicWebApi.src.Domain.Options;
using MusicWebApi.src.Domain.Entities;
using StackExchange.Redis;

namespace MusicWebApi.src.Infrastructure.Redisk;
public class VerifyMailRepo
{
    private readonly short _maxAttempts;
    private readonly RedisConnectionProvider _provider;
    private readonly IRedisCollection<VerifyUserRedis> _usersCollection;
    Random random = new Random();
    private readonly string idPattern = @":([^:]+)$";

    public VerifyMailRepo(IOptions<RedisSettings> options)
    {
        Console.WriteLine(options.Value.EndPoint);
        ConfigurationOptions conf = new ConfigurationOptions
        {
            EndPoints = { options.Value.EndPoint },
            DefaultDatabase = options.Value.VerifyUserDbIndex,
            User = options.Value.User,
            Password = options.Value.Password
        };
        _provider = new RedisConnectionProvider(conf);
        _provider.Connection.CreateIndexAsync(typeof(VerifyUserRedis));
        _usersCollection = _provider.RedisCollection<VerifyUserRedis>();
        _maxAttempts = options.Value.VerificationLimit;
    }

    public short CreateCode()
    {         // Generate a random 4-digit code
        short code = (short)random.Next(1000, 9999);
        return code;
    }

    // returns the sessionId or null if not created
    public async Task Create(string userId, short code)
    {
        var user = new VerifyUserRedis
        {
            UserId = userId,
            Code = code,
        };
        await _usersCollection.InsertAsync(user, TimeSpan.FromMinutes(15));
    }
    
    /// <summary>
    /// Verifies the code for the given session ID. If sesion ID is not found, it returns null.
    /// </summary>
    /// <param name="sesionId"></param>
    /// <param name="code"></param>
    /// <returns></returns>
    public async Task<bool?> Verify(string userID, short code)
    {
        var el = await _usersCollection.Where(x => x.UserId == userID).FirstOrDefaultAsync();
        
        Console.WriteLine(el.ToString());

        if (el is null)
            return null;

        if (el.Attempts >= _maxAttempts)
        {
            await _usersCollection.DeleteAsync(el);
            return null;
        }

        if (el.Code == code)
        {
            await _usersCollection.DeleteAsync(el);
            return true;
        }
        el.Attempts++;
        await _usersCollection.UpdateAsync(el);
        return false;
    }
}

