using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using PhilLibX;
using PhilLibX.IO;
using NevisX.Structures;

namespace NevisX
{
    class Program
    {
        /// <summary>
        /// Gets or Sets the Tools Folder Watcher
        /// </summary>
        public static FileSystemWatcher ToolsFolderWatcher { get; set; }

        /// <summary>
        /// Gets the Asset Layouts
        /// </summary>
        public static List<AssetLayout> Layouts { get; } = new List<AssetLayout>();

        /// <summary>
        /// Gets the GDT Files we are going to load
        /// </summary>
        public static Stack<string> Files { get; } = new Stack<string>();

        /// <summary>
        /// Attempts to get the asset layout by pool index
        /// </summary>
        /// <param name="pool">Pool to get</param>
        /// <param name="layout">Layout if found, otherwise null</param>
        /// <returns>True if found, otherwise false</returns>
        public static bool TryGetAssetLayout(AssetPool pool, out AssetLayout layout) => (layout = Layouts.Find(x => x.Pool == pool)) != null;

        /// <summary>
        /// Attempts to get the asset layout by gdf name
        /// </summary>
        /// <param name="gdf">Gdf to look for</param>
        /// <param name="layout">Layout if found, otherwise null</param>
        /// <returns>True if found, otherwise false</returns>
        public static bool TryGetAssetLayout(string gdf, out AssetLayout layout) => (layout = Layouts.Find(x => x.GDFTypes.Contains(gdf))) != null;

        /// <summary>
        /// Loads XAsset DB from Black Ops III's process
        /// </summary>
        /// <param name="gameStream">Black Ops III's Process Stream</param>
        /// <returns>Resulting pools and assets</returns>
        private static Dictionary<AssetPool, Dictionary<string, Asset>> LoadXAssetDB(ProcessStream gameStream)
        {
#if DEBUG
            // Asset list for debug
            using var writer = new StreamWriter("AssetList.txt");
#endif
            var results = new Dictionary<AssetPool, Dictionary<string, Asset>>();
            var reader = new BinaryReader(gameStream, Encoding.Default, true);
            var scanResults = reader.BaseStream.Scan("48 8D 05 ?? ?? ?? ?? 41 8B 34 24 85 F6 0F 84 F0 00 00 00 4C 8D", true);

            if (scanResults.Length == 0)
                throw new Exception("Failed to locate XAsset List in Black Ops III");

            var scanResult = scanResults[0];

            foreach(var xassetEntry in reader.ReadStructArray<XAssetEntry>(156672, reader.ReadInt32(scanResult + 3) + scanResult + 7))
            {
                if(TryGetAssetLayout(xassetEntry.Pool, out var layout))
                {
                    if (!results.TryGetValue(xassetEntry.Pool, out var poolAssets))
                    {
                        poolAssets = new Dictionary<string, Asset>();
                        results[xassetEntry.Pool] = poolAssets;
                    }

                    var name = reader.ReadUTF8NullTerminatedString(reader.ReadInt64(xassetEntry.HeaderPointer + layout.NameOffset));
                    poolAssets[name] = new Asset(name, xassetEntry.HeaderPointer, layout);

#if DEBUG
                    writer.WriteLine($"{xassetEntry.Pool},{reader.ReadUTF8NullTerminatedString(reader.ReadInt64(xassetEntry.HeaderPointer + layout.NameOffset))},{xassetEntry.HeaderPointer}");
#endif
                }
            }

            return results;
        }

        internal static void LoadLayouts(string dir)
        {
            Layouts.Clear();

            foreach (var file in Directory.EnumerateFiles(dir))
            {
                Printer.WriteLine("INIT", "Loading Layout: " + Path.GetFileNameWithoutExtension(file));

                switch(Path.GetExtension(file).ToLower())
                {
                    case ".json":
                        Layouts.Add(AssetLayout.LoadJson(file));
                        break;
                    case ".nxal":
                        Layouts.Add(AssetLayout.LoadBinary(file));
                        break;
                    default:
                        Printer.WriteLine("WARNING", "Invalid Asset Layout file format", ConsoleColor.DarkRed);
                        break;
                }
            }
        }

