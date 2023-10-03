using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NevisX.Structures
{
    /// <summary>
    /// A struct to hold a Black Ops III XAsset Entry
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = 32)]
    internal struct XAssetEntry
    {
        public AssetPool Pool;
        public uint InUse;
        public long HeaderPointer;
        public byte ZoneIndex;
    }
}
