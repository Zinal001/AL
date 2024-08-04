using MongoDB.Bson;
using Newtonsoft.Json.Linq;

namespace MapGenerator.DB
{
    public class MonsterZone : MapItem
    {
        public String Name { get; set; }
        public String? SType { get; set; }
        public String Type { get; set; }
        public int Count { get; set; } = 1;
        public List<Boundary> Boundaries { get; set; }

        public bool Roams { get; set; } = false;
        public bool Grow { get; set; } = false;
        public String? SkinFilename { get; set; }
        public int SkinWidth { get; set; }
        public int SkinHeight { get; set; }


        public static async Task<MonsterZone?> FromGenerator(JObject data, ObjectId mapId, Generator generator)
        {
            MonsterZone zone = new MonsterZone() {
                MapId = mapId,
                Type = data.Value<String>("type"),
                Count = data.TryGetValue("count", out JToken? countToken) && countToken.Type == JTokenType.Integer ? countToken.Value<int>() : 1,
                SType = data.TryGetValue("stype", out JToken? stypeToken) && stypeToken.Type == JTokenType.String ? stypeToken.Value<String>() : null,
                Roams = data.TryGetValue("roam", out JToken? roamToken) && roamToken.Type == JTokenType.Boolean && roamToken.Value<bool>(),
                Grow = data.TryGetValue("grow", out JToken? growToken) && growToken.Type == JTokenType.Boolean && growToken.Value<bool>()
            };

            zone.Name = zone.Type;

            List<Boundary> boundaries = new List<Boundary>();

            if (data.TryGetValueAs("boundary", out JArray boundary))
            {
                boundaries.Add(new Boundary()
                {
                    X1 = boundary[0].ToObject<int>() - generator._MapMinX,
                    Y1 = boundary[1].ToObject<int>() - generator._MapMinY,
                    X2 = boundary[2].ToObject<int>() - generator._MapMinX,
                    Y2 = boundary[3].ToObject<int>() - generator._MapMinY,
                });
            }
            else if(data.TryGetValueAs("boundaries", out JArray boundariesArr))
            {
                foreach (JArray boundaryData in boundariesArr.Cast<JArray>())
                {
                    if (boundaryData[0].ToObject<String>() != generator._CurrentMap)
                        continue;

                    boundaries.Add(new Boundary() {
                        X1 = boundaryData[1].ToObject<int>() - generator._MapMinX,
                        Y1 = boundaryData[2].ToObject<int>() - generator._MapMinY,
                        X2 = boundaryData[3].ToObject<int>() - generator._MapMinX,
                        Y2 = boundaryData[4].ToObject<int>() - generator._MapMinY,
                    });
                }
            }

            if (boundaries.Count == 0)
                return null;
            zone.Boundaries = boundaries;

            if(generator._Monsters.TryGetValueAs(zone.Type, out JObject monsterData))
            {
                zone.Name = monsterData.Value<String>("name");

                String? monsterSkin = monsterData.Value<String>("skin");
                if(!String.IsNullOrEmpty(monsterSkin))
                {
                    if(await generator._Skins.Get(monsterSkin, 0, "monster") is Generator.Skin skin)
                    {
                        zone.SkinFilename = skin.Filename;
                        zone.SkinWidth = skin.Width;
                        zone.SkinHeight = skin.Height;
                    }
                }
            }

            return zone;
        }

    }
}
