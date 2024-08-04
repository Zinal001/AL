namespace MapGenerator
{
    internal class Program
    {
        private static DateTime _LastRun = DateTime.MinValue;

        static void Main(string[] args)
        {
            MainAsync(args).GetAwaiter().GetResult();
        }

        static async Task MainAsync(String[] args)
        {
            int interval = 0;
            String scale = "m";

            foreach(String arg in args)
            {
                String[] parts = arg.Split('=');
                String cmd = parts[0].ToLower();

                if ((cmd == "-interval" || cmd == "-i") && parts.Length > 1 && int.TryParse(parts[1], out int i))
                    interval = i;
                else if (cmd == "-scale" || cmd == "-s" && parts.Length > 1)
                    scale = parts[1].ToLower();
            }

            switch(scale)
            {
                case "s":
                    interval *= 1000;
                    break;
                case "m":
                    interval *= 60000;
                    break;
                case "h":
                    interval *= 60000 * 60;
                    break;
                case "d":
                    interval *= 60000 * 60 * 24;
                    break;
            }

            if (interval == 0)
                await Run();
            else
            {
                Console.WriteLine($"Running continuously. Interval: {interval}ms");
                while(true)
                {
                    await Run();
                    _LastRun = DateTime.Now;

                    await Task.Delay(interval);
                }
            }

        }

        static async Task Run()
        {
            DB.Server[] servers;

            //dataCtx.Database.EnsureDeleted();
            DB.DataContext.Instance.Database.EnsureCreated();

            if (!DB.DataContext.Instance.Servers.Any(s => s.Name == "Adventure Land"))
            {
                Console.WriteLine("No server instances.. Creating them!");
                DB.DataContext.Instance.Servers.Add(new DB.Server() { Name = "Adventure Land", Url = "https://adventure.land", ServerKey = "Default" });
                DB.DataContext.Instance.Servers.Add(new DB.Server() { Name = "thmsn", Url = "http://thmsn.adventureland.community", ServerKey = "thmsn" });
                DB.DataContext.Instance.SaveChanges();
            }

            servers = [.. DB.DataContext.Instance.Servers];

            int i = 0;
            int j = servers.Length;
            foreach (var server in servers)
            {
                i++;
                Console.WriteLine($"[{i}/{j}] Generating server {server.Name}...");
                try
                {
                    using (Generator gen = new Generator(server.ServerKey))
                    {
                        await gen.GenerateAllMaps();
                    }
                    GC.Collect();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{i}/{j}] Failed: {ex.Message}");
                }
            }

            Console.WriteLine("All servers/maps generated");
        }
    }
}
