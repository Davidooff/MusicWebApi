using MusicWebApi.src.Application.Entities;
using MusicWebApi.src.Application.Interfaces;

namespace MusicWebApi.src.Application.Services;

public class PlatformsService : PlatformManager
{
    private readonly YTMusicService _ytMusicService;
    public PlatformsService(ILogger<PlatformsService> logger)
    {
        _ytMusicService = new YTMusicService(logger);
    }

    private IPlatform ChoosePlatform(string platform)
    {

        if (!Enum.TryParse<EPlatform>(platform, out var idPlatform))
            throw PlatformManager.UnknownServiceExeption(platform);

        return idPlatform switch
        {
            EPlatform.YTMusic => _ytMusicService,
            _ => throw PlatformManager.UnknownServiceExeption(platform)
        };
    }


    public async Task<IEnumerable<TrackData>> Search(string query, string platformId) =>
        await ChoosePlatform(platformId).Search(query);


    public async Task<Stream> StreamTrack(string trackId, string platformId)
    {
        return await ChoosePlatform(platformId).StreamTrack(trackId);
    }
}

