using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class Playlist
    {
        public string Name { get; set; } = "New album";

        public IdNameGroup Owner { get; set; } = null!;

        public TrackData[] Track { get; set; } = Array.Empty<TrackData>();

        public Playlist(IdNameGroup owner, string? name)
        {
            Owner = owner;
            if (name is not null) Name = name;
        }
    }
}
