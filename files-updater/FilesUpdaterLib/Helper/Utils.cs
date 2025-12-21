using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.IO.Enumeration;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using static AnsiHelper;

namespace FilesUpdaterLib
{
    public static class Utils
    {
        public static readonly int ChunkSize = (99 * 1024 * 1024) + ((1024 / 2) * 1024);

        public static readonly string ExecutablePath = GetExecutablePath();
        public static readonly string CurrentFileName = Path.GetFileNameWithoutExtension(ExecutablePath);
        public static readonly string CurrentPath = Path.GetDirectoryName(ExecutablePath);

        //public static readonly bool DeleteEmptyDirectories = true;

        public static readonly string OutputFolder = ".upd";
        public static readonly string PackedExtension = ".upack";
        public static readonly string ProgressExtension = ".uprog";

        public static readonly string ConfigFileName = "snapshot" + PackedExtension;

        public static readonly string[] StaticExcludedExtensions = [ProgressExtension];

        public static readonly string[] StaticExcludedDirectories = [Utils.OutputFolder, ".git"];

        public static readonly string[] StaticExcludedFiles = [
            ConfigFileName,
            //UserConfigStruct.UpdaterFile,
            CurrentFileName+ ".exe",
            //CurrentFileName+ ".cck",
            CurrentFileName + ".pdb"
        ];

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern uint GetModuleFileNameW(IntPtr hModule, StringBuilder lpFilename, uint nSize);


        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool MoveFileEx(string lpExistingFileName, string lpNewFileName, int dwFlags);
        public static void RemoveOnBoot(string path) => MoveFileEx(path, null, 0x4);

        public static string GetExecutablePath()
        {
            var buffer = new StringBuilder(256);
            uint size = GetModuleFileNameW(IntPtr.Zero, buffer, (uint)buffer.Capacity);
            if (size == 0)
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            return buffer.ToString(0, (int)size);
        }


        //public static byte[] SerealizeJson(object ob) => Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(ob)).Compress().Transform();

        //public static T DeserealizeJson<T>(byte[] dt) => JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(dt.Transform().Decompress()));

        public static FileSystemEnumerable<string> GetDirFiles(string rootDir)
        {
            var enumerationOptions = new EnumerationOptions
            {
                RecurseSubdirectories = true,
                IgnoreInaccessible = true,
                AttributesToSkip = FileAttributes.ReparsePoint, // Evita bucles simbólicos
                ReturnSpecialDirectories = false
            };

            var fileSystemEnumerable = new FileSystemEnumerable<string>(
                rootDir, (ref FileSystemEntry entry) =>
                {
                    return Path.GetRelativePath(rootDir, entry.ToFullPath());
                },
                enumerationOptions)
            {
                ShouldRecursePredicate = (ref FileSystemEntry entry) =>
                    !UserConfigStruct.ExcludedDirectories.Contains(entry.FileName.ToString()),

                ShouldIncludePredicate = (ref FileSystemEntry entry) =>
                {
                    if (entry.IsDirectory)
                        return false; // Solo nos interesan los archivos

                    // Evita Path.Get... para un rendimiento marginalmente mayor dentro del predicado
                    var fileName = entry.FileName;

                    if (UserConfigStruct.ExcludedFiles.Contains(fileName.ToString())) 
                        return false;

                    int lastDotIndex = fileName.LastIndexOf('.');

                    if (lastDotIndex != -1)
                    {
                        var extension = fileName.Slice(lastDotIndex);

                        if (UserConfigStruct.ExcludedExtensions.Contains(extension.ToString())) 
                            return false;
                    }

                    return true;
                }
            };

            return fileSystemEnumerable;
        }

