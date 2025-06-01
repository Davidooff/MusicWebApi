using StackExchange.Redis;
using NRedisStack.Search;
using NRedisStack.Search.Literals.Enums;
using Microsoft.Extensions.Options;
using Domain.Options;
using NRedisStack.RedisStackCommands;
using Microsoft.Extensions.Logging;
using Infrastructure.Database;
using System.Text.Json;
using Domain.Entities;
using Infrastructure.Datasbase;

namespace Infrastructure.Redis;

public class UserRedisRepository
{
    private readonly IDatabase _usersDb;
    private readonly ILogger _logger;
    private readonly UserAlbumRepository _albumRepository;
    //private readonly UsersRepository _usersRepository;

    public UserRedisRepository(IOptions<UserRedisRepoSettings> options, ILogger<UserRedisRepository> logger,
        UsersRepository usersRepository, UserAlbumRepository albumRepository)
    {
        _albumRepository = albumRepository;
        //_usersRepository = usersRepository;
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

        try
        {
            _usersDb.FT().DropIndex("hash-idx:users");
            bool hashIndexCreated = _usersDb.FT().Create(
                "hash-idx:users",
                new FTCreateParams()
                    .On(IndexDataType.HASH)
                    .Prefix("huser:"),
                hashSchema
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to create index for verify users: {Message}", ex.Message);
        }
    }

    private async Task SetSession(string userId, string userName)
    {
        var userEl = _usersDb.KeyTypeAsync($"huser:{userId}");
        var usersPlaylist = await _albumRepository.GetUsersPlaylistsInfo(userId);

        if (await userEl != RedisType.Hash)
            await _usersDb.HashSetAsync($"huser:{userId}", 
                new HashEntry[] {
                    new("timesListned", 0),
                    new("userName", userName),
                    new("userAlbums", JsonSerializer.Serialize(usersPlaylist)),
                });
    }

    public async Task delletSession(string userId) =>
        await _usersDb.KeyDeleteAsync($"huser:{userId}");

    public async Task CreateOrExtendTtl(string userId, string userName)
    {
        var userEl = await _usersDb.KeyTypeAsync($"huser:{userId}");
        if (userEl != RedisType.Hash)
            await SetSession(userId, userName);
        else 
            await _usersDb.KeyExpireAsync($"huser:{userId}", TimeSpan.FromMinutes(30));
    }

    public async Task<PlaylistInfo[]?> GetUserAlbums(string userId)
    {
        var searchResult = await _usersDb.HashGetAsync($"huser:{userId}", "userAlbums");
        if (searchResult.IsNull) {
        
            return (await UpdateUserAlbums(userId)).Value.data.ToArray();    
        };
        return JsonSerializer.Deserialize<PlaylistInfo[]>(searchResult.ToString());
    }

    public async Task<(bool result, IEnumerable<PlaylistInfo> data)?> UpdateUserAlbums(string userId)
    {
        var albumsInfo = await _albumRepository.GetUsersPlaylistsInfo(userId);
        if (albumsInfo == null)
            return null;
        return (await _usersDb.HashSetAsync($"huser:{userId}", "userAlbums", JsonSerializer.Serialize(albumsInfo)), albumsInfo);
    }
}




