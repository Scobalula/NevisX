using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PhilLibX;
using PhilLibX.IO;

namespace NevisX
{
    /// <summary>
    /// A class to hold a game asset
    /// </summary>
    class Asset
    {
        /// <summary>
        /// Gets or Sets the name of the asset
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or Sets the layout this asset uses
        /// </summary>
        public AssetLayout Layout { get; set; }

        /// <summary>
        /// Gets or Sets the pointer to the asset header
        /// </summary>
        public long HeaderPointer { get; set; }

        /// <summary>
        /// Gets or Sets the asset buffer
        /// </summary>
        public byte[] AssetBuffer { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Asset"/> class with the provided information
        /// </summary>
        /// <param name="name">Asset name</param>
        /// <param name="ptr">Pointer to the asset</param>
        /// <param name="layout">Asset layout</param>
        public Asset(string name, long ptr, AssetLayout layout)
        {
            Name = name;
            HeaderPointer = ptr;
            Layout = layout;
        }

        public void Load(ProcessStream gameStream)
        {
            AssetBuffer = new byte[Layout.BufferSize];
            foreach (var buffer in Layout.Buffers)
            {
                if (buffer.PointerOffset == -1)
                {
                    gameStream.Seek(HeaderPointer, SeekOrigin.Begin);
                    gameStream.Read(AssetBuffer, buffer.Offset, buffer.Size);
                }
                else
                {
                    gameStream.Seek(BitConverter.ToInt64(AssetBuffer, buffer.PointerOffset), SeekOrigin.Begin);
                    gameStream.Read(AssetBuffer, buffer.Offset, buffer.Size);
                }
            }
        }

        public void Save(ProcessStream gameStream)
        {
            foreach (var buffer in Layout.Buffers)
            {
                if (buffer.PointerOffset == -1)
                {
                    gameStream.Seek(HeaderPointer, SeekOrigin.Begin);
                    gameStream.Write(AssetBuffer, buffer.Offset, buffer.Size);
                }
                else
                {
                    gameStream.Seek(BitConverter.ToInt64(AssetBuffer, buffer.PointerOffset), SeekOrigin.Begin);
                    gameStream.Write(AssetBuffer, buffer.Offset, buffer.Size);
                }
            }
        }

        public unsafe void ParseFromGDT(GameDataTable.Asset gdtAsset)
        {
            fixed (byte* numPtr = &AssetBuffer[0])
            {
                foreach (var (propKey, propValue) in gdtAsset.Properties)
                {
                    if (Layout.TryGetField(propKey, out AssetField field))
                    {
                        switch (field.DataType)
                        {
                            case AssetFieldDataType.Int:
                                {
                                    *(int*)(numPtr + field.Offset) = int.TryParse(propValue, out var result) ? result : 0;
                                    break;
                                }
                            case AssetFieldDataType.UInt:
                                {
                                    *(int*)(numPtr + field.Offset) = uint.TryParse(propValue, out var result) ? (int)result : 0;
                                    break;
                                }
                            case AssetFieldDataType.Float:
                                {
                                    *(float*)(numPtr + field.Offset) = float.TryParse(propValue, out var result) ? result : 0.0f;
                                    break;
                                }
                            case AssetFieldDataType.Bool:
                                {
                                    *(numPtr + field.Offset) = uint.TryParse(propValue, out var result) && result != 0 ? (byte)1 : (byte)0;
                                    break;
                                }
                            case AssetFieldDataType.QBool:
                                {
                                    *(int*)(numPtr + field.Offset) = uint.TryParse(propValue, out var result) && result != 0 ? 1 : 0;
                                    break;
                                }
                            case AssetFieldDataType.Milliseconds:
                                {
                                    *(int*)(numPtr + field.Offset) = float.TryParse(propValue, out var result) ? (int)(result * 1000.0) : 0;
                                    break;
                                }
                            case AssetFieldDataType.IntEnum:
                                {
                                    if(field.Enums != null)
                                        if(field.Enums.TryGetValue(propValue, out var value))
                                            *(int*)(numPtr + field.Offset) = int.TryParse(value, out var result) ? result : 0;
                                    break;
                                }
                            case AssetFieldDataType.RGBAColor:
                                {
                                    var allThingSplit = propValue.Split(' ', StringSplitOptions.TrimEntries);

                                    if (allThingSplit.Length >= 4)
                                    {
                                        *(float*)(numPtr + field.Offset + 00) = float.TryParse(allThingSplit[0], out var rResult) ? Math.Clamp(rResult, 0.0f, 1.0f) : 0;
                                        *(float*)(numPtr + field.Offset + 04) = float.TryParse(allThingSplit[1], out var gResult) ? Math.Clamp(gResult, 0.0f, 1.0f) : 0;
                                        *(float*)(numPtr + field.Offset + 08) = float.TryParse(allThingSplit[2], out var bResult) ? Math.Clamp(bResult, 0.0f, 1.0f) : 0;
                                        *(float*)(numPtr + field.Offset + 12) = float.TryParse(allThingSplit[3], out var aResult) ? Math.Clamp(aResult, 0.0f, 1.0f) : 0;
                                    }
                                    break;
                                }
                            default:
#if DEBUG
                                Printer.WriteLine("DEBUG", $"Unknown field: {propKey} of type: {field.DataType}", ConsoleColor.DarkGreen);
#endif
                                break;
                        }
                    }
                }
            }
        }
    }
}