        /*public static string[] OldGetDirFiles(string dir, string? rootDir = null)
        {
          
            if (string.IsNullOrEmpty(dir)) 
                throw new Exception(nameof(dir) + " is null");

            List<string> files = [];

            foreach (var file in Directory.EnumerateFiles(dir))
            {
                if (UserConfigStruct.ExcludedExtensions.Any(f => file.EndsWith(f, StringComparison.InvariantCultureIgnoreCase)))
                    continue;

                //Console.WriteLine(string.Join("; ", UserConfigStruct.ExcludedExtensions));

                if (UserConfigStruct.ExcludedFiles.Any(f => file.EndsWith(f, StringComparison.InvariantCultureIgnoreCase)))
                    continue;

                string filePath = file.Replace(rootDir ?? dir, "").TrimStart('\\');

                if (filePath.EndsWith(PackedExtension, StringComparison.InvariantCultureIgnoreCase))
                    filePath = filePath.Substring(0, filePath.Length - Utils.PackedExtension.Length);

                files.Add(filePath);
            }

            var subdirs = Directory.EnumerateDirectories(dir)
                .Where(subdir => !UserConfigStruct.ExcludedDirectories
                    .Any(exc => subdir.EndsWith(exc, StringComparison.InvariantCultureIgnoreCase)))
                        .ToArray();

            foreach (var subdir in subdirs)
            {
                files.AddRange(OldGetDirFiles(subdir, rootDir ?? dir));
            }

            if (rootDir != null && DeleteEmptyDirectories && files.Count == 0 && subdirs.Length == 0 && !UserConfigStruct.ExcludedDirectories.Any(d => dir.EndsWith(d, StringComparison.InvariantCultureIgnoreCase)))
            {
                Directory.Delete(dir);
            }

            return files.ToArray();
        }*/




        /*public static int RemoveUnmatchingFiles(IEnumerable<string> paths, string directory)
        {
            int filesRemoved = 0;

            var files = GetDirFiles(directory);

            foreach (var file in files)
            {
                if (UserConfigStruct.ExcludedExtensions.Any(e => file.EndsWith(e, StringComparison.InvariantCultureIgnoreCase)))
                    continue;

                if (UserConfigStruct.ExcludedFiles.Any(f => file.EndsWith(f, StringComparison.InvariantCultureIgnoreCase)))
                    continue;

                if (!paths.Contains(file, StringComparer.InvariantCultureIgnoreCase)) //if (!paths.Any(p => string.Equals(file, p, StringComparison.InvariantCultureIgnoreCase)))
                {
                    ConsoleHelper.WriteLine(AnsiColors.Red + "Removing " +
                        AnsiColors.Silver + file);

                    File.Delete(Path.Combine(directory, file));

                    filesRemoved++;
                }
            }

            if (filesRemoved > 0)
                Console.WriteLine();

            return filesRemoved;
        }*/

        public static int RemoveUnmatchingFiles(
        string directory,
        IEnumerable<string> validRelativePaths)
        {
            int filesRemoved = 0;

            var enumerationOptions = new EnumerationOptions
            {
                RecurseSubdirectories = true,
                IgnoreInaccessible = true,
                AttributesToSkip = FileAttributes.ReparsePoint
            };

            var fullPathEnumerable = new FileSystemEnumerable<string>(
                directory,
                (ref FileSystemEntry entry) => entry.ToFullPath(), 
                enumerationOptions)
            {
                ShouldRecursePredicate = (ref FileSystemEntry entry) =>
                    !UserConfigStruct.ExcludedDirectories.Contains(entry.FileName.ToString()),

                ShouldIncludePredicate = (ref FileSystemEntry entry) =>
                {
                    if (entry.IsDirectory) 
                        return false;

                    if (UserConfigStruct.ExcludedFiles.Contains(entry.FileName.ToString())) 
                        return false;

                    int lastDotIndex = entry.FileName.LastIndexOf('.');

                    if (lastDotIndex != -1 && UserConfigStruct.ExcludedExtensions.Contains(entry.FileName.Slice(lastDotIndex).ToString()))
                        return false;

                    return true;
                }
            };

            foreach (var fullPath in fullPathEnumerable)
            {
                string relativePath = Path.GetRelativePath(directory, fullPath.EndsWith(Utils.PackedExtension) ? Path.Combine(Path.GetDirectoryName(fullPath), Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(fullPath))) : fullPath);

                if (!validRelativePaths.Any(x => string.Equals(x, relativePath, StringComparison.OrdinalIgnoreCase)))
                {
                    ConsoleHelper.WriteLine(AnsiColors.Red + "Removing " + AnsiColors.Silver + relativePath);

                    try
                    {
                        File.Delete(fullPath);

                        filesRemoved++;
                    }
                    catch (IOException ex)
                    {
                        // Opcional: Loggear errores si el archivo está bloqueado, etc.
                        // Console.WriteLine($"Error deleting {fullPath}: {ex.Message}");
                    }
                }
            }

