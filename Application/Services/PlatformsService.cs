using Domain.Entities;
using Infrastructure.Database;
using Microsoft.Extensions.Logging;

namespace Application.Services;

public class PlatformsService
{
    private readonly YTMusicService _ytMusicService;
    private readonly MusicFileRepository _musicFileRepository;
    private readonly MusicRepository _musicRepository; 

    public PlatformsService(ILogger<PlatformsService> logger, MusicRepository musicRepository, MusicFileRepository musicFileRepository)
    {
        _ytMusicService = new YTMusicService(logger);
        _musicRepository = musicRepository ?? throw new ArgumentNullException(nameof(musicRepository));
        _musicFileRepository = musicFileRepository ?? throw new ArgumentNullException(nameof(musicFileRepository));
    }

    private IPlatform ChoosePlatform(EPlatform platform) =>
        platform switch
        {
            EPlatform.YTMusic => _ytMusicService,
            _ => throw new ArgumentException(nameof(platform), "Invalid platform")
        };

    public async Task<IEnumerable<TrackData>> Search(string query, EPlatform platformId) =>
        await ChoosePlatform(platformId).Search(query);

    public async Task<AlbumDB?> GetAlbum(string albumId, EPlatform platformId) =>
        await ChoosePlatform(platformId).GetAlbum(albumId);


    public async Task<Stream?> StreamTrack(string trackId, EPlatform platformId)
    {
        Stream? downloadedStream = await _musicFileRepository.DownloadStream(trackId, platformId);

        if ( downloadedStream is null)
            downloadedStream = await ChoosePlatform(platformId).StreamTrack(trackId);
            
        if (downloadedStream is Stream stream)
            return stream;

        return null;
    }

    public async Task<(Stream stream, bool isListeningAdded)?> ListenTrack(string trackId, EPlatform platformId)
    {
        Task<Stream?> downloadedStream = _musicFileRepository.DownloadStream(trackId, platformId);
        Task<bool> isAdded = _musicRepository.AddListening(trackId, platformId);

        if (await downloadedStream is null)
            downloadedStream = ChoosePlatform(platformId).StreamTrack(trackId);

        if (await downloadedStream is Stream stream)
            return (stream, await isAdded);

        return null;
    }
}

