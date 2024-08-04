using MongoDB.Bson;
using Newtonsoft.Json.Linq;

namespace MapGenerator.DB
{
    public class Zone : MapItem
    {
        public String Name { get; set; }
        public String Type { get; set; }
        public String Drop { get; set; }
        public List<DBPoint> Polygon { get; set; }


        public static Zone FromGenerator(JObject data, ObjectId mapId, int minMapX, int minMapY)
        {
            Zone zone = new Zone() { 
                MapId = mapId,
                Name = data.Value<String>("type"),
                Type = data.Value<String>("type"),
                Drop = data.Value<String>("drop")
            };

            List<DBPoint> polygons = new List<DBPoint>();

            foreach (JArray poly in data.Value<JArray>("polygon"))
                polygons.Add(new DBPoint() { X = poly[0].ToObject<int>() - minMapX, Y = poly[1].ToObject<int>() - minMapY });
            zone.Polygon = polygons;

            return zone;
        }
    }
}
