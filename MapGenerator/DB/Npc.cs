using MongoDB.Bson;
using Newtonsoft.Json.Linq;
using SixLabors.ImageSharp;
using System.ComponentModel.DataAnnotations.Schema;

namespace MapGenerator.DB
{
    public class Npc : MapItem
    {
        public String Name { get; set; }
        public String NpcId { get; set; }
        public List<DBPoint> Positions { get; set; }
        public Boundary? Boundary { get; set; } = null;
        public List<String>? Says { get; set; }
        public String? QuestName { get; set; } = null;
        public int Width { get; set; }
        public int Height { get; set; }
        public bool Moves { get; set; } = false;
        public String? SkinFilename { get; set; }
        
        public virtual Item? TokenItem { get; set; } = null;
        public virtual List<Item> Items { get; set; } = new List<Item>();

        public static Npc? FromGenerator(JObject data, ObjectId mapId, Generator generator)
        {
            Npc npc = new Npc() { 
                MapId = mapId,
                Name = data.TryGetValue("name", out JToken? nameToken) && nameToken.Type == JTokenType.String ? nameToken.Value<String>() : data.Value<String>("id"),
                NpcId = data.Value<String>("id")
            };

            List<DBPoint> positions = new List<DBPoint>();

            if(data.TryGetValueAs("position", out JArray positionData))
            {
                positions.Add(new DBPoint() { 
                    X = positionData[0].ToObject<int>() - generator._MapMinX,
                    Y = positionData[1].ToObject<int>() - generator._MapMinY
                });
            }
            else if(data.TryGetValueAs("positions", out JArray positionsArr))
            {
                foreach(JArray pos in positionsArr.Cast<JArray>())
                {
                    positions.Add(new DBPoint()
                    {
                        X = pos[0].ToObject<int>() - generator._MapMinX,
                        Y = pos[1].ToObject<int>() - generator._MapMinY
                    });
                }
            }

            if(data.TryGetValueAs("boundary", out JArray boundaryData))
            {
                npc.Boundary = new Boundary()
                {
                    X1 = boundaryData[0].ToObject<int>() - generator._MapMinX,
                    Y1 = boundaryData[1].ToObject<int>() - generator._MapMinY,
                    X2 = boundaryData[2].ToObject<int>() - generator._MapMinX,
                    Y2 = boundaryData[3].ToObject<int>() - generator._MapMinY,
                };
            }

            if (positions.Count == 0)
                return null;

            npc.Positions = positions;

            return npc;
        }
    }
}
