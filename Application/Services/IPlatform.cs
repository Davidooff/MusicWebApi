using Domain.Entities;


namespace Application.Services;

public interface IPlatform
{
    Task<IEnumerable<TrackData>> Search(string query);
    Task<Stream?> StreamTrack(string trackId);
    Task<AlbumDB?> GetAlbum(string albumId);
}

