using Domain.Entities;

namespace Application.Services;

public interface PlatformManager
{
    public static ArgumentException UnknownServiceExeption(string platform) => 
        new ArgumentException(nameof(platform));
    abstract public Task<IEnumerable<TrackData>> Search(string query, string platformId);
    //IEnumerable<TrackData> OpenAlbum(string id);
    abstract public Task<Stream> StreamTrack(string trackId, string platformId);
}

