using MongoDB.Driver;
using Domain.Entities;
using Domain.Options;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using MongoDB.Driver.Linq;
using Org.BouncyCastle.Asn1.Crmf;
using MongoDB.Bson;

namespace Infrastructure.Database;

public class MusicRepository
{
    private readonly IMongoCollection<AlbumDB> _ytAlbumsCollection;


    public MusicRepository(IOptions<DatabaseSettings> databaseSettings)
    {
        var mongoClient = new MongoClient(
            databaseSettings.Value.ConnectionString);

        var mongoDatabase = mongoClient.GetDatabase(
            databaseSettings.Value.DatabaseName);

        _ytAlbumsCollection = mongoDatabase.GetCollection<AlbumDB>(
            databaseSettings.Value.YTPlaylists);

    }

    protected IMongoCollection<AlbumDB> GetAlbumPlatform (EPlatform platform)=> _ytAlbumsCollection;


    /// <summary>
    /// Add listening to the track.
    /// </summary>
    /// <returns>Is el was found</returns>
    public async Task<bool> AddListening(string trackId, EPlatform platform)
    {
        var collection = GetAlbumPlatform(platform);
        var filter = Builders<AlbumDB>.Filter.ElemMatch(el => el.Trackes, trackes => trackes.Id == trackId);

        var result = await collection.Find(filter).FirstOrDefaultAsync();
        if (result == null)
        {
            return false; // Handle case where no matching document is found
        }

        result.TimesListened.AddOneListening();
        var trackRes = result.Trackes.First(el => el.Id == trackId);
        trackRes.TimesListened.AddOneListening();

        var updateFilter = Builders<AlbumDB>.Filter.Eq(el => el.Id, result.Id);
        var updateResult = await collection.ReplaceOneAsync(updateFilter, result);

        return updateResult.IsAcknowledged && updateResult.ModifiedCount > 0;
    }

    public async Task<bool> AddAlbum(AlbumDB album, EPlatform platform)
    {
        var collection = GetAlbumPlatform(platform);
        var filter = Builders<AlbumDB>.Filter.Eq(el => el.AlbumId, album.AlbumId);

        var existing = await collection.Find(filter).FirstOrDefaultAsync();
        if (existing != null)
        {
            album.Id = existing.Id; // Preserve the existing _id
        }

        var replaceOptions = new ReplaceOptions { IsUpsert = true };
        var updateResult = await collection.ReplaceOneAsync(filter, album, replaceOptions);

        return updateResult.IsAcknowledged && (updateResult.ModifiedCount > 0 || updateResult.UpsertedId != null);
    }
}

