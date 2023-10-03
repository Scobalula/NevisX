using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NevisX.Structures
{
    /// <summary>
    /// A struct to hold a GDT Asset Field
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = 32)]
    internal struct GDTAssetField
    {
        public long FieldName;
        public int Offset;
        public AssetFieldDataType Type;
    }
}
