using FilesUpdaterLib;
using System.Diagnostics;
using System.IO.Compression;
using ZstdSharp;
using ZstdSharp.Unsafe;

namespace FilesCompiler
{
    internal class Program
    {
        static void BenchmarkCompression(string inputFile)
        {
            Console.WriteLine("Benchmark results for: " + inputFile);

            var bufferSize = 86 * 1024 * 1024;
            byte[] buffer = new byte[bufferSize]; // 1 MB copy buffer

            // --- ZstdSharp.Port ---
            string zstdFile = inputFile + ".zst";
            var zstdWatch = Stopwatch.StartNew();
            using (var inFs = File.OpenRead(inputFile))
            using (var outFs = File.Create(zstdFile))
            using (var zc = new CompressionStream(outFs, level: 22, bufferSize))
            {
                zc.SetParameter(ZSTD_cParameter.ZSTD_c_windowLog, 27); // Already set, but confirm
                zc.SetParameter(ZSTD_cParameter.ZSTD_c_hashLog, 27); // Increase for better initial matching
                zc.SetParameter(ZSTD_cParameter.ZSTD_c_chainLog, 30); // Higher than 27 for deeper chaining
                zc.SetParameter(ZSTD_cParameter.ZSTD_c_searchLog, 8); // Slightly higher for more searches
                zc.SetParameter(ZSTD_cParameter.ZSTD_c_minMatch, 3); // Lower for potentially better ratio

                // Enable Long Distance Matching for potential ratio improvements on structured data
                zc.SetParameter(ZSTD_cParameter.ZSTD_c_enableLongDistanceMatching, 1);
                zc.SetParameter(ZSTD_cParameter.ZSTD_c_ldmHashLog, 24); // High for better LDM
                zc.SetParameter(ZSTD_cParameter.ZSTD_c_ldmMinMatch, 64); // Default-like
                zc.SetParameter(ZSTD_cParameter.ZSTD_c_ldmHashRateLog, 0); // Low for denser hashing, better ratio but slower
                zc.SetParameter(ZSTD_cParameter.ZSTD_c_ldmBucketSizeLog, 3); // Default

                int read;
                while ((read = inFs.Read(buffer, 0, buffer.Length)) > 0)
                    zc.Write(buffer, 0, read);
            }
            zstdWatch.Stop();
            long zstdSize = new FileInfo(zstdFile).Length;
            double zstdRatio = (double)zstdSize / new FileInfo(inputFile).Length;

            Console.WriteLine($"ZstdSharp.Port: {zstdWatch.ElapsedMilliseconds} ms, Size: {zstdSize:N0} bytes, Ratio: {zstdRatio:P2}");

            // --- BrotliStream ---
            string brFile = inputFile + ".br";
            var brWatch = Stopwatch.StartNew();
            using (var inFs = File.OpenRead(inputFile))
            using (var outFs = File.Create(brFile))
            using (var br = new BrotliStream(outFs, CompressionLevel.SmallestSize))
            {
                int read;
                while ((read = inFs.Read(buffer, 0, buffer.Length)) > 0)
                    br.Write(buffer, 0, read);
            }
            brWatch.Stop();
            long brSize = new FileInfo(brFile).Length;
            double brRatio = (double)brSize / new FileInfo(inputFile).Length;

            // --- Results ---
            Console.WriteLine($"BrotliStream  : {brWatch.ElapsedMilliseconds} ms, Size: {brSize:N0} bytes, Ratio: {brRatio:P2}");
        }
        static void Main(string[] args)
        {
            //Compiler.BrotliFile("C:\\Riot Games\\VALORANT\\VALORANT\\live\\ShooterGame\\Content\\Paks\\pakchunk10-WindowsClient.ucas", "C:\\Users\\Mrgaton\\Downloads\\zstd-v1.5.7-win64\\zstd-v1.5.7-win64\\output.file.br");

            /*BenchmarkCompression("C:\\Users\\mrgaton\\Downloads\\Downloads.zip");

            return;*/


            Console.WriteLine("FilesCompiler!!!!");

#if DEBUG
            args = ["compile", "..\\..\\..\\..\\FilesUpdater\\updater.config.ini", "C:\\Riot Games\\VALORANT\\VALORANT\\live", "https://github.com/Mrgaton/Fornaitinthemelodies.git"];
            //args = ["git", "C:\\Users\\Mrgaton\\Downloads\\Risk of Rain 2", "https://github.com/Mrgaton/Riesgoaltodelluvianivel2.git"];
#endif

            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Idle;

            if (args.Length > 0)
            {
                Console.WriteLine(@"Usage:
    compile 'configFile' 'pathToCompile' 'repoUrl'
    git  'filesDir' 'repoUrl'");

                if (string.Equals(args[0], "compile", StringComparison.InvariantCultureIgnoreCase))
                {
                    string path = Path.GetFullPath(args[1]);

                    Console.WriteLine("Config path: " + path);
                    UserConfigStruct.LoadFrom(File.ReadAllText(args[1]));

                    Console.WriteLine("Target Path: " + Path.GetFullPath(args[2]));
                    Compiler.Compile(args[2] ?? ".");

                    if (args.Length >= 4 && !string.IsNullOrEmpty(args[3]))
                    {
                        GitConfigure.SetupAndPushNewRepoHistory(Path.Combine(args[2], Utils.OutputFolder), args[3]);
                    }
                }
                else if (string.Equals(args[0], "git", StringComparison.InvariantCultureIgnoreCase))
                {
                    string path = Path.GetFullPath(args[1]);

                    GitConfigure.SetupAndPushNewRepoHistory(Path.Combine(path, Utils.OutputFolder), args[2]);

                    throw new Exception("Not working now");
                }
                else
                {
                    Console.WriteLine("Uknown argument :c");
                }
            }
        }
    }
}
