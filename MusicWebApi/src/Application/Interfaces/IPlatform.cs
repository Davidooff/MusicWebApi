

using MusicWebApi.src.Application.Entities;

namespace MusicWebApi.src.Application.Interfaces;

public interface IPlatform
{
    Task<IEnumerable<TrackData>> Search(string query);
    Task<Stream> StreamTrack(string trackId);
}