            return filesRemoved;
        }

        public static void DeleteEmptyDirectories(string rootDir)
        {
            foreach (var dir in Directory.EnumerateDirectories(rootDir, "*", SearchOption.AllDirectories).Reverse())
            {
                if (UserConfigStruct.ExcludedDirectories.Contains(Path.GetFileName(dir)))
                {
                    continue;
                }

                if (!Directory.EnumerateFileSystemEntries(dir).Any())
                {
                    try
                    {
                        Directory.Delete(dir);
                    }
                    catch (IOException) {  }
                }
            }
        }

        public static void Serialize(Stream s, ConfigStructure c)
        {
            using (var bs = new BrotliStream(s, CompressionLevel.SmallestSize, leaveOpen: true))
            //using (var xs = new XorStream(bs, ByteArrayExtensions.DefaultKey))
            using (var bw = new BinaryWriter(bs, Encoding.UTF8))
            {
                c.Encode(bw);
            }
        }
      
        public static ConfigStructure Deserialize(Stream s)
        {
            //Console.WriteLine(new StreamReader(s).ReadToEnd());

            //using (var xs = new XorStream(s, ByteArrayExtensions.DefaultKey))
            using (var bs = new BrotliStream(s, CompressionMode.Decompress, leaveOpen: true))
            using (var br = new BinaryReader(bs, Encoding.UTF8))
            {
                return ConfigStructure.Decode(br);
            }
        }
       
        public static bool PathEqual(string a, string b)
        {
            return string.Equals(Path.GetFullPath(a), Path.GetFullPath(b), StringComparison.InvariantCultureIgnoreCase);
        }

        public static void ThrowNullConfig() => throw new NullReferenceException(nameof(UserConfigStruct) + " is not loaded, use " + nameof(UserConfigStruct.LoadFrom) + " with valid data to load config.");

        /*
#if DEBUG
        public static void PrintBarChart(KeyValuePair<string, FileStruct>[] data)
        {
            int windowWidth = Console.WindowWidth;

            int maxBarWidth = Math.Max(windowWidth - 30, 10);

            long maxValue = data.Length > 0 ? data.Max(kvp => kvp.Value.CompressedSize) : 1;

            Console.WriteLine(new string('-', windowWidth));

            foreach (var kvp in data)
            {
                int barLength = (int)(((double)kvp.Value.CompressedSize / maxValue) * maxBarWidth);
                string bar = new string('█', barLength);
                Console.WriteLine($"{kvp.Key.PadRight(20)} | {bar.PadRight(maxBarWidth)} {kvp.Value.CompressedSize}");
            }

            Console.WriteLine(new string('-', windowWidth));
        }
        public static IEnumerable<KeyValuePair<string, FileStruct>> InterleaveDictionary(KeyValuePair<string, FileStruct>[] dict, int length)
        {
            //if (dict.Length <= numCores * 2)
                //return dict;

            return dict.OrderByDescending(kvp => kvp.Value.Size)
                .Select((entry, index) => new { entry, index })
                .GroupBy(x => x.index % length)
                .Select(g => g.Select(x => x.entry).ToList())
                .SelectMany(chunk => chunk); ;
        }
#endif
        */
        private const long Kilobyte = 1000;
        private const long Megabyte = Kilobyte * 1000;
        private const long Gigabyte = Megabyte * 1000;
        private const long Terabyte = Gigabyte * 1000;
        private const long Petabyte = Terabyte * 1000;
        private const long Exabyte = Petabyte * 1000;

        private const string DecimalMask = "0.###";

