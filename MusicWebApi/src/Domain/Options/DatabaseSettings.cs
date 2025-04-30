namespace MusicWebApi.src.Domain.Options;

public class DatabaseSettings
{
    public string ConnectionString { get; set; } = null!;

    public string DatabaseName { get; set; } = null!;

    public string UsersCollectionName { get; set; } = null!;

    public string YTMusicCollectionName { get; set; } = null!;
}