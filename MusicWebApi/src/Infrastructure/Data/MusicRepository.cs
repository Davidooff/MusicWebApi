using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MusicWebApi.src.Domain.Models;
using MusicWebApi.src.Infrastructure.Options;

namespace MusicWebApi.src.Infrastructure.Services;
public enum EPlatform
{
    YTMusic = 0
}

public class MusicRepository
{
    private readonly IMongoCollection<AlbumDB> _ytMusicCollection;

    public MusicRepository(
        IOptions<DatabaseSettings> databaseSettings)
    {
        var mongoClient = new MongoClient(
            databaseSettings.Value.ConnectionString);

        var mongoDatabase = mongoClient.GetDatabase(
            databaseSettings.Value.DatabaseName);

        _ytMusicCollection = mongoDatabase.GetCollection<AlbumDB>(
            databaseSettings.Value.YTMusicCollectionName);
    }

    public async Task<AlbumDB?> Find(string id, EPlatform platform) =>
        platform switch
        {
            EPlatform.YTMusic => await _ytMusicCollection.Find(x => x.Id == id).FirstOrDefaultAsync(),
            _ => throw new ArgumentException("Invalid platform", nameof(platform))
        };


    public async Task AddAlbum(AlbumDB albumDB, EPlatform platform)
    {
        var collection = platform switch
        {
            EPlatform.YTMusic => _ytMusicCollection,
            _ => throw new ArgumentException("Invalid platform", nameof(platform))
        };
        var filter = Builders<AlbumDB>.Filter.Eq(el => el.Id, albumDB.Id);
        var el = _ytMusicCollection.Find(filter).FirstOrDefault();
            
        if (el is not null)
        {
            el.Trackes = el.Trackes.Concat(albumDB.Trackes)
                  .GroupBy(kv => kv.Key)
                  .ToDictionary(g => g.Key, g => g.Last().Value);
            await UpdateAsync(albumDB, platform);
        } else
        {
            await collection.InsertOneAsync(albumDB);
        }
    }

    public async Task UpdateAsync(AlbumDB albumDB, EPlatform platform)
    {
        var _ = platform switch
        {
            EPlatform.YTMusic => _ytMusicCollection,
            _ => throw new ArgumentException("Invalid platform", nameof(platform))
        };
        await _.ReplaceOneAsync(x => x.Id == albumDB.Id, albumDB);
    }
}

