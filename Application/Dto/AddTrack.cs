using Domain.Entities;

namespace Application.Dto;

public class AddTrack
{
    public string PlayListId { get; set; } = null!;
    public string TrackId { get; set; } = null!;
    public EPlatform EPlatform { get; set; }
}

