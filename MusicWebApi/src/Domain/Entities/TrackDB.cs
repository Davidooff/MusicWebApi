using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MusicWebApi.src.Domain.Models;

public class TrackDB
{
    public string Name { get; set; } = null!;

    public (int total, int? lastWeek, int thisWeek) TimesListened { get; set; } = (0, null, 0);

}