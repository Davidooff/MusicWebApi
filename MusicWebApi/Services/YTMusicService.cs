using MusicWebApi.Interfaces;
using MusicWebApi.Models;
using YouTubeMusicAPI.Client;
using YouTubeMusicAPI.Models;
using YouTubeMusicAPI.Models.Search;
namespace MusicWebApi.Services;
public class YTMusicService //: IPlatform
{
    private readonly ILogger<YTMusicService> logger; // Fixes IDE0044: Make field readonly

    private readonly YouTubeMusicClient client;

    public YTMusicService(ILogger<YTMusicService> logger)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger)); // Fixes CS8618: Ensures logger is initialized
        client = new YouTubeMusicClient(logger); // Ensures client is initialized
    }

    private static TrackData Transform(SongSearchResult song)
    {
        List<(string id, string name)?> Artists = new();
        foreach (YouTubeMusicItem artist in song.Artists)
        {
            Artists.Add(artist.Id != null ? (artist.Id, artist.Name) : null);
        }


        return new TrackData()
        {
            Id = song.Id,
            Album = song.Album.Id != null ? 
                (song.Album.Id, song.Album.Name) : 
                throw new ArgumentException("Parameter cannot be null", nameof(song.Album)), 
            Artists = Artists.ToArray()
        };
    }

    public async Task<IEnumerable<TrackData>> Search(string query)
    {
        IEnumerable<SongSearchResult> searchResults = await client.SearchAsync<SongSearchResult>(query);
        return searchResults.Select(Transform);

        //IEnumerable<Shelf> searchResults = await client.SearchAsync(query);
    }

    //public async Task<Stream> StreamTrack(string trackId)
    //{

    //}
}

