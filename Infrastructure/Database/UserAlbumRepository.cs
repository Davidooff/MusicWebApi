using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Domain.Entities;
using Domain.Options;
using Org.BouncyCastle.Security;
using System.Xml.Linq;

namespace Infrastructure.Database;
public class UserAlbumRepository
{
    private readonly IMongoCollection<UserAlbumDB> _userAbumCollection;

    public UserAlbumRepository(
        IOptions<DatabaseSettings> databaseSettings)
    {
        var mongoClient = new MongoClient(
            databaseSettings.Value.ConnectionString);

        var mongoDatabase = mongoClient.GetDatabase(
            databaseSettings.Value.DatabaseName);

        _userAbumCollection = mongoDatabase.GetCollection<UserAlbumDB>(
            databaseSettings.Value.UserAbums);
    }

    public async Task<UserAlbumDB?> GetAsync(string userId) =>
        await _userAbumCollection.Find(x =>
            x.Id == userId)
            .FirstOrDefaultAsync();

    public async Task CreateAsync(string userId, string userName, string? name) =>
        await _userAbumCollection.InsertOneAsync(new UserAlbumDB(new(userId, userName), null));

    public async Task RemovePlaylis(string id) =>
        await _userAbumCollection.DeleteOneAsync(x => x.Id == id);

    public async Task<bool> AddTrack(string albumId, TrackData trackData)
    {
        var _ = await _userAbumCollection.UpdateOneAsync(
            x => x.Id == albumId,
            Builders<UserAlbumDB>.Update.Push(x => x.Track, trackData));
        return _.IsAcknowledged && _.ModifiedCount == 1;
    }

    public async Task<bool> RemoveTrack(string albumId, string trackId, EPlatform ePlatform)
    {
        var _ = await _userAbumCollection.UpdateOneAsync(
            x => x.Id == albumId,
            Builders<UserAlbumDB>.Update.PullFilter(x => x.Track, x => x.Id == trackId && x.EPlatform == ePlatform));

        return _.IsAcknowledged && _.ModifiedCount == 1;
    }

    public async Task<List<UserAlbumDB>?> GetUserAlbums(string userId)
    {
        return await (await _userAbumCollection.FindAsync(x => x.Owner.Id == userId)).ToListAsync();
    }

    public async Task<IEnumerable<PlaylistCollectionEl>> GetUsersPlaylistsInfo(string userId)
    {
        var filter = Builders<UserAlbumDB>.Filter.Eq(u => u.Owner.Id, userId);
        var userAlbums = await _userAbumCollection.FindAsync(filter);
        if (userAlbums == null)
            return [];

        return userAlbums.ToList().Select(x => 
            new PlaylistCollectionEl (x.Id, x.Name) { 
              Owner = x.Owner, Imgs = GetImgs(x.Track, 500) 
            });
    }

    private TrackImage[] GetImgs(TrackData[] trackData, int target)
    {
        if (trackData.Length == 0)
            return [];
        if (trackData.Length >= 1 && trackData.Length < 4)
            return [TrackImage.TakeNearestResolution(trackData[0].ImgUrls, target)];
        else
            return [
                TrackImage.TakeNearestResolution(trackData[0].ImgUrls, target),
                TrackImage.TakeNearestResolution(trackData[1].ImgUrls, target),
                TrackImage.TakeNearestResolution(trackData[2].ImgUrls, target),
                TrackImage.TakeNearestResolution(trackData[3].ImgUrls, target)
                ];
    }
}

