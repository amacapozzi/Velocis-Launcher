#define Parallel

using FilesUpdaterLib.Helper;
using System.Buffers;
using System.Collections.Concurrent;
using System.Drawing;
using System.IO.Compression;
using System.Runtime.InteropServices;

namespace FilesUpdaterLib
{
    public static class Compiler
    {
        static class IconExtractor
        {
            // Resource types
            static readonly IntPtr RT_GROUP_ICON = (IntPtr)14;
            static readonly IntPtr RT_ICON = (IntPtr)3;
            const uint LOAD_LIBRARY_AS_DATAFILE = 0x00000002;

            [StructLayout(LayoutKind.Sequential, Pack = 2)]
            struct GRPICONDIR
            {
                public short reserved;   // must be 0
                public short type;       // 1 for icons
                public short count;      // number of entries
            }

            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            struct GRPICONDIRENTRY
            {
                public byte width;
                public byte height;
                public byte colorCount;
                public byte reserved;
                public short planes;
                public short bitCount;
                public int bytesInRes;
                public short id;         // resource ID (MAKEINTRESOURCE)
            }

            // P/Invoke
            [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
            static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hFile, uint dwFlags);
           
            [DllImport("kernel32.dll", SetLastError = true)]
            static extern bool FreeLibrary(IntPtr hModule);
            
