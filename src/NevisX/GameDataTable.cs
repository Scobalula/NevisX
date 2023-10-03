using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NevisX
{
    /// <summary>
    /// A class to hold and handle parsing a Game Data Table
    /// </summary>
    public class GameDataTable
    {
        /// <summary>
        /// The exception type that is thrown for parsing issues
        /// </summary>
        private class FileParseException : Exception
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="FileParseException"/> class.
            /// </summary>
            public FileParseException() : base() { }

            /// <summary>
            /// Initializes a new instance of the <see cref="FileParseException"/> class.
            /// </summary>
            /// <param name="message">The message that describes the error.</param>
            public FileParseException(string message) : base(message) { }

            /// <summary>
            /// Initializes a new instance of the <see cref="FileParseException"/> class.
            /// </summary>
            /// <param name="message">The message that describes the error.</param>
            /// <param name="line">Line in the source that the error occured at</param>
            /// <param name="column">Column in the line of the source the error occured at</param>
            public FileParseException(string message, int line, int column) : base($"{message} Line: {line} Col: {column}") { }
        }

        /// <summary>
        /// Class to hold GDT Asset Information
        /// </summary>
        public class Asset
        {
            /// <summary>
            /// Name of Asset
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Type of Asset (GDF)
            /// </summary>
            public string Type { get; set; }

            /// <summary>
            /// If this asset is derived or not
            /// </summary>
            public bool Derivative { get; set; }

            /// <summary>
            /// Asset Properties/Settings
            /// </summary>
            public Dictionary<string, string> Properties = new Dictionary<string, string>();

            /// <summary>
            /// Initializes a GDT Asset
            /// </summary>
            public Asset() { }

            /// <summary>
            /// Initializes a GDT Asset with Name and Type
            /// </summary>
            /// <param name="name">Name of the Asset</param>
            /// <param name="type">Type of Asset (GDF)</param>
            public Asset(string name, string type)
            {
                Name = name;
                Type = type;
            }

            /// <summary>
            /// Clones this Asset
            /// </summary>
            /// <returns>Resulting Asset</returns>
            public Asset Copy()
            {
                // New Asset
                var result = new Asset(Name, Type) { Derivative = Derivative };

                // Copy properties
                foreach (var property in Properties)
                    result.Properties[property.Key] = property.Value;

                // Return result
                return result;
            }

            /// <summary>
            /// Checks the two <see cref="Asset"/> objects for equality based off Name and Type
            /// </summary>
            /// <param name="obj">Object to compare this <see cref="Asset"/> to</param>
            /// <returns>True if equal, otherwise false</returns>
            public override bool Equals(object obj)
            {
                if (obj is Asset asset)
                    return asset.Name == Name && asset.Type == Type;

                return base.Equals(obj);
            }

            /// <summary>
            /// Gets the Hash Code for the <see cref="Asset"/>
            /// </summary>
            /// <returns>Hash Code based off Name and Type</returns>
            public override int GetHashCode() => HashCode.Combine(Name, Type);
        }

        /// <summary>
        /// Gets or Sets the Name of the Game Data Table
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets the Assets in the Game Data Table
        /// </summary>
        public HashSet<Asset> Assets { get; } = new HashSet<Asset>();

        /// <summary>
        /// Gets the number of Assets in the GDT
        /// </summary>
        public int AssetCount => Assets.Count;

        /// <summary>
        /// Initializes a new instance of the <see cref="GameDataTable"/> class
        /// </summary>
        public GameDataTable() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="GameDataTable"/> class from a file
        /// </summary>
        /// <param name="filePath">File to load from</param>
        public GameDataTable(string filePath) => Load(filePath);

        /// <summary>
        /// Initializes a new instance of the <see cref="GameDataTable"/> class from a <see cref="Stream"/>
        /// </summary>
        /// <param name="stream"><see cref="Stream"/> to load from</param>
        public GameDataTable(Stream stream) => Load(stream);

        /// <summary>
        /// Initializes a new instance of the <see cref="GameDataTable"/> class from a <see cref="TextReader"/>
        /// </summary>
        /// <param name="stream"><see cref="TextReader"/> to load from</param>
        public GameDataTable(TextReader reader) => Load(reader);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <param name="asset"></param>
        /// <returns></returns>
        public bool TryGetAsset(string name, string type, out Asset asset) => (asset = Assets.FirstOrDefault(x => x.Name == name && x.Type == type)) != null;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <param name="asset"></param>
        /// <returns></returns>
        public bool TryGetAsset(string name, out Asset asset) => (asset = Assets.FirstOrDefault(x => x.Name == name)) != null;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <param name="asset"></param>
        /// <returns></returns>
        public bool TryGetRootAsset(Asset asset, out Asset root)
        {
            if (!asset.Derivative)
            {
                root = asset;
                return true;
            }

            root = null;
            var parent = asset;

            while (true)
            {
                if (!TryGetAsset(parent.Type, out parent))
                    break;

                if (!parent.Derivative)
                {
                    root = parent;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Loads from the provided file
        /// </summary>
        /// <param name="filePath">File to load from</param>
        public void Load(string filePath)
        {
            if (!File.Exists(filePath))
                return;

            using var reader = new StreamReader(filePath);

            Load(reader);
        }

        /// <summary>
        /// Loads from the provided <see cref="Stream"/>
        /// </summary>
        /// <param name="stream"><see cref="Stream"/> to load from</param>
        public void Load(Stream stream)
        {
            // Do not take ownership of the stream, leave it to the caller
            using var reader = new StreamReader(stream, leaveOpen: true);

            Load(reader);
        }

        /// <summary>
        /// Loads from the provided <see cref="TextReader"/>
        /// </summary>
        /// <param name="reader"><see cref="TextReader"/> to load from</param>
        public void Load(TextReader reader)
        {
            int line = 1;
            int column = 0;

            // Returns the next "token"
            // GDTs will only have strings, (, ), {, }, [, or ]
            string RequestNextToken(TextReader reader)
            {
                while (true)
                {
                    int c = reader.Read();

                    if (c == -1)
                        throw new FileParseException("Unexpected EOF", line, column);

                    column++;

                    if (c == '\n')
                    {
                        column = 0;
                        line++;
                        continue;
                    }

                    if (char.IsWhiteSpace((char)c))
                    {
                        continue;
                    }
                    else if (c == '"')
                    {
                        var builder = new StringBuilder();

                        while (true)
                        {
                            c = reader.Read();

                            if (c == -1)
                                throw new FileParseException("Unexpected EOF", line, column);
                            if (c == '\n')
                                throw new FileParseException("Unexpected EOL in string literal", line, column);
                            if (c == '"')
                                return builder.ToString();

                            builder.Append((char)c);
                        }
                    }
                    else
                    {
                        return new string((char)c, 1);
                    }
                }
            }

            if (RequestNextToken(reader) != "{")
                throw new FileParseException("Expecting { at start of file", line, column);

            while (true)
            {
                var assetName = RequestNextToken(reader);

                if (assetName == "}")
                    break;

                var startBracket = RequestNextToken(reader);
                var assetType = RequestNextToken(reader);
                var endBracket = RequestNextToken(reader);
                bool derivative;

                if (startBracket == "(" && endBracket == ")")
                    derivative = false;
                else if (startBracket == "[" && endBracket == "]")
                    derivative = true;
                else
                    throw new FileParseException("Expecting type brackets", line, column);

                if (RequestNextToken(reader) != "{")
                    throw new FileParseException("Expecting { at start of asset", line, column);

                var asset = new Asset(assetName, assetType)
                {
                    Derivative = derivative
                };

                while (true)
                {
                    var key = RequestNextToken(reader);

                    if (key == "}")
                        break;

                    asset.Properties[key] = RequestNextToken(reader);
                }

                if (!Assets.Add(asset))
                    throw new FileParseException($"Duplicate asset: {assetName} of type: {assetType}", line, column);
            }

        }

        /// <summary>
        /// Saves the GDT to the file
        /// </summary>
        /// <param name="filePath">File to save to</param>
        public void Save(string filePath)
        {
            using var writer = new StreamWriter(filePath);

            Save(writer);
        }

        /// <summary>
        /// Saves the GDT to the <see cref="Stream"/>
        /// </summary>
        /// <param name="file"><see cref="Stream"/> to save to</param>
        public void Save(Stream stream)
        {
            using var writer = new StreamWriter(stream, leaveOpen: true);

            Save(writer);
        }

        /// <summary>
        /// Saves the GDT to the <see cref="TextWriter"/>
        /// </summary>
        /// <param name="writer"><see cref="TextWriter"/> to save to</param>
        public void Save(TextWriter writer)
        {
            writer.WriteLine("{");

            foreach (var asset in Assets)
            {
                bool isDerived = asset.Derivative;

                writer.WriteLine("	\"{0}\" {2} \"{1}\" {3}",
                    asset.Name,
                    asset.Type,
                    isDerived ? "[" : "(",
                    isDerived ? "]" : ")");

                writer.WriteLine("	{");

                foreach (var setting in asset.Properties)
                    writer.WriteLine("		\"{0}\" \"{1}\"", setting.Key, setting.Value);

                writer.WriteLine("	}");
            }

            writer.WriteLine("}");
        }
    }
}
