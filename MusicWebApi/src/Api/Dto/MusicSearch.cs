using MusicWebApi.src.Domain.Entities;

namespace MusicWebApi.src.Api.Dto;

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

