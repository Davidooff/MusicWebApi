namespace Domain.Options;

public class DatabaseSettings
{
    public string ConnectionString { get; set; } = null!;

    public string DatabaseName { get; set; } = null!;

    public string UsersCollectionName { get; set; } = null!;

    public string YTTracks { get; set; } = null!;
    
    public string YTPlaylists { get; set; } = null!;

    public string YTMusicBucket { get; set; } = null!;

    public string UserAbums { get; set; } = null!;
}