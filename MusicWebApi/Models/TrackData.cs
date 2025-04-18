using YouTubeMusicAPI.Models;

namespace MusicWebApi.Models;

public class TrackData
{
    public string? Name { get; set; } = null!;

    //
    // Summary:
    //     The id of this song
    public string? Id { get; set; } = null!;

    //
    // Summary:
    //     The artist of this song
    public (string id, string name)?[] Artists { get; set; } = null!;

    //
    // Summary:
    //     The album of this song
    public (string id, string name)? Album { get; set; } = null!;
}

