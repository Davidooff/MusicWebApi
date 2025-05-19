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

    protected string CreateId(string source)
    {
        var tmpSource = Encoding.ASCII.GetBytes(source);
        return Convert.ToHexString(_mdProvider.ComputeHash(tmpSource));
    }
}

