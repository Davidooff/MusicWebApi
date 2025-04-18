using MusicWebApi.src.Application.Dto;
using MusicWebApi.src.Application.Interfaces;
using YouTubeMusicAPI.Client;
using YouTubeMusicAPI.Models;
using YouTubeMusicAPI.Models.Search;
using YouTubeMusicAPI.Models.Streaming;
namespace MusicWebApi.src.Application.Services;
public class YTMusicService : IPlatform
{
    private readonly ILogger logger; // Fixes IDE0044: Make field readonly

    private readonly YouTubeMusicClient client;

    public YTMusicService(ILogger logger)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger)); // Fixes CS8618: Ensures logger is initialized
        client = new YouTubeMusicClient(logger); // Ensures client is initialized
    }

    private static TrackData Transform(SongSearchResult song)
    {
        List<IdNameGroup?> ArtistsList = new();
        foreach (YouTubeMusicItem artist in song.Artists)
        {
            if (artist.Id is not null)
                ArtistsList.Add(new() { Id = artist.Id, Name = artist.Name });
        }

        // Fix for CS0029: Extract the URL from the Thumbnail object
        string? thumbnailUrl = song.Thumbnails.MaxBy(el => el.Width * el.Height)?.Url;

        // Fix for CS8601: Ensure thumbnailUrl is not null before assignment
        if (thumbnailUrl == null)
        {
            throw new ArgumentException("Thumbnails cannot be null or empty", nameof(song.Thumbnails));
        }

        return new TrackData()
        {
            Id = song.Id,
            Name = song.Name,
            Album = song.Album.Id != null ?
                new IdNameGroup() { Id = song.Album.Id, Name = song.Album.Name, ImgUrl = thumbnailUrl } :
                throw new ArgumentException("Parameter cannot be null", nameof(song.Album)),
            Artists = ArtistsList.ToArray()
        };
    }

    public async Task<IEnumerable<TrackData>> Search(string query)
    {
        IEnumerable<SongSearchResult> searchResults = await client.SearchAsync<SongSearchResult>(query);
        return searchResults.Select(Transform);
    }

    public async Task<Stream> StreamTrack(string trackId)
    {
        Console.WriteLine(trackId);
        StreamingData streamingData = await client.GetStreamingDataAsync(trackId);

        MediaStreamInfo highestAudioStreamInfo = streamingData.StreamInfo
          .OfType<AudioStreamInfo>()
          .OrderByDescending(info => info.Bitrate)
          .First();
        Stream stream = await highestAudioStreamInfo.GetStreamAsync();

        return stream;
    }

    private static void PrintObject(object obj)
    {
        Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(obj, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        }));
    }
}

