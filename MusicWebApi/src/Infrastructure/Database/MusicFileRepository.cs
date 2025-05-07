using System;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using MusicWebApi.src.Application.Services;
using MusicWebApi.src.Domain.Entities;
using MusicWebApi.src.Domain.Options;

namespace MusicWebApi.src.Infrastructure.Database;

public class MusicFileRepository
{
    private readonly GridFSBucket _YTbucket;

    public MusicFileRepository(
        IOptions<DatabaseSettings> databaseSettings)
    {
        var mongoClient = new MongoClient(
            databaseSettings.Value.ConnectionString);

        var mongoDatabase = mongoClient.GetDatabase(
            databaseSettings.Value.DatabaseName);

        var options = new GridFSBucketOptions { BucketName = databaseSettings.Value.YTMusicBucket };
        _YTbucket = new GridFSBucket(mongoDatabase, options);
    }

    private GridFSBucket ChooseBucket (EPlatform platform) => platform switch { 
        EPlatform.YTMusic => _YTbucket, 
        _ => throw new ArgumentException("Invalid platform", nameof(platform)) 
    };

    public static string CreateID(string id, EPlatform platform)
    {
        return platform switch
        {
            EPlatform.YTMusic => "YT" + id,
            _ => throw new ArgumentException("Invalid platform", nameof(platform))
        };
    }

    public void UploadStreams(string id, Stream stream, EPlatform platform)
    {

        string searchId = CreateID(id, platform);
        var filter = Builders<GridFSFileInfo>.Filter.Eq(x => x.Filename, searchId);
        var _bucket = ChooseBucket(platform);
        var foundEl = _bucket.Find(filter).FirstOrDefault();

        if (foundEl != null)
        {
            stream.Close(); // Check is it works as expected 
            return;
        }

        _ = _bucket.UploadFromStreamAsync(searchId, stream);
    }

    public void UploadStreams((string id, Stream stream)[] fileStreams, EPlatform platform)
    {
        foreach (var (id, stream) in fileStreams) {
            string searchId = CreateID(id, platform);
            var filter = Builders<GridFSFileInfo>.Filter.Eq(x => x.Filename, searchId);
            var _bucket = ChooseBucket(platform);
            var foundEl = _bucket.Find(filter).FirstOrDefault();

            if (foundEl != null) {
                stream.Close(); // Check is it works as expected 
                continue;
            }

            _ = _bucket.UploadFromStreamAsync(searchId, stream);
        }   
    }

    public async Task<Stream?> DownloadStream(string id, EPlatform platform)
    {
        string searchId = CreateID(id, platform);
        var filter = Builders<GridFSFileInfo>.Filter.Eq(x => x.Filename, searchId);
        var _bucket = ChooseBucket(platform);
        var foundEl = _bucket.Find(filter).FirstOrDefault();
        if (foundEl == null)
        {
            return null;
        }
        return await _bucket.OpenDownloadStreamAsync(foundEl.Id);
    }

    public async Task<Stream> DownloadStream(GridFSFileInfo fileInfo, EPlatform platform)
    {
        var _bucket = ChooseBucket(platform);
        return await _bucket.OpenDownloadStreamAsync(fileInfo.Id);
    }

    public GridFSFileInfo? FindFileInfo(string id, EPlatform platform)
    {
        string searchId = CreateID(id, platform);
        var filter = Builders<GridFSFileInfo>.Filter.Eq(x => x.Filename, searchId);
        var _bucket = ChooseBucket(platform);
        return _bucket.Find(filter).FirstOrDefault();
    }
}

