using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NevisX
{
    /// <summary>
    /// A class to define an Asset Buffer
    /// </summary>
    public class AssetBuffer
    {
        /// <summary>
        /// Gets or Sets the name of the buffer
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or Sets the buffer offset
        /// </summary>
        public int Offset { get; set; }

        /// <summary>
        /// Gets or Sets the size of the buffer
        /// </summary>
        public int Size { get; set; }

        /// <summary>
        /// Gets or Sets the offset to the pointer to the buffer
        /// </summary>
        public int PointerOffset { get; set; }
    }
}