            [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
            static extern IntPtr FindResource(IntPtr hModule, IntPtr lpName, IntPtr lpType);
          
            [DllImport("kernel32.dll", SetLastError = true)]
            static extern IntPtr LoadResource(IntPtr hModule, IntPtr hResInfo);
           
            [DllImport("kernel32.dll")]
            static extern IntPtr LockResource(IntPtr hResData);
           
            [DllImport("kernel32.dll", SetLastError = true)]
            static extern uint SizeofResource(IntPtr hModule, IntPtr hResInfo);
          
            [DllImport("user32.dll")]
            static extern IntPtr CreateIconFromResourceEx(IntPtr presbits, uint cb, bool fIcon, uint version, int cxDesired, int cyDesired, uint flags);
            
            [DllImport("user32.dll", SetLastError = true)]
            static extern bool DestroyIcon(IntPtr hIcon);
            
            [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
            static extern bool EnumResourceNames(IntPtr hModule, IntPtr lpszType, EnumResNameProc lpEnumFunc, IntPtr lParam);
            
            delegate bool EnumResNameProc(IntPtr hModule, IntPtr lpszType, IntPtr lpszName, IntPtr lParam);

            public static List<Icon> ExtractAll(string exePath, int desiredSize = 256)
            {
                List<Icon> output = [];

                IntPtr hMod = LoadLibraryEx(exePath, IntPtr.Zero, LOAD_LIBRARY_AS_DATAFILE);

                if (hMod == IntPtr.Zero)
                    return output;

                try
                {
                    EnumResNameProc proc = (h, t, name, lparam) =>
                    {
                        try
                        {
                            IntPtr hResInfo = FindResource(hMod, name, RT_GROUP_ICON);

                            if (hResInfo == IntPtr.Zero) 
                                return true;

                            IntPtr hResData = LoadResource(hMod, hResInfo);

                            if (hResData == IntPtr.Zero) 
                                return true;

                            IntPtr pDir = LockResource(hResData);

                            if (pDir == IntPtr.Zero)
                                return true;

                            var dir = Marshal.PtrToStructure<GRPICONDIR>(pDir);

                            if (dir.count <= 0) 
                                return true;

                            int entrySz = Marshal.SizeOf<GRPICONDIRENTRY>();
                            GRPICONDIRENTRY best = default;
                            long bestArea = -1;

                            for (int i = 0; i < dir.count; i++)
                            {
                                IntPtr entPtr = IntPtr.Add(pDir, Marshal.SizeOf<GRPICONDIR>() + i * entrySz);
                                var entry = Marshal.PtrToStructure<GRPICONDIRENTRY>(entPtr);

                                int w = (entry.width == 0) ? 256 : entry.width;
                                int hgt = (entry.height == 0) ? 256 : entry.height;
                                long area = (long)w * hgt;

                                if (bestArea < area || (bestArea == area && entry.bitCount > best.bitCount))
                                {
                                    best = entry;
                                    bestArea = area;
                                }
                            }

                            if (best.id == 0) 
                                return true;

                            IntPtr hIconRes = FindResource(hMod, (IntPtr)best.id, RT_ICON);

                            if (hIconRes == IntPtr.Zero) 
                                return true;

                            IntPtr hIconData = LoadResource(hMod, hIconRes);

                            if (hIconData == IntPtr.Zero) 
                                return true;

                            IntPtr pBits = LockResource(hIconData);

                            if (pBits == IntPtr.Zero) 
                                return true;

                            uint size = (best.bytesInRes > 0) ? (uint)best.bytesInRes : SizeofResource(hMod, hIconRes);

                            if (size == 0) 
                                return true;

                            IntPtr hIcon = CreateIconFromResourceEx(pBits, size, true, 0x00030000, desiredSize, desiredSize, 0);

                            if (hIcon != IntPtr.Zero)
                            {
                                using (Icon temp = Icon.FromHandle(hIcon))
                                {
                                    var ico = (Icon)temp.Clone();

                                    output.Add(ico);
                                }

                                DestroyIcon(hIcon);
                            }
                        }
                        catch
                        {
                            // swallow errors for this entry and continue enumeration
                        }

                        return true; // continue
                    };

                    EnumResourceNames(hMod, RT_GROUP_ICON, proc, IntPtr.Zero);
                }
                finally
                {
                    FreeLibrary(hMod);
                }

                return output;
            }
        }
        public static void Compile(string? path = null)
        {
            try
            {
                var exe = Directory.GetFiles(path, "*.exe").FirstOrDefault();

                if (exe != null)
                {
                    var icons = IconExtractor.ExtractAll(exe);

                    if (icons.Count != 0)
                    {
                        var biggest = icons
                        .OrderByDescending(ic => ic.Width * ic.Height)
                        .First();


                        using var fsa = File.OpenWrite("..\\..\\..\\..\\FilesUpdater\\app.ico");

                        biggest.Save(fsa);
                    }
                }
            }
            catch(Exception ex)
            {
                Console.Error.WriteLine(ex.ToString());
            }
           

            string tarjetPath = path ?? UserConfigStruct.InstallPath;
            string outputPath = Path.Combine(tarjetPath, Utils.OutputFolder);

            if (!UserConfigStruct.Loaded)
                Utils.ThrowNullConfig();

            if (!Directory.Exists(outputPath))
                Directory.CreateDirectory(outputPath);

            ConfigStructure cs = new ConfigStructure();

            Utils.RemoveUnmatchingFiles(outputPath, Utils.GetDirFiles(tarjetPath));

            ConcurrentDictionary<string, FileStruct> filesStruct = [];

            string structurePath = Path.Combine(outputPath, Utils.ConfigFileName);

            ConfigStructure oldSnapshot = null;

            if (File.Exists(structurePath))
            {
                using (FileStream fs = File.OpenRead(structurePath))
                {
                    oldSnapshot = Utils.Deserialize(fs);
                }
            }

#if Parallel
            Parallel.ForEach(Utils.GetDirFiles(tarjetPath), new ParallelOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount * 2 }, relativePath =>
            {
#else
            foreach (string relativePath in Utils.GetDirFiles(tarjetPath))
            {
#endif
                string filePath = Path.Combine(tarjetPath, relativePath);

                if (!File.Exists(filePath))
                {
                    Console.WriteLine("Fatal error " + filePath + " not found.");
                    return;
                }

                Console.WriteLine("Hashing [" + relativePath + "]");

                uint currentHash = Hasher.Hash(filePath);

                FileStruct? fileStruct = null;

                //Console.WriteLine((oldSnapshot == null) + "||" + !oldSnapshot.Files.ContainsKey(relativePath) +"||"+ (oldSnapshot.Files.TryGetValue(relativePath, out fileStruct) && currentHash != fileStruct.Hash));

                string packedPath = Path.Combine(tarjetPath, Utils.OutputFolder, relativePath + Utils.PackedExtension).ToLower();

                bool needsToCompile = oldSnapshot == null || !oldSnapshot.Files.ContainsKey(relativePath) || currentHash != (fileStruct = oldSnapshot.Files[relativePath]).Hash;

                if (needsToCompile)
                {
                    string outputFolder = Path.GetDirectoryName(packedPath);

                    if (!Directory.Exists(outputFolder))
                        Directory.CreateDirectory(outputFolder);

                    BrotliFile(tarjetPath, relativePath);

                    /*Console.WriteLine("Brotling [" + relativePath + "]");
                    string brotliPackedPath = Path.Combine(tarjetPath, Utils.OutputFolder, relativePath + ".brotli").ToLower();
                    BrotliFile(filePath, brotliPackedPath);*/

                    fileStruct = null;
                }


                /*if (needsToCompile)
                {
                    SplitChunks(tarjetPath, relativePath, packedInfo.Length);
                }*/

                long fileLength = fileStruct != null ? fileStruct.Size : new FileInfo(filePath).Length;

                filesStruct.TryAdd(relativePath, new FileStruct()
                {
                    Hash = currentHash,
                    //CompressedSize = !needsToCompile ? fileStruct.Size : packedInfo.Length,
                    Size = fileLength
                });
#if Parallel
            });
#else
            }
#endif
            cs.ChunkSize = Utils.ChunkSize;

            cs.Files = filesStruct;

            if (filesStruct.Any(fs => string.IsNullOrWhiteSpace(fs.Key) || fs.Value == null))
                throw new InvalidDataException();

            using (FileStream fs = File.OpenWrite(structurePath))
            {
                fs.SetLength(0);

                Utils.Serialize(fs, cs);
            }

            // Console.WriteLine(JsonConvert.SerializeObject(cs));

            //File.WriteAllBytes(structurePath, Utils.SerealizeJson(cs));
        }

