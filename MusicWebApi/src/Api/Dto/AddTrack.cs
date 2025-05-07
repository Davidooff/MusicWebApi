using MusicWebApi.src.Domain.Entities;

namespace MusicWebApi.src.Api.Dto;
public class AddTrack
{
    public string PlayListId { get; set; } = null!;
    public string TrackId { get; set; } = null!;
    public EPlatform EPlatform { get; set; }
}

