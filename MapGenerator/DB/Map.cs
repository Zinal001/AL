using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MapGenerator.DB
{
    public class Map
    {
        [Key]
        public ObjectId Id { get; set; }

        [Required]
        [StringLength(255)]
        public String Name { get; set; }

        [StringLength(255)]
        public String DisplayName { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public int MinMapX { get; set; }
        public int MinMapY { get; set; }

        [Required]
        public ObjectId ServerId { get; set; }

        [ForeignKey(nameof(ServerId))]
        public virtual Server Server { get; set; }


        public ICollection<Quirk> Quirks { get; set; } = new List<Quirk>();
        public ICollection<Spawn> Spawns { get; set; } = new List<Spawn>();
        public ICollection<Door> Doors { get; set; } = new List<Door>();
        public ICollection<Zone> Zones { get; set; } = new List<Zone>();
        public ICollection<MonsterZone> Monsters { get; set; } = new List<MonsterZone>();
        public ICollection<Npc> Npcs { get; set; } = new List<Npc>();

        public Map LoadCollections(DataContext dataCtx)
        {
            Quirks = dataCtx.Quirks.Where(x => x.MapId == Id).ToList();
            Spawns = dataCtx.Spawns.Where(x => x.MapId == Id).ToList();
            Doors = dataCtx.Doors.Include(d => d.ToMap).Where(x => x.MapId == Id).ToList();
            Zones = dataCtx.Zones.Where(x => x.MapId == Id).ToList();
            Monsters = dataCtx.MonsterZones.Where(x => x.MapId == Id).ToList();
            Npcs = dataCtx.Npcs.Where(x => x.MapId == Id).ToList();

            return this;
        }


        public DateTime? LastGenerated { get; set; } = null;
    }
}
