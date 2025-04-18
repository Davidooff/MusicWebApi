namespace MusicWebApi.src.Api.Dto;

public enum MusicSearchOptions
{
    Track,
    Playlist,
}

public class MusicSearch
{
    public string search { get; set; } = null!;
    public MusicSearchOptions SearchOption { get; set; }
}