        private static string CreateChunkPath(string tarjetPath, string relativePath, int chunkNum) => Path.Combine(tarjetPath, Utils.OutputFolder, relativePath + '.' + (chunkNum >= 0 ? chunkNum.ToString() : '*'.ToString()) + Utils.PackedExtension).ToLower();
        private static string CreateChunkPath(string filePath, int chunkNum) => Path.Combine(filePath + '.' + chunkNum.ToString() + Utils.PackedExtension).ToLower();
        private static void SplitChunks(string tarjetPath, string relativePath, long fileSize)
        {
            string packedPath = Path.Combine(tarjetPath, Utils.OutputFolder, relativePath + Utils.PackedExtension).ToLower();

            if (fileSize <= Utils.ChunkSize)
            {
                File.Move(packedPath, CreateChunkPath(tarjetPath, relativePath, 0), true);
                return;
            }

            string path = CreateChunkPath(tarjetPath, relativePath, -1);

            foreach (var f in Directory.GetFiles(Path.GetDirectoryName(path), Path.GetFileName(path)))
            {
                Console.WriteLine("Removing " + f);

                File.Delete(f);
            }

            using (FileStream fs = File.OpenRead(packedPath))
            {
                byte[] buffer = new byte[Utils.ChunkSize];

                int totalChunks = (int)(fileSize / Utils.ChunkSize) + (fileSize % Utils.ChunkSize > 0 ? 1 : 0);

                for (int i = 0; i < totalChunks; i++)
                {
                    var chunkPath = CreateChunkPath(tarjetPath, relativePath, i);

                    using (FileStream fw = File.OpenWrite(chunkPath))
                    {
                        int readed = fs.Read(buffer, 0, buffer.Length);

                        fw.Write(buffer, 0, readed);
                    }
                }
            }

            File.Delete(packedPath);
        }
        /*public static void DeflateFile(string sourceFile, string destinationFile)
        {
            using (FileStream originalFileStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read))
            {
                using (FileStream compressedFileStream = new FileStream(destinationFile, FileMode.Create))
                {
                    using (DeflateStream zc = new DeflateStream(compressedFileStream, new ZLibCompressionOptions() { CompressionLevel = 9, CompressionStrategy = ZLibCompressionStrategy.Default }, false))
                    {
                        originalFileStream.CopyTo(zc);
                    }
                }
            }
        }*/

        /*public static void BrotliFile(string sourceFile, string destinationFile)
        {
            using (FileStream originalFileStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read))
            {
                using (FileStream compressedFileStream = new FileStream(destinationFile, FileMode.Create))
                {
                    //BrotliEncoder enc = new BrotliEncoder(11, 24);
               
                    using (BrotliStream zc = new BrotliStream(compressedFileStream,  CompressionLevel.SmallestSize, leaveOpen: false))
                    {
                        originalFileStream.CopyTo(zc);
                    }
                }
            }
        }*/

        /*public static void BrotliFile(string sourceFile, string destinationFile)
        {
            int quality = 11;
            
            // Configure the process start information.
            var startInfo = new ProcessStartInfo
            {
                FileName = "brotli.exe",
                Arguments = $"-Z --large_window=28 -n -o \"{destinationFile}\" \"{sourceFile}\"",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using (var process = new Process { StartInfo = startInfo })
            {
                try
                {
                    process.Start();

                    string output = process.StandardOutput.ReadToEnd();

                    string error = process.StandardError.ReadToEnd();

                     process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        throw new Exception($"Brotli compression failed with exit code {process.ExitCode}: {error}");
                    }
                }
                catch (Exception ex)
                {
                    // Handle any exceptions that occur during process execution.
                    throw new Exception("An error occurred while compressing the file.", ex);
                }
            }
        }*/

        private const int BufferSize = 81920 * 8;

