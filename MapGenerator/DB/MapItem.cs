using MongoDB.Bson;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MapGenerator.DB
{
    public abstract class MapItem
    {
        [Key]
        public ObjectId Id { get; set; }

        public ObjectId MapId { get; set; }

        [ForeignKey(nameof(MapId))]
        public virtual Map Map { get; set; }
    }
}
