﻿using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using MusicWebApi.Data.Models;

namespace MusicWebApi.Data.Services;

public class MusicFileService
{
    private readonly GridFSBucket _bucket;


    public MusicFileService(
        IOptions<DatabaseSettings> databaseSettings)
    {
        var mongoClient = new MongoClient(
            databaseSettings.Value.ConnectionString);

        var mongoDatabase = mongoClient.GetDatabase(
            databaseSettings.Value.DatabaseName);

        _bucket = new GridFSBucket(mongoDatabase);
    }

    public async Task UploadStream(AlbumDB album, (string id, Stream stream)[] fileStreams, EPlatform platform)
    {
        foreach (var (id, stream) in fileStreams) {
            string search = id + platform switch { 
                EPlatform.YTMusic => "YT", 
                _ =>throw new ArgumentException("Invalid platform", nameof(platform)) };
            var foundEl = _bucket.Find(search).FirstOrDefault();
            if (foundEl != null) continue;

        }
    }
       

}