        /// <summary>
        /// Parses GDT Fields from Linker
        /// </summary>
        /// <param name="pointer">Pointer to the fields</param>
        /// <param name="count">Number of fields</param>
        /// <param name="name">Asset/GDF name</param>
        static void ParseGDTFields(long pointer, int count, string name)
        {
            var potentialProcesses = Process.GetProcessesByName("linker_modtools");

            if (potentialProcesses.Length == 0)
                return;

            using var reader = new BinaryReader(new ProcessStream(potentialProcesses[0], ProcessStream.AccessRightsFlags.Read | ProcessStream.AccessRightsFlags.Write));

            var result = new AssetLayout()
            {
                GDFTypes = new string[]
                {
                    $"{name.ToLower()}.gdf"
                },
                Buffers = new AssetBuffer[]
                {
                    new AssetBuffer()
                    {
                        Name = "Header",
                        Offset = 0,
                        Size = -1,
                        PointerOffset = -1,
                    }
                },
                Fields = new AssetField[count],
            };

            var cspFields = reader.ReadStructArray<GDTAssetField>(count, pointer);

            for (int i = 0; i < count; i++)
            {
                result.Fields[i] = new AssetField()
                {
                    Name = reader.ReadUTF8NullTerminatedString(cspFields[i].FieldName),
                    Offset = cspFields[i].Offset,
                    DataType = cspFields[i].Type
                };
            }

            AssetLayout.SaveJson($"{name}.json", result);
        }

        static void Main(string[] args)
        {
            Directory.SetCurrentDirectory(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName));

            Printer.WriteLine("INIT", $"------------------------------");
            Printer.WriteLine("INIT", $"NevisX by Scobalula");
            Printer.WriteLine("INIT", $"Black Ops III In-Game Asset Compiler");
            Printer.WriteLine("INIT", $"Version: {Assembly.GetExecutingAssembly().GetName().Version}");
            Printer.WriteLine("INIT", $"UwU Edition");
            Printer.WriteLine("INIT", $"------------------------------");

            // ParseCspFields(140702356204912, 159, "Beam");

            ToolsFolderWatcher = new FileSystemWatcher()
            {
                Path = Environment.GetEnvironmentVariable("TA_TOOLS_PATH"),
                NotifyFilter = NotifyFilters.LastWrite,
                Filter = "*.gdt",
                EnableRaisingEvents = true,
                IncludeSubdirectories = true
            };

            ToolsFolderWatcher.Changed += ToolsFolderWatcherChanged;

            Printer.WriteLine("WATCHER", "Watching for changes to gdts....");

            while(true)
            {
                try
                {
                    if (Files.Count > 0)
                    {
                        LoadLayouts(@"Layouts");

                        var potentialProcesses = Process.GetProcessesByName("blackops3");

                        if (potentialProcesses.Length == 0)
                            throw new Exception("Failed to find Black Ops III's process");
                        Printer.WriteLine("LOADER", $"Attemping to load Black Ops III....");

                        using var gameStream = new ProcessStream(potentialProcesses[0], ProcessStream.AccessRightsFlags.Read | ProcessStream.AccessRightsFlags.Write);
                        var gameAssets = LoadXAssetDB(gameStream);

                        Printer.WriteLine("LOADER", $"Loaded {gameAssets.Sum(x => x.Value.Count)} assets from {gameAssets.Count} pools");

                        while (Files.Count > 0)
                        {
                            try
                            {
                                var input = Files.Pop();

                                Printer.WriteLine("COMPILER", $"Loading: {Path.GetFileName(input)}");

                                var gdt = new GameDataTable(input);

                                foreach (var asset in gdt.Assets)
                                {
                                    if (gdt.TryGetRootAsset(asset, out var root))
                                    {
                                        if (TryGetAssetLayout(root.Type, out var layout) && gameAssets.TryGetValue(layout.Pool, out var assets) && assets.TryGetValue(asset.Name, out var gameAsset))
                                        {
                                            Printer.WriteLine("COMPILER", $"Compiling {gameAsset.Name}");
                                            gameAsset.Load(gameStream);
                                            gameAsset.ParseFromGDT(asset);
                                            gameAsset.Save(gameStream);
                                            Printer.WriteLine("COMPILER", $"Compiled {gameAsset.Name}");
                                        }
                                    }
                                }
                            }
                            catch
                            {
                                continue;
                            }
                        }

                        Printer.WriteLine("WATCHER", "Watching for changes to gdts....");
                    }
                }
                catch(Exception e)
                {
                    Files.Clear();
                    Printer.WriteLine("ERROR", e.Message, ConsoleColor.DarkRed);
                    Printer.WriteLine("WATCHER", "Watching for changes to gdts....");
                }

                Thread.Sleep(1000);
            }
        }

        private static void ToolsFolderWatcherChanged(object sender, FileSystemEventArgs e)
        {
            if (!Path.GetExtension(e.FullPath).Equals(".gdt", StringComparison.CurrentCultureIgnoreCase))
                return;
            if (Files.Contains(e.FullPath))
                return;

            Files.Push(e.FullPath);
        }
    }
}
