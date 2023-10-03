using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NevisX
{
    /// <summary>
    /// Main Global Class
    /// </summary>
    class Global
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
    }
}
