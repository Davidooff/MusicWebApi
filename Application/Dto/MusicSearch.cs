using Domain.Entities;

namespace Application.Dto;

public enum MusicSearchOptions
{
    Track,
    Playlist,
}

public class MusicSearch
{
    public string Search { get; set; } = null!;

    public EPlatform Platform { get; set; }
}

