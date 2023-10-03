using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NevisX
{
    /// <summary>
    /// A class to define an Asset Field
    /// </summary>
    public class AssetField
    {
        /// <summary>
        /// Gets or Sets the name of the field
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or Sets the offset of the field in the buffer
        /// </summary>
        public int Offset { get; set; }

        /// <summary>
        /// Gets or Sets the field data type
        /// </summary>
        public AssetFieldDataType DataType { get; set; }

        /// <summary>
        /// Gets or Sets the enum values if this is Enum
        /// </summary>
        public Dictionary<string, string> Enums { get; set; }
    }
}
