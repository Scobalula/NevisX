using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NevisX
{
    /// <summary>
    /// A class to hold an Asset Layout
    /// </summary>
    public class AssetLayout
    {
        /// <summary>
        /// Gets the Asset Layouts
        /// </summary>
        public static List<AssetLayout> Layouts { get; } = new List<AssetLayout>();

        /// <summary>
        /// Gets or Sets the GDF Types
        /// </summary>
        public string[] GDFTypes { get; set; }

        /// <summary>
        /// Gets or Sets the size of the buffer
        /// </summary>
        public int BufferSize { get; set; }

        /// <summary>
        /// Gets or Sets the asset pool
        /// </summary>
        public AssetPool Pool { get; set; }

        /// <summary>
        /// Gets or Sets the offset to the asset name
        /// </summary>
        public int NameOffset { get; set; }

        /// <summary>
        /// Gets or Sets the buffers
        /// </summary>
        public AssetBuffer[] Buffers { get; set; }

        /// <summary>
        /// Gets or Sets the fields
        /// </summary>
        public AssetField[] Fields { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public bool TryGetField(string fileName, out AssetField field) => (field = Fields.FirstOrDefault(x => x.Name == fileName)) != null;

        public static AssetLayout LoadJson(string fileName)
        {
            return JsonSerializer.Deserialize<AssetLayout>(File.ReadAllBytes(fileName));
        }

        public static void SaveJson(string fileName, AssetLayout layout)
        {
            File.WriteAllBytes(fileName, JsonSerializer.SerializeToUtf8Bytes(layout, new JsonSerializerOptions()
            {
                WriteIndented = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            }));
        }

        public static void SaveBinary(string fileName, AssetLayout layout)
        {
            using var writer = new BinaryWriter(File.Create(fileName));

            writer.Write(0x4C41584E); // NXAL magic (Nevis X Asset Layout)
            writer.Write(1); // Version

            writer.Write(layout.GDFTypes.Length);

            foreach (var type in layout.GDFTypes)
            {
                writer.Write(type);
            }

            writer.Write(layout.BufferSize);
            writer.Write((int)layout.Pool);
            writer.Write(layout.NameOffset);

            writer.Write(layout.Buffers.Length);

            foreach (var buffer in layout.Buffers)
            {
                writer.Write(buffer.Name);
                writer.Write(buffer.Offset);
                writer.Write(buffer.Size);
                writer.Write(buffer.PointerOffset);
            }

            writer.Write(layout.Fields.Length);

            foreach (var field in layout.Fields)
            {
                writer.Write(field.Name);
                writer.Write(field.Offset);
                writer.Write((int)field.DataType);
                writer.Write(field.Enums.Count);

                foreach (var (enumKey, enumValue) in field.Enums)
                {
                    writer.Write(enumKey);
                    writer.Write(enumValue);
                }
            }
        }

        public static AssetLayout LoadBinary(string fileName)
        {
            var result = new AssetLayout();

            using var reader = new BinaryReader(File.OpenRead(fileName));

            if (reader.ReadUInt32() != 0x4C41584E)
                throw new Exception("Invalid Asset Layout File Magic");
            if (reader.ReadUInt32() != 1)
                throw new Exception("Invalid Asset Layout File Magic");

            result.GDFTypes = new string[reader.ReadInt32()];

            for (int i = 0; i < result.GDFTypes.Length; i++)
            {
                result.GDFTypes[i] = reader.ReadString();
            }

            result.BufferSize = reader.ReadInt32();
            result.Pool = (AssetPool)reader.ReadInt32();
            result.NameOffset = reader.ReadInt32();


            result.Buffers = new AssetBuffer[reader.ReadInt32()];

            for (int i = 0; i < result.Buffers.Length; i++)
            {
                result.Buffers[i] = new AssetBuffer
                {
                    Name = reader.ReadString(),
                    Offset = reader.ReadInt32(),
                    Size = reader.ReadInt32(),
                    PointerOffset = reader.ReadInt32()
                };
            }

            result.Fields = new AssetField[reader.ReadInt32()];

            for (int i = 0; i < result.Fields.Length; i++)
            {
                result.Fields[i] = new AssetField()
                {
                    Name = reader.ReadString(),
                    Offset = reader.ReadInt32(),
                    DataType = (AssetFieldDataType)reader.ReadInt32(),
                };

                var enumCount = reader.ReadInt32();

                for (int e = 0; e < enumCount; e++)
                {
                    result.Fields[i].Enums[reader.ReadString()] = reader.ReadString();
                }
            }

            return result;
        }

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
    }
}
