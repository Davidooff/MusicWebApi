using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MusicWebApi.src.Domain.Entities;

public class TrackDB
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; } = null!;

    public string PlatformId { get; set; } = null!;
    public string Name { get; set; } = null!;

    public ListeningStats TimesListened { get; set; } = new ListeningStats (0);

    public TrackDB(string name, int listenings)
    {
        Name = name;
        TimesListened = new ListeningStats(listenings);
    }

}