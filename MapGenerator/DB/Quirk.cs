using MongoDB.Bson;
using Newtonsoft.Json.Linq;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MapGenerator.DB
{
    public class Quirk : MapItem
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        [StringLength(255)]
        public String Type { get; set; }

        [StringLength(255)]
        public String? Text { get; set; }


        public static Quirk? FromGenerator(JArray data, ObjectId mapId, int mapMinX, int mapMinY)
        {
            if (data.Count < 5)
                return null;

            int x = data[0].ToObject<int>();
            int y = data[1].ToObject<int>();
            int w = data[2].ToObject<int>();
            int h = data[3].ToObject<int>();
            String type = data[4].ToObject<String>();

            Quirk q = new Quirk() { 
                MapId = mapId,
                X = x - mapMinX - (w / 2),
                Y = y - mapMinY - h,
                Width = w,
                Height = h,
                Type = type,
            };

            if (data.Count > 5 && data[5].Type == JTokenType.String)
                q.Text = data[5].ToObject<String>();

            if(String.IsNullOrEmpty(q.Text))
            {
                switch(q.Type)
                {
                    case "upgrade":
                        q.Text = "Upgrade Station";
                        break;
                    case "compound":
                        q.Text = "Compound Station";
                        break;
                    case "list_pvp":
                        q.Text = "PVP Leaderboard";
                        break;
                }
            }

            return q;
        }

    }
}
