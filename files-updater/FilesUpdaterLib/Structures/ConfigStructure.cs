using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FilesUpdaterLib
{
    public class ConfigStructure
    {
        /*[JsonProperty("v")]
        public string Version { get; set; }

        [JsonProperty("r")]
        public int Revision { get; set; }*/

        public const byte Version = 0;
        public int ChunkSize { get; set; }
        public ConcurrentDictionary<string, FileStruct> Files { get; set; }

        private static Dictionary<string, object> BuildNestedCollection(Dictionary<string, FileStruct> files)
        {
            return files.Keys.Aggregate(new Dictionary<string, object>(), (dict, path) =>
            {
                var parts = path.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);

                IDictionary<string, object> current = dict;

                for (int i = 0; i < parts.Length; i++)
                {
                    var part = parts[i];

                    if (i == parts.Length - 1)
                    {
                        if (!current.ContainsKey(part))
                            current.Add(part, files[path]);
                    }
                    else
                    {
                        if (!current.ContainsKey(part))
                            current.Add(part, new Dictionary<string, object>());

                        current = (Dictionary<string, object>)current[part];
                    }
                }

                return dict;
            });
        }

        public static ConfigStructure Decode(BinaryReader br)
        {
            var config = new ConfigStructure();

            /*byte v = br.ReadByte();
            if (v != Version)
            {
                throw new InvalidDataException("Unsupported config version (" + v + ") expected (" + Version + ")");
            }*/

            config.ChunkSize = br.ReadInt32();
            config.Files = new();

            Flatten(DecodeNest(br), "", config.Files);

            /*foreach(var e in config.Files)
            {
                Console.WriteLine(e.Key);
            }*/

            return config;
        }

        private static void Flatten(Dictionary<string, object> nested, string currentPath, ConcurrentDictionary<string, FileStruct> flat)
        {
            foreach (var kvp in nested)
            {
                string path = string.IsNullOrEmpty(currentPath) ? kvp.Key : currentPath + '\\' + kvp.Key;

                if (kvp.Value is Dictionary<string, object> childDict)
                {
                    Flatten(childDict, path, flat);
                }
                else if (kvp.Value is FileStruct file)
                {
                    flat[path] = file;
                }
            }
        }


        private const byte NestedFlag = 2;
        private const byte FileFlag = 1;
        private const byte EndFlag = 0;

        private static Dictionary<string, object> DecodeNest(BinaryReader br)
        {
            //ushort count = br.ReadUInt16();

            var dict = new Dictionary<string, object>();

            byte header = 0;

            while ((header = br.ReadByte()) != EndFlag)
            {
                string key = br.ReadString();

                if (header == NestedFlag)
                {
                    dict.Add(key, DecodeNest(br));
                }
                else if (header == FileFlag)
                {
                    // Otherwise, decode the FileStruct
                    dict.Add(key, FileStruct.Decode(br));
                }
                else
                {
                    throw new InvalidDataException("Unknown header flag data is probably corrupted: " + header);
                }
            }

            return dict;
        }

        /*public void wr(Dictionary<string, object> ob, string parent = "")
        {
            foreach(var e  in ob)
            {
                Console.WriteLine(parent + "\\"+ e.Key + ", " + (e.Value?.GetType()));

                if (e.Value is IDictionary)
                {
                    wr((Dictionary<string,object>)e.Value, parent + "\\"+ e.Key);
                }
            }
        }*/

        public void Encode(BinaryWriter bw)
        {
            //bw.Write(Version);
            bw.Write(this.ChunkSize);

            var we = BuildNestedCollection(new(this.Files));

            //wr(we);

            EncodeNest(bw, we);
        }

        private void EncodeNest(BinaryWriter bw, Dictionary<string, object> d)
        {
            foreach (var e in d.OrderByDescending(e => e.Key.ToLower()))
            {
                if (e.Value is IDictionary)
                {
                    bw.Write(NestedFlag);
                    bw.Write(e.Key);
                    EncodeNest(bw, (Dictionary<string, object>)e.Value);
                }
                else
                {
                    bw.Write(FileFlag);
                    bw.Write(e.Key);
                    ((FileStruct)e.Value).Encode(bw);
                }
            }

            bw.Write(EndFlag);
        }
    }

    public class FileStruct
    {
        public uint Hash { get; set; }
        public long Size { get; set; }
        //public long CompressedSize { get; set; }
        public long? LastModified { get; set; }
        public static FileStruct Decode(BinaryReader br)
        {
            var fs = new FileStruct();

            fs.Hash = br.ReadUInt32();
            fs.Size = br.ReadInt64();
            //fs.CompressedSize = br.ReadInt64();

            if (br.ReadBoolean())
            {
                fs.LastModified = br.ReadInt64();
            }
            //fs.LastModified = br.ReadInt64();
            return fs;
        }

        public void Encode(BinaryWriter bw)
        {
            bw.Write(Hash);
            bw.Write(Size);
            //bw.Write(CompressedSize);

            bw.Write(LastModified != null);

            if (LastModified != null)
            {
                bw.Write((long)LastModified);
            }
        }
        /*private static ulong ReverseBytes(ulong value)
        {
            ulong b0 = (value & 0x00000000000000FFUL) << 56;
            ulong b1 = (value & 0x000000000000FF00UL) << 40;
            ulong b2 = (value & 0x0000000000FF0000UL) << 24;
            ulong b3 = (value & 0x00000000FF000000UL) << 8;
            ulong b4 = (value & 0x000000FF00000000UL) >> 8;
            ulong b5 = (value & 0x0000FF0000000000UL) >> 24;
            ulong b6 = (value & 0x00FF000000000000UL) >> 40;
            ulong b7 = (value & 0xFF00000000000000UL) >> 56;
            return b0 | b1 | b2 | b3 | b4 | b5 | b6 | b7;
        }*/
    }
}