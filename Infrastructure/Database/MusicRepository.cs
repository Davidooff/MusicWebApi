using MongoDB.Driver;
using Domain.Entities;
using Domain.Options;
using Microsoft.Extensions.Options;

namespace Infrastructure.Database;

public class MusicRepository
{
    private readonly IMongoCollection<AlbumDB> _ytAlbumsCollection;
    private readonly IMongoCollection<TrackDB> _ytTrackCollection;

    private static readonly UpdateDefinition<TrackDB> _incrementCounters =
        Builders<TrackDB>.Update
            .Inc(x => x.TimesListened.Total, 1)
            .Inc(x => x.TimesListened.ThisWeek, 1);

    public MusicRepository(IOptions<DatabaseSettings> databaseSettings)
    {
        var mongoClient = new MongoClient(
            databaseSettings.Value.ConnectionString);

        var mongoDatabase = mongoClient.GetDatabase(
            databaseSettings.Value.DatabaseName);

        _ytAlbumsCollection = mongoDatabase.GetCollection<AlbumDB>(
            databaseSettings.Value.YTPlaylists);

        _ytTrackCollection = mongoDatabase.GetCollection<TrackDB>(
            databaseSettings.Value.YTTracks);

    }

    protected IMongoCollection<AlbumDB> GetAlbumPlatform (EPlatform platform)=> _ytAlbumsCollection;
    protected IMongoCollection<TrackDB> GetTrackPlatform(EPlatform platform) => _ytTrackCollection;


    /// <summary>
    /// Add listening to the track.
    /// </summary>
    /// <returns>Is el was found</returns>
    public async Task<bool> AddListening(string trackId, EPlatform platform)
    {
        var collection = GetTrackPlatform(platform);
        var filter = Builders<TrackDB>.Filter.Eq(el => el.PlatformId, trackId);
        var updateResult = await collection.UpdateOneAsync(filter, _incrementCounters);

        return updateResult.IsAcknowledged && updateResult.ModifiedCount != 0;
    }

    public async Task<bool> AddAlbum(AlbumDB album, EPlatform platform)
    {
        var collection = GetAlbumPlatform(platform);
        var filter = Builders<AlbumDB>.Filter.Eq(el => el.PlatformId, album.PlatformId);
        var replaceOptions = new ReplaceOptions { IsUpsert = true };
        var updateResult = await collection.ReplaceOneAsync(filter, album, replaceOptions);

        return updateResult.IsAcknowledged && updateResult.ModifiedCount != 0;
    }
}

