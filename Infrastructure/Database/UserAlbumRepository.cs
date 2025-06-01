using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Domain.Entities;
using Domain.Options;
using Org.BouncyCastle.Security;
using System.Xml.Linq;

namespace Infrastructure.Database;
public class UserAlbumRepository
{
    private readonly IMongoCollection<UserPlaylistDB> _userAbumCollection;

    public UserAlbumRepository(
        IOptions<DatabaseSettings> databaseSettings)
    {
        var mongoClient = new MongoClient(
            databaseSettings.Value.ConnectionString);

        var mongoDatabase = mongoClient.GetDatabase(
            databaseSettings.Value.DatabaseName);

        _userAbumCollection = mongoDatabase.GetCollection<UserPlaylistDB>(
            databaseSettings.Value.UserAbums);
    }


    public async Task<UserPlaylistDB?> GetAsync(string userId) =>
        await _userAbumCollection.Find(x =>
            x.Id == userId)
            .FirstOrDefaultAsync();

    public async Task CreateDefaultPlaylists(string userId, string userName)
    {
        UserPlaylistDB[] playlists = [
            new UserPlaylistDB(new(userId, userName), "Likes") { DefaultPlaylist = true }, 
            new UserPlaylistDB(new(userId, userName), "Dislikes") { DefaultPlaylist = true }, 
            new UserPlaylistDB(new(userId, userName), "History") { DefaultPlaylist = true }, 
            new UserPlaylistDB(new(userId, userName), "Saved") { DefaultPlaylist = true }];
        
        await _userAbumCollection.InsertManyAsync(playlists);
    }

    public async Task CreateAsync(string userId, string userName, string? name = null) =>
        await _userAbumCollection.InsertOneAsync(new UserPlaylistDB(new(userId, userName), name));

    public async Task RemovePlaylis(string id) =>
        await _userAbumCollection.DeleteOneAsync(x => x.Id == id);

    public async Task<bool> AddTrack(string albumId, TrackData trackData)
    {
        var _ = await _userAbumCollection.UpdateOneAsync(
            x => x.Id == albumId,
            Builders<UserPlaylistDB>.Update.Push(x => x.Track, trackData));
        return _.IsAcknowledged && _.ModifiedCount == 1;
    }

    public async Task<bool> RemoveTrack(string albumId, string trackId, EPlatform ePlatform)
    {
        var _ = await _userAbumCollection.UpdateOneAsync(
            x => x.Id == albumId,
            Builders<UserPlaylistDB>.Update.PullFilter(x => x.Track, x => x.Id == trackId && x.EPlatform == ePlatform));

        return _.IsAcknowledged && _.ModifiedCount == 1;
    }

    public async Task<List<UserPlaylistDB>?> GetUserAlbums(string userId)
    {
        return await (await _userAbumCollection.FindAsync(x => x.Owner.Id == userId)).ToListAsync();
    }

    public async Task<IEnumerable<PlaylistInfo>> GetUsersPlaylistsInfo(string userId)
    {
        var filter = Builders<UserPlaylistDB>.Filter.Eq(u => u.Owner.Id, userId);
        var userAlbums = await _userAbumCollection.FindAsync(filter);
        if (userAlbums == null)
            return [];

        return userAlbums.ToList().Select(x => 
            new PlaylistInfo (x.Id, x.Name) { 
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

