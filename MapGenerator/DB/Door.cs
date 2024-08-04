using MongoDB.Bson;
using Newtonsoft.Json.Linq;
using System.ComponentModel.DataAnnotations.Schema;

namespace MapGenerator.DB
{
    public class Door : MapItem
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public String ToMapName { get; set; }
        public int FromSpawnId { get; set; }
        public int ToSpawnId { get; set; }
        public int ToX { get; set; }
        public int ToY { get; set; }

        public ObjectId? ToMapId { get; set; }
        public virtual Map? ToMap { get; set; }

        public static Door? FromGenerator(JArray data, ObjectId mapId, int minMapX, int minMapY)
        {
            if (data.Count < 7)
                return null;

            int x = data[0].ToObject<int>();
            int y = data[1].ToObject<int>();
            int w = data[2].ToObject<int>();
            int h = data[3].ToObject<int>();
            String mapName = data[4].ToObject<String>();
            int fromSpawn = data[5].ToObject<int>();
            int toSpawn = data[6].ToObject<int>();

            Door door = new Door() {
                MapId = mapId,
                X = x - minMapX - (w / 2),
                Y = y - minMapY - h,
                Width = w,
                Height = h,
                ToMapName = mapName,
                FromSpawnId = fromSpawn,
                ToSpawnId = toSpawn,
            };

            return door;
        }
    }
}
