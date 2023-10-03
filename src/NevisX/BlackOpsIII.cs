using NevisX.Structures;
using PhilLibX.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NevisX
{
    /// <summary>
    /// 
    /// </summary>
    internal class BlackOpsIII
    {
        /// <summary>
        /// Attempts to locate the game process in the current system
        /// </summary>
        /// <returns>Game process if found, otherwise null</returns>
        internal static ProcessStream GetGameProcessStream()
        {
            var potentialProcesses = Process.GetProcessesByName("blackops3");

            if (potentialProcesses.Length == 0)
                return null;

            return new ProcessStream(potentialProcesses[0], ProcessStream.AccessRightsFlags.Read | ProcessStream.AccessRightsFlags.Write);
        }

        /// <summary>
        /// Loads XAsset DB from Black Ops III's process
        /// </summary>
        /// <param name="gameStream">Black Ops III's Process Stream</param>
        /// <returns>Resulting pools and assets</returns>
        internal static Dictionary<AssetPool, Dictionary<string, Asset>> LoadXAssetDB(ProcessStream gameStream)
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

            foreach (var xassetEntry in reader.ReadStructArray<XAssetEntry>(156672, reader.ReadInt32(scanResult + 3) + scanResult + 7))
            {
                if (AssetLayout.TryGetAssetLayout(xassetEntry.Pool, out var layout))
                {
                    if (!results.TryGetValue(xassetEntry.Pool, out var poolAssets))
                    {
                        poolAssets = new Dictionary<string, Asset>();
                        results[xassetEntry.Pool] = poolAssets;
                    }

                    var name = reader.ReadUTF8NullTerminatedString(reader.ReadInt64(xassetEntry.HeaderPointer + layout.NameOffset));
                    poolAssets[name] = new Asset(name, xassetEntry.HeaderPointer, layout);
                }

#if DEBUG
                writer.WriteLine($"{xassetEntry.Pool},{reader.ReadUTF8NullTerminatedString(reader.ReadInt64(xassetEntry.HeaderPointer + layout.NameOffset))},{xassetEntry.HeaderPointer}");
#endif
            }

            return results;
        }
    }
}
