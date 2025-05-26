using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Domain.Entities;
using Domain.Options;

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
    
    public async Task CreateAsync(UserAlbumDB newUser) =>
        await _userAbumCollection.InsertOneAsync(newUser);

    public async Task CreateAsync(string userId) =>
        await _userAbumCollection.InsertOneAsync(new UserAlbumDB() { OwnerId = userId });

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
}

