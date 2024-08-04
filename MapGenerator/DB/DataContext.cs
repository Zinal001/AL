using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MongoDB.Driver;

namespace MapGenerator.DB
{
    public class DataContext : DbContext
    {
        private const String _MONGO_CONN_STRING = "mongodb://root:root@db:27017";
        private static MongoClient _MongoClient;
        private static DataContext _Instance;

        public static DataContext Instance
        {
            get
            {
                if (_MongoClient == null)
                    _MongoClient = new MongoClient(_MONGO_CONN_STRING);

                if(_Instance == null)
                    _Instance = new DataContext(new DbContextOptionsBuilder().UseMongoDB(_MongoClient, "almapper").Options);

                return _Instance;
            }
        }

        //public DataContext() : base() { }
        public DataContext(DbContextOptions options) : base(options) { }

        public static DataContext NewContext()
        {
            return new DataContext(new DbContextOptionsBuilder().UseMongoDB(_MONGO_CONN_STRING, "almapper").Options);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if(!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseMongoDB(_MONGO_CONN_STRING, "almapper");
                //optionsBuilder.UseMySQL("server=db; database=almapper; user=root; password=root");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            /*modelBuilder.Entity<Zone>()
                .Property(x => x.Polygon)
                    .HasConversion<DBJsonConverter<List<DBPoint>>>();
            modelBuilder.Entity<MonsterZone>()
                .Property(x => x.Boundaries)
                    .HasConversion<DBJsonConverter<List<Boundary>>>();
            modelBuilder.Entity<Npc>()
                .Property(x => x.Positions)
                    .HasConversion<DBJsonConverter<List<DBPoint>>>();
            modelBuilder.Entity<Npc>()
                .Property(x => x.Says)
                    .HasConversion<DBJsonConverter<List<String>?>>();
            modelBuilder.Entity<Npc>()
                .Property(x => x.Boundary)
                    .HasConversion<DBJsonConverter<Boundary?>>();
            modelBuilder.Entity<Npc>()
                .Property(x => x.TokenItem)
                    .HasConversion<DBJsonConverter<Item?>>();
            modelBuilder.Entity<Npc>()
                .Property(x => x.Items)
                    .HasConversion<DBJsonConverter<List<Item>>>();
            modelBuilder.Entity<Spawn>()
                .Property(x => x.Connections)
                    .HasConversion<DBJsonConverter<List<String>>>();*/
            modelBuilder.Entity<Door>()
                .HasOne(e => e.ToMap)
                .WithMany(e => e.Doors)
                .HasForeignKey(e => e.ToMapId)
                .IsRequired(false);
        }

        public DbSet<Server> Servers { get; set; }
        public DbSet<Map> Maps { get; set; }
        public DbSet<Quirk> Quirks { get; set; }
        public DbSet<Spawn> Spawns { get; set; }
        public DbSet<Door> Doors { get; set; }
        public DbSet<Zone> Zones { get; set; }
        public DbSet<MonsterZone> MonsterZones { get; set; }
        public DbSet<Npc> Npcs { get; set; }
        //public DbSet<Item> Items { get; set; }

        private class DBJsonConverter<T> : ValueConverter<T, String>
        {
            public DBJsonConverter() : base(
                v => Newtonsoft.Json.JsonConvert.SerializeObject(v, Newtonsoft.Json.Formatting.None),
                v => Newtonsoft.Json.JsonConvert.DeserializeObject<T>(v)
                ) { }
        }

        public IEnumerable<Spawn> GetSpawnsWithMap()
        {
            List<Spawn> spawns = new List<Spawn>();
            foreach(Spawn spawn in Spawns)
            {
                if(spawn.Map == null)
                    spawn.Map = Maps.FirstOrDefault(m => m.Id == spawn.MapId);
                spawns.Add(spawn);
            }
            return spawns;
        }

        public IEnumerable<Door> GetDoorsWithMap()
        {
            List<Door> doors = new List<Door>();
            foreach (Door door in Doors)
            {
                if (door.Map == null)
                    door.Map = Maps.FirstOrDefault(m => m.Id == door.MapId);
                doors.Add(door);
            }
            return doors;
        }
    }
}
