using MusicWebApi.Models;
using YouTubeMusicAPI.Models.Search;

namespace MusicWebApi.Interfaces;

public interface IPlatform
{
    Task<IEnumerable<TrackData>> Search(string query);
    //IEnumerable<TrackData> OpenAlbum(string id);
    Stream StreamTrack(string trackId);
}

