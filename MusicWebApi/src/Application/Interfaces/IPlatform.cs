using MusicWebApi.src.Domain.Entities;
using MusicWebApi.src.Domain.Models;

namespace MusicWebApi.src.Application.Interfaces;

public interface IPlatform
{
    Task<IEnumerable<TrackData>> Search(string query);
    Task<Stream?> StreamTrack(string trackId);

    Task<AlbumDB?> GetAlbum(string albumId);

    //Task<TrackData?> GetTrack(string trackId);
}

