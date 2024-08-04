namespace MapGenerator.DB
{
    public class Spawn : MapItem
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int SpawnId { get; set; }

        public List<String> Connections { get; set; } = new List<string>();
    }
}
