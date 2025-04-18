using System;
using MusicWebApi.src.Application.Dto;
using MusicWebApi.src.Application.Interfaces;
using MusicWebApi.src.Infrastructure.Services;

namespace MusicWebApi.src.Application.Services;

public class PlatformsService : PlatformManager
{
    private YTMusicService _ytMusicService;
    public PlatformsService(ILogger<PlatformsService> logger)
    {
        _ytMusicService = new YTMusicService(logger);
    }

    private IPlatform ChoosePlatform(string platform)
    {

        if (!Enum.TryParse<EPlatform>(platform, out var idPlatform))
            throw UnknownServiceExeption(platform);

        return idPlatform switch
        {
            EPlatform.YTMusic => _ytMusicService,
            _ => throw UnknownServiceExeption(platform)
        };
    }


    override public async Task<IEnumerable<TrackData>> Search(string query, string platformId) =>
        await ChoosePlatform(platformId).Search(query);


    override public async Task<Stream> StreamTrack(string trackId, string platformId)
    {
        return await ChoosePlatform(platformId).StreamTrack(trackId);
    }
}

