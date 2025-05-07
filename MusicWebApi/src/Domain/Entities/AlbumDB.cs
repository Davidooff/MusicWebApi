using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MusicWebApi.src.Domain.Entities;

namespace MusicWebApi.src.Domain.Models;

public class AlbumDB
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; } = null!;

    public string? PlatformId { get; set; } = null!;

    public string Name { get; set; } = null!;

    public IdNameGroup[] Author { get; set; } = null!;

    public TrackInAlb[] Trackes { get; set; } = null!; 

    public ListeningStats TimesListened { get; set; } = new ListeningStats(0);

    public string? ImgUrl { get; set; }
}