        public static void BrotliFile(string tarjetPath, string relativePath)
        {
            var sourceFile = Path.Combine(tarjetPath, relativePath);

            using (FileStream stream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.SequentialScan))
            {
                long chunks = (stream.Length / Utils.ChunkSize);

                Parallel.For(0, chunks + 1, (long i) =>
                {
                    using(var vstream = new ChunkedViewStream(stream, i, Utils.ChunkSize))
                    {
                        using (FileStream compressedStream = new FileStream(CreateChunkPath(tarjetPath,relativePath, (int)i), FileMode.CreateNew, FileAccess.Write, FileShare.None, BufferSize, FileOptions.SequentialScan))
                        {
                            // Create the BrotliEncoder with maximum quality (11) and maximum window size (24).

                            int quality = SelectQuality(vstream.Length, Path.GetExtension(sourceFile));

                            Console.WriteLine("Compressing [" + sourceFile + "] L:" + quality);
                            //Console.WriteLine("Compressing " + sourceFile + " with quality " + quality);

                            using (var encoder = new BrotliEncoder(quality, 24))
                            {
                                byte[] inputBuffer = new byte[BufferSize];
                                byte[] outputBuffer = new byte[BufferSize + 4096];

                                bool endOfFile = false;

                                while (!endOfFile)
                                {
                                    int bytesRead = vstream.Read(inputBuffer, 0, inputBuffer.Length);

                                    if (bytesRead == 0)
                                    {
                                        endOfFile = true;
                                    }

                                    ReadOnlySpan<byte> inputSpan = inputBuffer.AsSpan(0, bytesRead);

                                    while (true)
                                    {
                                        Span<byte> outputSpan = outputBuffer.AsSpan();

                                        bool isFinalBlock = endOfFile && inputSpan.IsEmpty;

                                        OperationStatus lastResult = encoder.Compress(inputSpan, outputSpan, out int bytesConsumed, out int bytesWritten, isFinalBlock);

                                        if (bytesWritten > 0)
                                        {
                                            compressedStream.Write(outputBuffer, 0, bytesWritten);
                                        }

                                        if (lastResult == OperationStatus.Done)
                                        {
                                            break;
                                        }
                                        else if (lastResult == OperationStatus.NeedMoreData)
                                        {
                                            if (bytesConsumed > 0)
                                            {
                                                inputSpan = inputSpan.Slice(bytesConsumed);
                                            }

                                            break;
                                        }
                                        else if (lastResult == OperationStatus.DestinationTooSmall)
                                        {
                                            if (bytesConsumed > 0)
                                            {
                                                inputSpan = inputSpan.Slice(bytesConsumed);
                                            }

                                            continue;
                                        }
                                        else
                                        {
                                            throw new InvalidOperationException($"Brotli compression failed with status: {lastResult}");
                                        }
                                    }
                                }

                                while (true)
                                {
                                    Span<byte> outputSpan = outputBuffer.AsSpan();

                                    OperationStatus flushStatus = encoder.Flush(outputSpan, out int bytesWritten);

                                    if (bytesWritten > 0)
                                    {
                                        compressedStream.Write(outputBuffer, 0, bytesWritten);
                                    }

                                    if (flushStatus == OperationStatus.Done)
                                    {
                                        break;
                                    }
                                    else if (flushStatus == OperationStatus.DestinationTooSmall)
                                    {
                                        // Keep flushing until Done; continue loop to write more into the stream.
                                        continue;
                                    }
                                    else if (flushStatus != OperationStatus.DestinationTooSmall)
                                    {
                                        throw new InvalidOperationException($"Brotli flush failed with status: {flushStatus}");
                                    }
                                }

                                compressedStream.Flush();
                            }
                        }
                    }
                });
            }
        }

        private static readonly string[] NonCompresableExt = [".zip", ".rar", ".7z", ".gz", ".bz2", ".xz", ".tar.gz", ".tgz", ".tar.bz2", ".tbz2", ".tar.xz", ".txz", ".zst", ".br", ".jar", ".war", ".ear", ".epub", ".jpg", ".jpeg", ".webp", ".avif", ".heic", ".heif", ".jp2", ".j2k", ".mp4", ".m4v", ".mov", ".avi", ".mkv", ".webm", ".flv", ".mpg", ".mpeg", ".wmv", ".ogv", ".3gp", ".3g2", ".mp3", ".aac", ".m4a", ".ogg", ".oga", ".opus", ".flac", ".wma"];
        private static int SelectQuality(long length, string ext)
        {
            if (NonCompresableExt.Contains(ext, StringComparer.OrdinalIgnoreCase))
                return 1;

            if (length > 125L << 20) return 4;
            else if (length > 100L << 20) return 5;
            else if (length > 75L << 20) return 7;
            else if (length > 55L << 20) return 9;
            else if (length > 25L << 20) return 10;
            else return 11;
        }
    }
}