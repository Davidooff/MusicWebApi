using System.Xml.Linq;
using Domain.Entities;
using Microsoft.Extensions.Logging;
using YouTubeMusicAPI.Client;
using YouTubeMusicAPI.Models;
using YouTubeMusicAPI.Models.Search;
using YouTubeMusicAPI.Models.Streaming;

namespace Application.Services;

public class YTMusicService : IPlatform
{
    private readonly ILogger logger;

    private readonly YouTubeMusicClient _client;

    public YTMusicService(ILogger logger)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _client = new YouTubeMusicClient(logger); // Ensures client is initialized
    }

    private static TrackData Transform(SongSearchResult song)
    {
        List<IdNameGroup?> ArtistsList = new();
        foreach (YouTubeMusicItem artist in song.Artists)
        {
            if (artist.Id is not null)
                ArtistsList.Add(new( artist.Id,  artist.Name));
        }
       

        return new ()
        {
            Id = song.Id,
            Name = song.Name,
            AlbumId = song.Album.Id ?? "",
            EPlatform = EPlatform.YTMusic,
            Artists = ArtistsList.ToArray(),
            ImgUrls = song.Thumbnails.Select(el => new TrackImage(el.Url, el.Width * el.Height)).ToArray(),
            Duration = (int)song.Duration.TotalSeconds,
        };
    }

    public async Task<IEnumerable<TrackData>> Search(string query)
    {
        IEnumerable<SongSearchResult> searchResults = await _client.SearchAsync<SongSearchResult>(query);
        return searchResults.Select(Transform);
    }

    public async Task<AlbumDB?> GetAlbum(string albumId)
    {
        var browseId = await _client.GetAlbumBrowseIdAsync(albumId);
        var album = await _client.GetAlbumInfoAsync(browseId);

        if (album == null)
            return null;

        return new AlbumDB()
        {
            PlatformId = album.Id,
            Name = album.Name,
            Trackes = album.Songs
                .Select(el => new TrackInPlatformAlb(el.Id , el.Name) { Duration = (int)el.Duration.TotalSeconds })
                .ToArray(),
            Author = album.Artists.Select(el => 
                new IdNameGroup( el.Id ,  el.Name)).ToArray(),
            ImgUrl = album.Thumbnails.Last().Url,
        };
    }


    public async Task<Stream> StreamTrack(string trackId)
    {
        Console.WriteLine(trackId);
        StreamingData streamingData = await _client.GetStreamingDataAsync(trackId);

        MediaStreamInfo highestAudioStreamInfo = streamingData.StreamInfo
          .OfType<AudioStreamInfo>()
          .OrderByDescending(info => info.Bitrate)
          .First();
        Stream stream = await highestAudioStreamInfo.GetStreamAsync();

        return stream;
    }
}

