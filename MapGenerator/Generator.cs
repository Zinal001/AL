using MapGenerator.DB;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using Newtonsoft.Json.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace MapGenerator
{
    public class Generator : IDisposable
    {
        internal String _CurrentMap;
        internal int _MapMinX;
        internal int _MapMinY;
        internal int _MapWidth;
        internal int _MapHeight;

        internal ObjectId _ServerId;
        internal String _ServerUrl;
        private JObject _G;
        internal JObject _Maps;
        internal JObject _Geometry;
        internal JObject _Tilesets;
        internal JObject _Skills;
        internal JObject _Npcs;
        internal JObject _GSprites;
        internal JObject _Items;
        internal JObject _Positions;
        internal JObject _GImageSets;
        internal JObject _Images;
        internal JObject _Tokens;
        internal JObject _Monsters;
        internal JObject _Dimensions;

        internal String _ResourceFolder;
        private Tiles _Tiles;
        private TileSets _TileSets;
        internal ImageSets _ImageSets;
        private Sprites _Sprites;
        internal Skins _Skins;

        public Generator(String serverKey)
        {
            Server? server = DataContext.Instance.Servers.FirstOrDefault(s => s.ServerKey == serverKey);
            if (server == null)
                throw new Exception("Server not found");

            _ServerId = server.Id;
            _ServerUrl = server.Url;

            if (!_ServerUrl.EndsWith('/'))
                _ServerUrl += '/';

            _ResourceFolder = Path.Combine("/resources", $"Server_{_ServerId}");
            Directory.CreateDirectory(_ResourceFolder);

            _Tiles = new Tiles(this);
            _TileSets = new TileSets(this);
            _ImageSets = new ImageSets(this);
            _Sprites = new Sprites(this);
            _Skins = new Skins(this);
        }

        public async Task<bool> Initialize()
        {
            if (_G != null)
                return true;

            using (HttpClient client = new HttpClient())
            {
                String gJson = await client.GetStringAsync($"{_ServerUrl}data.js");
                if (String.IsNullOrEmpty(gJson))
                {
                    Console.WriteLine($"No G json?");
                    return false;
                }

                int firstBracket = gJson.IndexOf('{');
                int lastBracket = gJson.LastIndexOf('}');

                gJson = gJson.Substring(firstBracket, lastBracket - firstBracket + 1);

                _G = JObject.Parse(gJson);
                _Geometry = _G.Value<JObject>("geometry");
                _Tilesets = _G.Value<JObject>("tilesets");
                _Maps = _G.Value<JObject>("maps");
                _Skills = _G.Value<JObject>("skills");
                _Npcs = _G.Value<JObject>("npcs");
                _GSprites = _G.Value<JObject>("sprites");
                _Items = _G.Value<JObject>("items");
                _Positions = _G.Value<JObject>("positions");
                _GImageSets = _G.Value<JObject>("imagesets");
                _Images = _G.Value<JObject>("images");
                _Tokens = _G.Value<JObject>("tokens");
                _Monsters = _G.Value<JObject>("monsters");
                _Dimensions = _G.Value<JObject>("dimensions");
            }

            return true;
        }

        public async Task<String> DownloadFile(String type, String name, String? sourceName)
        {
            String filename = Path.Combine(_ResourceFolder, type, $"{name}.png");
            Directory.CreateDirectory(Path.Combine(_ResourceFolder, type));

            FileInfo fi = new FileInfo(filename);
            if (fi.Exists && DateTime.UtcNow.Subtract(fi.LastWriteTimeUtc).TotalDays < 1d)
                return filename;

            using (HttpClient client = new HttpClient())
            {
                using (var sourceStream = await client.GetStreamAsync($"{_ServerUrl}{sourceName.TrimStart('/')}"))
                {
                    using (var targetStream = new FileStream(filename, FileMode.Create, FileAccess.Write))
                    {
                        await sourceStream.CopyToAsync(targetStream);
                    }
                }
            }

            return filename;
        }

        public async Task GenerateAllMaps()
        {
            if (!await Initialize())
                return;

            JProperty[] mapProps = _Maps.Properties().ToArray();
            int i = 0;
            int j = mapProps.Length;
            foreach(JProperty prop in mapProps)
            {
                i++;
                Console.WriteLine($"\t[{i}/{j}] Generating map {prop.Name}");
                if (prop.Name == "test")
                {
                    Console.WriteLine("\t\tIgnoring map");
                    continue;
                }
                await GenerateMap(prop.Name);
            }

            var allSpawns = DataContext.Instance.Spawns.WithMap().Where(s => s.Map.ServerId == _ServerId).ToArray();
            var allDoors = DataContext.Instance.Doors.WithMap().Where(s => s.Map.ServerId == _ServerId).ToArray();
            //Console.WriteLine($"Spawns: {allSpawns.Length}");
            //Console.WriteLine($"Doors: {allDoors.Length}");

            foreach (var spawn in allSpawns)
                spawn.Connections.Clear();

            foreach (Door door in allDoors)
            {
                if (door.ToMapId == null)
                {
                    if (DataContext.Instance.Maps.FirstOrDefault(m => m.ServerId == _ServerId && m.Name == door.ToMapName) is Map toMap)
                    {
                        door.ToMapId = toMap.Id;
                        DataContext.Instance.Doors.Update(door);
                    }
                }

                if (allSpawns.FirstOrDefault(s => s.Map.Name == door.ToMapName && s.SpawnId == door.FromSpawnId) is Spawn spawn)
                {
                    spawn.Connections.Add(door.Map.DisplayName);
                    DataContext.Instance.Spawns.Update(spawn);

                    door.ToSpawnId = spawn.SpawnId;
                    door.ToX = spawn.X;
                    door.ToY = spawn.Y;
                    DataContext.Instance.Doors.Update(door);
                }
            }

            foreach (Spawn spawn in allSpawns)
            {
                if (spawn.SpawnId == 0 && !spawn.Connections.Contains("Town skill location"))
                {
                    spawn.Connections.Add("Town skill location");
                    DataContext.Instance.Spawns.Update(spawn);
                }
            }


            await DataContext.Instance.SaveChangesAsync();
        }

        public async Task GenerateMap(String mapName)
        {
            if (!await Initialize())
            {
                Console.WriteLine("\t\tError: Unable to initialize G");
                return;
            }

            if (!_Geometry.TryGetValueAs(mapName, out JObject? geo))
            {
                Console.WriteLine("\t\tError: G.geometry doesn't contain map");
                return;
            }

            if (!_Maps.TryGetValueAs(mapName, out JObject mapData))
            {
                Console.WriteLine("\t\tError: G.maps doesn't contain map");
                return;
            }

            if (mapData.TryGetValue("ignore", out JToken? ignoreToken) && ignoreToken.Type == JTokenType.Boolean && ignoreToken.Value<bool>())
            {
                Console.WriteLine("\t\tIgnoring map (ignore-token in G.maps)");
            }

            _Tiles.Clear();

            _CurrentMap = mapName;

            _MapMinX = geo.Value<JToken>("min_x").ToObject<int>();
            _MapMinY = geo.Value<JToken>("min_y").ToObject<int>();
            _MapWidth = geo.Value<JToken>("max_x").ToObject<int>() - _MapMinX;
            _MapHeight = geo.Value<JToken>("max_y").ToObject<int>() - _MapMinY;

            Image mapImage = new Image<Rgb24>(_MapWidth, _MapHeight);

            if(geo.TryGetValue("default", out JToken? defaultTileNr) && defaultTileNr.Type == JTokenType.Integer)
            {
                if(await _Tiles.GetTile(defaultTileNr.ToObject<int>()) is Tile defaultTile)
                {
                    for (int y = 0; y < _MapHeight; y += 16)
                    {
                        for (int x = 0; x < _MapWidth; x += 16)
                        {
                            defaultTile.DrawOn(mapImage, x, y);
                        }
                    }
                }
            }

            if(geo.TryGetValueAs("placements", out JArray? placements))
            {
                for(int i = 0; i < placements.Count; i++)
                {
                    if (placements[i] is JArray data && await Placement.GetPlacement(data, this) is Placement p)
                        p.DrawOn(mapImage, this);
                }
            }

            if(geo.TryGetValueAs("groups", out JArray? groups))
            {
                foreach(JArray dataArr in groups)
                {
                    foreach(Group g in await Group.GetGroups(dataArr, this))
                        g.DrawOn(mapImage, this);
                }
            }

            Directory.CreateDirectory(Path.Combine(_ResourceFolder, "Maps"));
            await mapImage.SaveAsPngAsync(Path.Combine(_ResourceFolder, "Maps", $"_{mapName}.png"), new PngEncoder() { ColorType = PngColorType.Rgb, CompressionLevel = PngCompressionLevel.BestCompression });
            mapImage.Dispose();
            File.Move(Path.Combine(_ResourceFolder, "Maps", $"_{mapName}.png"), Path.Combine(_ResourceFolder, "Maps", $"{mapName}.png"), true);


            Image boundsImage = new Image<Rgba32>(_MapWidth, _MapHeight);

            if (geo.TryGetValueAs<JArray>("y_lines", out JArray? ylines))
            {
                foreach (JArray yline in ylines)
                {
                    if (YLines.Get(yline) is YLines yl)
                        yl.DrawOn(boundsImage, this);
                }
            }

            if (geo.TryGetValueAs<JArray>("x_lines", out JArray? xlines))
            {
                foreach (JArray xline in xlines)
                {
                    if (XLines.Get(xline) is XLines xl)
                        xl.DrawOn(boundsImage, this);
                }
            }

            await boundsImage.SaveAsPngAsync(Path.Combine(_ResourceFolder, "Maps", $"_{mapName}_Bounds.png"));
            boundsImage.Dispose();
            File.Move(Path.Combine(_ResourceFolder, "Maps", $"_{mapName}_Bounds.png"), Path.Combine(_ResourceFolder, "Maps", $"{mapName}_Bounds.png"), true);

            using (DataContext dbCtx = DataContext.NewContext())
            {
                Server? server = dbCtx.Servers.FirstOrDefault(s => s.Id == _ServerId);
                if (server == null)
                    return;

                var map = dbCtx.Maps.FirstOrDefault(m => m.ServerId == _ServerId && m.Name == mapName);
                if(map == null)
                {
                    map = new Map() { 
                        Name = mapName,
                        DisplayName = mapData.TryGetValue("name", out JToken? mapNameToken) ? mapNameToken.Value<String>() : mapName,
                        ServerId = _ServerId,
                    };
                    dbCtx.Maps.Add(map);
                    await dbCtx.SaveChangesAsync();
                }

                map.Width = _MapWidth;
                map.Height = _MapHeight;
                map.MinMapX = _MapMinX;
                map.MinMapY = _MapMinY;

                dbCtx.Quirks.RemoveRange(dbCtx.Quirks.Where(q => q.MapId == map.Id));
                dbCtx.Spawns.RemoveRange(dbCtx.Spawns.Where(s => s.MapId == map.Id));
                dbCtx.Doors.RemoveRange(dbCtx.Doors.Where(s => s.MapId == map.Id));
                dbCtx.Zones.RemoveRange(dbCtx.Zones.Where(s => s.MapId == map.Id));
                dbCtx.MonsterZones.RemoveRange(dbCtx.MonsterZones.Where(s => s.MapId == map.Id));
                //dbCtx.Items.RemoveRange(dbCtx.Items.Where(s => s.ServerId == _ServerId));
                dbCtx.Npcs.RemoveRange(dbCtx.Npcs.Where(s => s.MapId == map.Id));

                if (mapData.TryGetValueAs("quirks", out JArray quirks))
                {
                    foreach (JArray quirkData in quirks.Cast<JArray>())
                    {
                        if (Quirk.FromGenerator(quirkData, map.Id, _MapMinX, _MapMinY) is Quirk q)
                            dbCtx.Quirks.Add(q);
                    }
                }

                if (mapData.TryGetValueAs("spawns", out JArray spawns))
                {
                    int spawnId = 0;
                    foreach (JArray spawnData in spawns.Cast<JArray>())
                    {
                        Spawn s = new Spawn()
                        {
                            MapId = map.Id,
                            X = spawnData[0].ToObject<int>() - _MapMinX,
                            Y = spawnData[1].ToObject<int>() - _MapMinY,
                            SpawnId = spawnId
                        };
                        dbCtx.Spawns.Add(s);
                        spawnId++;
                    }
                }

                if (mapData.TryGetValueAs("doors", out JArray doors))
                {
                    foreach (JArray doorData in doors.Cast<JArray>())
                    {
                        if (Door.FromGenerator(doorData, map.Id, _MapMinX, _MapMinY) is Door door)
                        {
                            if (dbCtx.Spawns.FirstOrDefault(s => s.MapId == map.Id && s.SpawnId == door.ToSpawnId) is Spawn outSpawn)
                            {
                                door.ToX = outSpawn.X;
                                door.ToY = outSpawn.Y;
                            }

                            dbCtx.Doors.Add(door);
                        }
                    }
                }

                if (mapData.TryGetValueAs("zones", out JArray zones))
                {
                    foreach (JObject zoneData in zones.Cast<JObject>())
                    {
                        if (Zone.FromGenerator(zoneData, map.Id, _MapMinX, _MapMinY) is Zone zone)
                        {
                            if (_Skills.TryGetValueAs(zone.Type, out JObject typeObj))
                                zone.Name = typeObj.Value<String>("name");

                            dbCtx.Zones.Add(zone);
                        }
                    }
                }

                if (mapData.TryGetValueAs("monsters", out JArray monsters))
                {
                    foreach (JObject monsterData in monsters.Cast<JObject>())
                    {
                        if (await MonsterZone.FromGenerator(monsterData, map.Id, this) is MonsterZone mz)
                            dbCtx.MonsterZones.Add(mz);
                    }
                }

                if (mapData.TryGetValueAs("npcs", out JArray npcs))
                {
                    List<Npc> extraNpcs = new List<Npc>();
                    int extraNpcX = 0;
                    int extraNpcY = 0;

                    foreach (JObject npcData in npcs.Cast<JObject>())
                    {
                        if (Npc.FromGenerator(npcData, map.Id, this) is Npc npc)
                        {
                            dbCtx.Npcs.Add(npc);

                            if(_Npcs.TryGetValueAs<JObject>(npc.NpcId, out JObject npcD))
                            {
                                npc.Name = npcD.Value<String>("name");

                                if(npcD.TryGetValue("says", out JToken? saysToken))
                                {
                                    if (saysToken.Type == JTokenType.String)
                                        npc.Says = [saysToken.ToObject<String>()];
                                    else if(saysToken.Type == JTokenType.Array)
                                        npc.Says = [.. ((JArray)saysToken).Select(v => (string)v)];
                                }

                                if(npcD.TryGetValueAs("items", out JArray itemsArr))
                                {
                                    foreach (String itemId in itemsArr)
                                    {
                                        if (String.IsNullOrEmpty(itemId))
                                            continue;

                                        Item? item = null;
                                        //Item? item = dbCtx.Items.FirstOrDefault(i => i.ItemId == itemId && i.ServerId == _ServerId && i.CurrencyType == "GOLD");

                                        if (item == null)
                                        {
                                            if (await Item.FromGenerator(itemId, this) is Item newItem)
                                            {
                                                item = newItem;
                                                //dbCtx.Items.Add(item);
                                                npc.Items.Add(item);
                                            }
                                            else
                                                continue;
                                        }
                                        else
                                            npc.Items.Add(item);
                                    }
                                }

                                if(npcD.TryGetValue("token", out JToken? token))
                                {
                                    String tokenType = token.ToObject<String>();

                                    Item? tokenItem = null;
                                    //Item? tokenItem = dbCtx.Items.FirstOrDefault(i => i.ItemId == tokenType && i.ServerId == _ServerId && i.CurrencyType == "TOKEN");

                                    if (tokenItem == null)
                                    {
                                        if (await Item.FromGenerator(tokenType, this, "TOKEN") is Item newTokenItem)
                                        {
                                            tokenItem = newTokenItem;
                                            //dbCtx.Items.Add(tokenItem);
                                            npc.TokenItem = tokenItem;
                                        }
                                    }
                                    else
                                        npc.TokenItem = tokenItem;

                                    if(_Tokens.TryGetValueAs(tokenType, out JObject tokenRef))
                                    {
                                        foreach(JProperty prop in tokenRef.Properties())
                                        {
                                            Item? item = null;
                                            //Item? item = dbCtx.Items.FirstOrDefault(i => i.ItemId == prop.Name && i.ServerId == _ServerId && i.CurrencyType == tokenType);

                                            if (item == null)
                                            {
                                                if (await Item.FromGenerator(prop.Name, this, tokenType) is Item newItem)
                                                {
                                                    item = newItem;
                                                    item.Cost = prop.Value<JToken>().ToObject<int>();
                                                    //dbCtx.Items.Add(item);
                                                    npc.Items.Add(item);
                                                }
                                                else if (prop.Name.Contains('-') && await Item.FromGenerator(prop.Name.Split('-')[0], this, tokenType) is Item newItem2)
                                                {
                                                    item = newItem2;
                                                    item.Cost = prop.Value<JToken>().ToObject<int>();
                                                    //dbCtx.Items.Add(item);
                                                    npc.Items.Add(item);
                                                }
                                                else
                                                    continue;
                                            }
                                            else
                                                npc.Items.Add(item);
                                        }
                                    }
                                }

                                if (npcD.TryGetValue("quest", out JToken? questToken))
                                    npc.QuestName = questToken.ToObject<String>();

                                if(npcD.TryGetValue("skin", out JToken? skinToken))
                                {
                                    if(await _Skins.Get(skinToken.Value<String>(), 0, "npc") is Skin skin)
                                    {
                                        npc.SkinFilename = skin.Filename;
                                        npc.Width = skin.Width;
                                        npc.Height = skin.Height;

                                        foreach(var pos in npc.Positions)
                                        {
                                            pos.X -= npc.Width / 2;
                                            pos.Y -= npc.Height;
                                        }
                                    }
                                }

                                if(npcD.ContainsKey("speed") && npcD.ContainsKey("delay") && npc.Positions.Count > 0)
                                {
                                    npc.Moves = true;
                                    npc.Positions[0].X = extraNpcX - _MapMinX;
                                    npc.Positions[0].Y = extraNpcY - _MapMinY;
                                    extraNpcX += npc.Width + 5;
                                    extraNpcs.Add(npc);
                                }
                            }
                        }
                    }

                    if(extraNpcs.Count > 0)
                    {
                        int w = (extraNpcX - 5) / 2;
                        foreach (Npc npc in extraNpcs)
                            npc.Positions[0].X -= w;
                    }

                }

                map.LastGenerated = DateTime.Now;
                server.LastGenerated = DateTime.Now;
                await dbCtx.SaveChangesAsync();
            }
        }

        private class Tiles(Generator generator)
        {
            private Dictionary<int, Tile> _Tiles = new Dictionary<int, Tile>();

            public async Task<Tile?> GetTile(int index)
            {
                if (_Tiles.TryGetValue(index, out Tile value))
                    return value;

                if (!generator._Geometry.TryGetValueAs(generator._CurrentMap, out JObject? geo))
                    return null;

                if (!geo.TryGetValueAs("tiles", out JArray? tiles))
                    return null;

                if (index < 0 || index >= tiles.Count)
                    return null;

                if (tiles[index] is not JArray tileData)
                    return null;

                TileSet? tileSet = await generator._TileSets.Get(tileData[0].ToObject<String>());
                if (tileSet == null)
                    return null;

                int x = tileData[1].ToObject<int>();
                int y = tileData[2].ToObject<int>();
                int w = tileData[3].ToObject<int>();
                int h = w;

                if(tileData.Count > 4 && tileData[4].Type == JTokenType.Integer)
                    h = tileData[4].ToObject<int>();

                Tile tile = new Tile() { 
                    X = x,
                    Y = y,
                    Width = w,
                    Height = h,
                    Set = tileSet.Value
                };

                _Tiles[index] = tile;
                return _Tiles[index];
            }

            public void Clear()
            {
                _Tiles.Clear();
            }
        }

        private class Sprites(Generator generator)
        {
            private Dictionary<String, Sprite> _Sprites = new Dictionary<string, Sprite>();
            private Dictionary<String, String> _SkinsRef = new Dictionary<string, string>();

            public async Task<Sprite?> Find(String skinName)
            {
                Sprite foundSprite = _Sprites.Values.FirstOrDefault(s => s.Contains(skinName));
                if (foundSprite.Matrix != null)
                    return foundSprite;

                if (_SkinsRef.TryGetValue(skinName, out String? spriteName))
                    return await Get(spriteName);

                foreach(JProperty prop in generator._GSprites.Properties())
                {
                    if(prop.Value is JObject s)
                    {
                        String?[][]? matrix = s.Value<JToken?>("matrix")?.ToObject<String?[][]>();

                        foreach (String[] row in matrix)
                        {
                            foreach(String name in row)
                            {
                                if (name == skinName)
                                    return await Get(prop.Name);
                                else if (!String.IsNullOrEmpty(name))
                                    _SkinsRef[name] = prop.Name;
                            }
                        }
                    }
                }

                return null;
            }

            public async Task<Sprite?> Get(String name)
            {
                if (_Sprites.TryGetValue(name, out Sprite outSprite))
                    return outSprite;

                if(!generator._GSprites.TryGetValueAs(name, out JObject ts))
                    return null;

                try
                {
                    String filename = await generator.DownloadFile("Sprites", name, ts.Value<String>("file"));

                    Sprite sprite = new Sprite() {
                        Rows = ts.Value<int>("rows"),
                        Columns = ts.Value<int>("columns"),
                        Type = ts.TryGetValue("type", out JToken? typeToken) && typeToken.Type == JTokenType.String ? typeToken.Value<String>() : null,
                        Matrix = ts.Value<JToken>("matrix").ToObject<String?[][]>(),
                        Image = Image.Load(filename)
                    };

                    _Sprites[name] = sprite;

                    return sprite;
                }
                catch { }

                return null;
            }

            public void Clear()
            {
                foreach (var pair in _Sprites)
                    pair.Value.Image.Dispose();

                _Sprites.Clear();
                _SkinsRef.Clear();
            }

            public void Destroy()
            {
                String dirName = Path.Combine(generator._ResourceFolder, "Sprites");
                if (Directory.Exists(dirName))
                    Directory.Delete(dirName, true);

                Clear();
            }

        }

        private class TileSets(Generator generator)
        {
            private Dictionary<String, TileSet> _Sets = new Dictionary<string, TileSet>();

            public async Task<TileSet?> Get(String name)
            {
                if (_Sets.TryGetValue(name, out TileSet value))
                    return value;

                if (!generator._Tilesets.TryGetValue(name, out JToken? ts))
                    return null;

                try
                {

                    String filename = await generator.DownloadFile("Sheets", name, ts.Value<String>("file"));

                    TileSet tileSet = new TileSet();
                    tileSet.Filename = filename;
                    tileSet.Image = await Image.LoadAsync(filename);

                    _Sets[name] = tileSet;
                    return _Sets[name];
                }
                catch { }

                return null;
            }

            public void Clear()
            {
                foreach(String key in _Sets.Keys)
                {
                    _Sets[key].Image.Dispose();
                }

                _Sets.Clear();
            }

            public void Destroy()
            {
                String dirName = Path.Combine(generator._ResourceFolder, "Sheets");
                if (Directory.Exists(dirName))
                    Directory.Delete(dirName, true);

                Clear();
            }
        }

        internal class ImageSets(Generator generator)
        {
            private Dictionary<String, ImageSet> _Sets = new Dictionary<string, ImageSet>();

            public async Task<ImageSet?> Get(String name)
            {
                if (_Sets.TryGetValue(name, out ImageSet outSet))
                    return outSet;

                if (!generator._GImageSets.TryGetValueAs(name, out JObject setData))
                    return null;

                try
                {
                    String filename = await generator.DownloadFile("ImageSets", name, setData.Value<String>("file"));

                    ImageSet set = new ImageSet() { 
                        Rows = setData.Value<int>("rows"),
                        Columns = setData.Value<int>("columns"),
                        Size = setData.Value<int>("size"),
                        File = filename,
                        Url = $"images/imagesets/{name}",
                        FileName = setData.Value<String>("file").Split('?')[0]
                    };

                    _Sets[name] = set;

                    return set;

                }
                catch { }

                return null;
            }

            public void Clear()
            {
                _Sets.Clear();
            }
        }

        internal class Skins(Generator generator)
        {
            private Dictionary<String, Skin> _Skins = new Dictionary<string, Skin>();

            public async Task<Skin?> Get(String name, int direction, String type = "npc")
            {
                String skinKey = $"{name}_{direction}_{type}";
                if (_Skins.TryGetValue(skinKey, out Skin oldSkin))
                    return oldSkin;

                if (await generator._Sprites.Find(name) is not Sprite sprite)
                    return null;

                int multiX = 3;
                int multiY = sprite.Type == "animation" ? 1 : 4;

                int spriteWidth = sprite.Image.Width / (sprite.Columns * multiX);
                int spriteHeight = sprite.Image.Height / (sprite.Rows * multiY);

                String filename = Path.Combine(generator._ResourceFolder, "Skins", $"{skinKey}.png");
                Directory.CreateDirectory(Path.Combine(generator._ResourceFolder, "Skins"));

                if(sprite.GetSkinPosition(name) is int[] spritePos)
                {
                    int yOffset = direction * spriteHeight;

                    Rectangle srcRect = new Rectangle((spritePos[1] * spriteWidth * multiX) + spriteWidth, spritePos[0] * spriteHeight * multiY + yOffset, spriteWidth, spriteHeight);

                    Skin skin = new Skin() { 
                        Filename = skinKey,
                        Width = spriteWidth,
                        Height = spriteHeight,
                        Image = new Image<Rgba32>(spriteWidth, spriteHeight)
                    };

                    skin.Image.Mutate(ctx => {
                        ctx.DrawImage(sprite.Image, new Point(0, 0), srcRect, 1);

                        if (generator._Dimensions.TryGetValueAs(name, out JArray dimArr) && dimArr.Count > 1)
                        {
                            try
                            {
                                int dW = dimArr[0].Value<int>();
                                int dH = dimArr[1].Value<int>();
                                int dX = 0;

                                if (dimArr.Count > 2)
                                    dX = dimArr[2].Value<int>();

                                int cX = Math.Max((srcRect.Width - dW) / 2 + dX, 0);
                                int cY = Math.Max(srcRect.Height - dH, 0);

                                ctx.Crop(new Rectangle(cX, cY, dW, dH));

                                skin.Width = dW;
                                skin.Height = dH;
                            }
                            catch { }
                        }
                    });

                    skin.Image.Save(filename);

                    _Skins[skinKey] = skin;
                    return skin;
                }

                return null;
            }

            public void Clear()
            {
                foreach(var skin in _Skins)
                    skin.Value.Image.Dispose();
                _Skins.Clear();
            }
        }

        internal struct Skin
        {
            public String Filename;
            public int Width;
            public int Height;

            public Image Image;
        }

        private struct Tile
        {
            public int X;
            public int Y;
            public int Width;
            public int Height;
            public TileSet Set;

            public readonly void DrawOn(Image image, int x, int y)
            {
                Tile t = this;
                image.Mutate(ctx => {
                    ctx.DrawImage(t.Set.Image, new Point(x, y), new Rectangle(t.X, t.Y, t.Width, t.Height), 1f);
                });
            }
        }

        private struct Sprite
        {
            public int Rows;
            public int Columns;
            public String? Type;
            public String?[][] Matrix;

            public Image Image;

            public readonly bool Contains(String skinName)
            {
                if (Matrix == null || Matrix.Length == 0)
                    return false;

                for(int y = 0; y < Matrix.Length; y++)
                {
                    for(int x = 0; x < Matrix[y].Length; x++)
                    {
                        if (Matrix[y][x] == skinName)
                            return true;
                    }
                }

                return false;
            }

            public readonly int[]? GetSkinPosition(String skinName)
            {
                for (int y = 0; y < Matrix.Length; y++)
                {
                    for (int x = 0; x < Matrix[y].Length; x++)
                    {
                        if (Matrix[y][x] == skinName)
                            return [y, x];
                    }
                }

                return null;
            }
        }

        private struct TileSet
        {
            public String Filename;
            public Image Image;
        }

        private struct Placement
        {
            public int TileIndex;
            public int X1;
            public int Y1;
            public int X2;
            public int Y2;
            public Tile Tile;

            public static async Task<Placement?> GetPlacement(JArray data, Generator generator)
            {
                if (data.Count < 3)
                    return null;

                Placement p = new Placement() { 
                    TileIndex = data[0].ToObject<int>(),
                    X1 = data[1].ToObject<int>(),
                    Y1 = data[2].ToObject<int>(),
                };

                if(data.Count > 3)
                {
                    p.X2 = data[3].ToObject<int>();
                    p.Y2 = data[4].ToObject<int>();
                }
                else
                {
                    p.X2 = p.X1;
                    p.Y2 = p.Y1;
                }

                if(await generator._Tiles.GetTile(p.TileIndex) is Tile tile)
                {
                    p.Tile = tile;
                    return p;
                }

                return null;
            }

            public readonly void DrawOn(Image image, Generator generator)
            {
                int numTilesX = (X2 - X1) / 16;
                int numTilesY = (Y2 - Y1) / 16;

                for(int y = 0; y <= numTilesY; y++)
                {
                    for(int x = 0; x <= numTilesX; x++)
                    {
                        int mapX = X1 + (x * 16) - generator._MapMinX;
                        int mapY = Y1 + (y * 16) - generator._MapMinY;
                        Tile.DrawOn(image, mapX, mapY);
                    }
                }
            }
        }

        private struct Group
        {
            public int TileIndex;
            public int X;
            public int Y;
            public Tile Tile;

            public static async Task<Group[]> GetGroups(JArray dataArr, Generator generator)
            {
                List<Group> groups = [];
                for(int i = 0; i < dataArr.Count; i++)
                {
                    if(await GetGroup(dataArr[i] as JArray, generator) is Group g)
                        groups.Add(g);
                }

                return [.. groups];
            }

            public static async Task<Group?> GetGroup(JArray? data, Generator generator)
            {
                if (data == null || data.Count < 3)
                    return null;

                Group g = new Group()
                {
                    TileIndex = data[0].ToObject<int>(),
                    X = data[1].ToObject<int>(),
                    Y = data[2].ToObject<int>(),
                };

                if(await generator._Tiles.GetTile(g.TileIndex) is Tile tile)
                {
                    g.Tile = tile;
                    return g;
                }

                return null;
            }

            public readonly void DrawOn(Image image, Generator generator)
            {
                int mapX = X - generator._MapMinX;
                int mapY = Y - generator._MapMinY;
                Tile.DrawOn(image, mapX, mapY);
            }
        }

        private struct YLines
        {
            internal static readonly Pen Pen = Pens.Solid(Color.Red);
            public int Y;
            public int X1;
            public int X2;

            public static YLines Get(JArray data)
            {
                return new YLines() { 
                    Y = data[0].ToObject<int>(),
                    X1 = data[1].ToObject<int>(),
                    X2 = data[2].ToObject<int>(),
                };
            }


            public readonly void DrawOn(Image image, Generator generator)
            {
                PointF[] points = [
                    new PointF(X1 - generator._MapMinX, Y - generator._MapMinY),
                    new PointF(X2 - generator._MapMinX, Y - generator._MapMinY)
                ];

                image.Mutate(ctx => {
                    ctx.DrawLine(Pen, points);
                });
            }
        }

        private struct XLines
        {
            public int X;
            public int Y1;
            public int Y2;

            public static XLines Get(JArray data)
            {
                return new XLines()
                {
                    X = data[0].ToObject<int>(),
                    Y1 = data[1].ToObject<int>(),
                    Y2 = data[2].ToObject<int>(),
                };
            }

            public readonly void DrawOn(Image image, Generator generator)
            {
                PointF[] points = [
                    new PointF(X - generator._MapMinX, Y1 - generator._MapMinY),
                    new PointF(X - generator._MapMinX, Y2 - generator._MapMinY)
                ];

                image.Mutate(ctx => {
                    ctx.DrawLine(YLines.Pen, points);
                });
            }
        }

        internal struct ImageSet
        {
            public int Rows;
            public int Columns;
            public int Size;
            public String File;
            public String FileName;
            public String Url;
        }

        public void Dispose()
        {
            _Tiles?.Clear();
            _Sprites?.Destroy();
            _Skins?.Clear();
            _TileSets?.Destroy();
            _ImageSets?.Clear();

            _G = null;
            _Geometry = null;
            _Tilesets = null;
            _Skills = null;
            _Npcs = null;
            _GSprites = null;
            _Items = null;
            _Positions = null;
            _GImageSets = null;
            _Images = null;
            _Tokens = null;
            _Monsters = null;
            _Dimensions = null;

        }

    }

    internal static class GeneratorExtensions
    {
        public static bool TryGetValueAs<T>(this JObject obj, String key, out T value) where T : JToken
        {
            value = default;
            if (!obj.TryGetValue(key, out JToken? token))
                return false;

            try
            {
                value = (T)token;
                return true;
            }
            catch { }

            return default;
        }

        public static IEnumerable<T> WithMap<T>(this IEnumerable<T> coll) where T : MapItem
        {
            foreach(T item in coll)
            {
                if(item.Map == null && item.MapId != null)
                    item.Map = DataContext.Instance.Maps.FirstOrDefault(m => m.Id == item.MapId);
            }

            return coll;
        }

        /*public static DbSet<T> WithMap<T>(this DbSet<T> set) where T : MapItem
        {
            foreach (T item in set)
            {
                if (item.Map == null && item.MapId != null)
                    item.Map = DataContext.Instance.Maps.FirstOrDefault(m => m.Id == item.MapId);
            }

            return set;
        }*/
    }
}