        public static string ByteSizeToString(long size)
        {
            if (size > Exabyte) return (size / ((double)Exabyte)).ToString(DecimalMask) + "EB";
            else if (size > Petabyte) return (size / ((double)Petabyte)).ToString(DecimalMask) + "PB";
            else if (size > Terabyte) return (size / ((double)Terabyte)).ToString(DecimalMask) + "TB";
            else if (size > Gigabyte) return (size / ((double)Gigabyte)).ToString(DecimalMask) + "GB";
            else if (size > Megabyte) return (size / ((double)Megabyte)).ToString(DecimalMask) + "MB";
            else if (size > Kilobyte) return (size / ((double)Kilobyte)).ToString(DecimalMask) + "KB";
            else return size + "B";
        }
    }


    public static class ByteArrayExtensions
    {
        public static byte[] Compress(this byte[] data)
        {
            using (var compressedStream = new MemoryStream())
            {
                using (var zc = new BrotliStream(compressedStream, CompressionLevel.SmallestSize))
                {
                    zc.Write(data, 0, data.Length);
                }

                return compressedStream.ToArray();
            }
        }

        public static byte[] Decompress(this byte[] data)
        {
            using (var compressedStream = new MemoryStream(data))
            {
                using (var dezc = new BrotliStream(compressedStream, CompressionMode.Decompress))
                {
                    using (var resultStream = new MemoryStream())
                    {
                        dezc.CopyTo(resultStream);

                        return resultStream.ToArray();
                    }
                }
            }
        }
    }


    /*public static byte[][] DefaultKey = [
        [0x83, 0x47, 0xd6, 0x01, 0x99, 0x2f, 0xd0, 0x36, 0x46, 0xa6, 0xcc, 0x37, 0x61, 0xf8, 0xc5, 0xd0, 0xc1, 0x74, 0x5c, 0xad, 0x70, 0xf8, 0xde, 0xd7, 0xff, 0xc0, 0xfc, 0x3c, 0xe6, 0x37, 0xfe, 0x20, 0xdc, 0x16, 0x57, 0x4d, 0xf0, 0x6d, 0x9e, 0xc8, 0x04, 0xbc, 0xb1, 0x10, 0x66, 0xeb, 0x71, 0x15, 0xbd, 0xa9, 0x80, 0x6b, 0xf6, 0xd1, 0x1d, 0x2f, 0x1b, 0xf1, 0xb8, 0xb6, 0x74, 0x34, 0x3d, 0xab, 0xf6, 0x2f, 0x15, 0x0e, 0xe9, 0x45, 0x0a, 0x33, 0xe8, 0x14, 0xef, 0x2c, 0xd3, 0xda, 0x00, 0xc8, 0x71, 0x7a, 0x4a, 0x30, 0x0c, 0x73, 0xb7, 0xfe, 0xa5, 0x71, 0x34, 0x13, 0x3b, 0xbb, 0x57, 0xab, 0x87, 0xb4, 0x38, 0x51, 0x45, 0xbd, 0x2a, 0xa9, 0x19, 0x2f, 0x7c, 0x30, 0x25, 0x7f, 0xf2, 0x80, 0x41, 0xa1, 0x32, 0xb9, 0x16, 0x10, 0x74, 0xa1, 0x21, 0x99, 0xdc, 0x87, 0x8e, 0xb1, 0xdc, 0x93, 0x60, 0x8a, 0x89, 0x58, 0xe0, 0x51, 0x51, 0xc3, 0x8c, 0x67, 0xb8, 0x2a, 0x0e, 0x76, 0x31, 0xac, 0x91, 0xa9, 0x48, 0x74, 0x9f, 0xcb, 0x9e, 0xa6, 0xf7, 0x1a, 0xee, 0x8a, 0x1c, 0xd2, 0x44, 0xad, 0xc1, 0x57, 0x44, 0x87, 0x2e, 0xd9, 0xcb, 0xf8, 0x06, 0x46, 0x26, 0xbc, 0x18, 0x4a, 0xe1, 0xd4, 0xdd, 0x68, 0x13, 0x99, 0x72, 0x31, 0xcc, 0x0f, 0x95, 0x17, 0x8e, 0x33, 0x61, 0x03, 0xc5, 0xd8, 0xdf, 0x7c, 0x96, 0xef, 0x9c, 0x27, 0x6e, 0xc6, 0x70, 0x5a, 0x5c, 0x75, 0xfc, 0x65, 0xa1, 0x83, 0x1b, 0xea, 0x71, 0xf9, 0xed, 0x18, 0x31, 0x34, 0x42, 0xa8, 0xbc, 0x4c, 0xd7, 0x4b, 0x3b, 0xaa, 0xc9, 0x44, 0x84, 0x19, 0xe2, 0x45, 0xb2, 0xd9, 0x7c, 0x5d, 0xac, 0x0e, 0xcc, 0xf3, 0x05, 0xca, 0xb3, 0xbc, 0xd9, 0xfb, 0x5f, 0x73, 0x5c, 0x83, 0x8f, 0x25, 0x6e, 0xe4, 0xbd, 0x29, 0x31],
        [0xb9, 0xbb, 0xa8, 0x63, 0x80, 0xee, 0x46, 0x38, 0xdd, 0xdd, 0xf7, 0xce, 0x98, 0xa7, 0xae, 0x65, 0x6a, 0x33, 0xe4, 0xf1, 0x66, 0x6f, 0xc4, 0x26, 0xe7, 0xb1, 0xe0, 0xbf, 0x0b, 0xea, 0xf5, 0x95, 0x82, 0x64, 0xb4, 0x5e, 0x9d, 0xc1, 0x2a, 0x5b, 0xa1, 0x7c, 0x55, 0x2b, 0xc1, 0xb6, 0x3f, 0x4a, 0xaf, 0x1f, 0xe2, 0xef, 0x41, 0xcb, 0x94, 0xcb, 0xdd, 0x4e, 0xeb, 0x87, 0x78, 0x81, 0x0b, 0xbd, 0x74, 0xda, 0x3a, 0x1f, 0x6f, 0xff, 0xef, 0xa5, 0x30, 0xa4, 0xbc, 0x72, 0x93, 0x7b, 0x98, 0xb4, 0x21, 0xf3, 0x3b, 0x07, 0x2b, 0x64, 0x2d, 0xf2, 0xf4, 0x2c, 0x26, 0xdb, 0xa3, 0xb3, 0xdc, 0x79, 0xd8, 0xec, 0xfa, 0x23, 0xe5, 0x57, 0x9a, 0xa9, 0xcc, 0x95, 0x30, 0xf8, 0xfa, 0x9d, 0xe2, 0xd0, 0x50, 0xcb, 0x2f, 0x38, 0x85, 0x00, 0x5d, 0xa5, 0x6c, 0x2e, 0x5b, 0xa0, 0x3c, 0x3d, 0x25, 0x41],
        [0xce, 0x9c, 0x28, 0x33, 0x30, 0x05, 0x94, 0x1e, 0xd0, 0x51, 0x8f, 0xc5, 0x94, 0x18, 0x50, 0x94, 0xa1, 0x97, 0xf1, 0x0a, 0xc9, 0x4b, 0xf6, 0xf3, 0x73, 0xbf, 0x99, 0xb8, 0x5b, 0xb3, 0xe5, 0x4a, 0x1b, 0xf0, 0x1d, 0x1e, 0xd5, 0xa7, 0x59, 0xeb, 0xd3, 0x11, 0x7e, 0xd5, 0x04, 0xb2, 0x40, 0x3a, 0x83, 0xea, 0xb3, 0x15, 0xac, 0x1b, 0x65, 0x6b, 0x88, 0xbf, 0x0e, 0x90, 0xb3, 0x1a, 0x9c, 0xa1],
        [0xcc, 0x42, 0xce, 0x0a, 0xca, 0x42, 0xbe, 0xab, 0xa6, 0x5b, 0xad, 0x53, 0x4f, 0x24, 0x48, 0x92, 0xf4, 0x13, 0x2f, 0x5d, 0x13, 0x70, 0xb2, 0xd7, 0x0e, 0x70, 0x88, 0x65, 0xaa, 0x31, 0x69, 0xfa],
        [0x7a, 0xed, 0xf7, 0x23, 0xc4, 0x1f, 0x9d, 0x7b, 0x9e, 0xcc, 0x0c, 0x36, 0xf4, 0x03, 0xa5, 0x28],
        [0x1a, 0xe3, 0x54, 0xa2, 0xcc, 0xcc, 0xaf, 0x9e],
        [0x3d, 0xc9, 0x66, 0x9a],
        [0xd0, 0x64],
        [0x4b]
    ];*/

    /*public static byte[] Transform(this byte[] data) => Transform(data, DefaultKey);

    public static byte[] Transform(this byte[] data, byte[] key)
    {
        byte[] result = new byte[data.Length];

        for (int i = 0; i < data.Length; i++)
        {
            result[i] = (byte)(data[i] ^ key[i % key.Length]);
        }

        return result;
    }*/
}
