using Microsoft.Extensions.Options;
using MusicWebApi.src.Domain.Options;
using StackExchange.Redis;
using System;
using System.Security.Cryptography;
using System.Text;


namespace MusicWebApi.src.Infrastructure.Redis;
public abstract class RedisRepositoryBase
{
    protected readonly IDatabase db;
    private readonly MD5 _mdProvider = MD5.Create(); // Changed field to private instance

    protected RedisRepositoryBase(IOptions<RedisSettings> redisSettings)
    {
        if (redisSettings == null || redisSettings.Value == null)
            throw new ArgumentNullException(nameof(redisSettings), "Redis settings cannot be null.");

        Console.WriteLine("redisSettings.Value.EndPoint");
        Console.WriteLine(redisSettings.Value.EndPoint);
        ConfigurationOptions conf = new ConfigurationOptions
        {
            EndPoints = { redisSettings.Value.EndPoint },
            User = redisSettings.Value.User,
            Password = redisSettings.Value.Password
        };


        ConnectionMultiplexer redis = ConnectionMultiplexer.Connect(conf);
        db = redis.GetDatabase();
    }

    protected string CreateId(string source) // Changed method to instance
    {
        var tmpSource = Encoding.ASCII.GetBytes(source);
        return Convert.ToHexString(_mdProvider.ComputeHash(tmpSource));
    }
}

