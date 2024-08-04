using Newtonsoft.Json.Linq;
using System.ComponentModel.DataAnnotations.Schema;

namespace MapGenerator.DB
{
    public class Item
    {
        public String ItemId { get; set; }
        public String Name { get; set; }
        public int Cost { get; set; }
        public String CurrencyType { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int Size { get; set; }
        public String ImageSetName { get; set; }

        public static async Task<Item?> FromGenerator(String itemId, Generator generator, String currecyType = "GOLD")
        {
            if (!generator._Items.TryGetValueAs(itemId, out JObject itemData))
                return null;

            Item item = new Item() {
                ItemId = itemId,
                Name = itemData.Value<String>("name"),
                Cost = itemData.Value<int>("g"),
                CurrencyType = currecyType,
                X = 9,
                Y = 24,
                Size = 20,
                Width = 320,
                Height = 1280,
                ImageSetName = "pack_20vt8"
            };

            String skinName = itemData.TryGetValue("skin", out JToken? skinToken) && skinToken.Type == JTokenType.String ? skinToken.Value<String>() : itemId;

            if(generator._Positions.TryGetValueAs(skinName, out JArray posRef) && posRef.Count > 2 && posRef[0].Type == JTokenType.String && posRef[1].Type == JTokenType.Integer)
            {
                String itemSetName = posRef[0].ToObject<String>();
                if (String.IsNullOrEmpty(itemSetName))
                    itemSetName = "pack_20";

                if(await generator._ImageSets.Get(itemSetName) is Generator.ImageSet imageSet)
                {
                    item.Size = imageSet.Size;
                    item.X = posRef[1].ToObject<int>();
                    item.Y = posRef[2].ToObject<int>();
                    item.ImageSetName = itemSetName;

                    if(generator._Images.TryGetValueAs(imageSet.FileName, out JObject img))
                    {
                        item.Width = img.Value<int>("width");
                        item.Height = img.Value<int>("height");
                    }
                }
            }

            return item;
        }
    }
}
