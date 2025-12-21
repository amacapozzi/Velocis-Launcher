using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static AnsiHelper;

namespace FilesUpdaterLib
{
    public static class FilesChecker
    {
        private static string LocalConfigFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FilesUpdaterData");
        private static string LocalConfigPath { get => Path.Combine(LocalConfigFolder, RemoveInvalidChars(UserConfigStruct.ProyectName) + ".snapshot"); }

        private static ConfigStructure LocalConfig = new ConfigStructure();
        public static string RemoveInvalidChars(string filename)
        {
            return string.Concat(filename.Split(Path.GetInvalidFileNameChars()));
        }

        private static int RetryTime = 4;

        //private static Random rand = new Random();
        private static void UpdateTitle(long downloaded = 0, long total = 0)
        {
            if (total > 0)
            {
                Console.Title = UserConfigStruct.ProyectName + "Updater downloaded " + Utils.ByteSizeToString(downloaded) + " / " + Utils.ByteSizeToString(total);
            }
            else
            {
                Console.Title = UserConfigStruct.ProyectName + "Updater starting";
            }
        }
        
        public static async Task Check()
        {
            //if (!UserConfigStruct.Loaded) Utils.ThrowNullConfig();

            UpdateTitle();

            if (!Directory.Exists(LocalConfigFolder)) 
                Directory.CreateDirectory(LocalConfigFolder);

            if (!Directory.Exists(UserConfigStruct.InstallPath))
                Directory.CreateDirectory(UserConfigStruct.InstallPath);

            //string updaterPath = Path.Combine(UserConfigStruct.InstallPath, UserConfigStruct.UpdaterFile);

            //ConfigStructure config = JsonConvert.DeserializeObject<ConfigStructure>(File.ReadAllText(Path.Combine(UserConfig.InstallPath, Helper.OutputFolder, Helper.ConfigFileName)));
            ConsoleHelper.WriteLine(AnsiColors.LightGray + '[' + AnsiColors.Silver + "Info" + AnsiColors.LightGray + ']' + AnsiColors.Cyan + " Fetching");


            var config = await ServerHelper.FetchConfig();

            Console.SetCursorPosition(0, Console.CursorTop);
            ConsoleHelper.WriteLine(AnsiColors.LightGray + '[' + AnsiColors.Silver + "Info" + AnsiColors.LightGray + ']' + AnsiColors.Orange + " Starting checks\n");

            bool configExist = File.Exists(LocalConfigPath);

            try
            {
                if (configExist)
                {
                    using (FileStream fs = File.Open(LocalConfigPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        LocalConfig = Utils.Deserialize(fs);

                        configExist = LocalConfig.Files.Count > 0;
                    }
                }
            }
            catch
            {
                if (configExist)
                    File.Delete(LocalConfigPath);
            }

            if (LocalConfig.Files == null)
                LocalConfig.Files = [];

            Utils.RemoveUnmatchingFiles(UserConfigStruct.InstallPath, config.Files.Select(kvp => kvp.Key));
            Utils.DeleteEmptyDirectories(UserConfigStruct.InstallPath);

            /*if (Utils.RemoveUnmatchingFiles(config.Files.Select(kvp => kvp.Key), UserConfigStruct.InstallPath) > 0)
            {
                Utils.GetDirFiles();
            }*/

            Stopwatch sw = new Stopwatch();


            var filesToFix = GetFilesToFix(config);
            int fileCount = filesToFix.Length;

#if DEBUG
            int threads = 1;
#else
            int threads = Math.Min((Environment.ProcessorCount * 2), (32 * 3));
#endif
           
            if (fileCount > 0)
            {
                long totalToDownload = filesToFix.Sum(f => f.Value.Size);
                long currentDownloaded = 0;

                UpdateTitle(currentDownloaded, totalToDownload);
                
                int downloadingIndex = 0;
                int downloadedIndex = 0;

                //var ftf = filesToFix.AsParallel().AsOrdered().OrderByDescending((f) => f.Value.CompressedSize);
                var ftf = filesToFix.OrderByDescending(f => f.Value.Size).AsParallel().AsOrdered();

                //Parallel.ForEach(Utils.InterleaveDictionary(filesToFix, Math.Max(filesToFix.Length / Environment.ProcessorCount, 4)), new ParallelOptions { MaxDegreeOfParallelism = threads }, file =>
                
                await Parallel.ForEachAsync(ftf, new ParallelOptions { MaxDegreeOfParallelism = threads, CancellationToken = CancellationToken.None }, async (file,ct) =>
                {
                    string filePath = Path.Combine(UserConfigStruct.InstallPath, file.Key);

                    if (string.Equals(file.Key, UserConfigStruct.UpdaterFile, StringComparison.OrdinalIgnoreCase) && string.Equals(filePath, Utils.ExecutablePath, StringComparison.OrdinalIgnoreCase))
                    {
                        string tempPath = Path.GetTempFileName();
                        File.Move(Utils.ExecutablePath, tempPath, true);
                        Utils.RemoveOnBoot(tempPath);
                    }

                retry:

                    try
                    {
                        FileStruct remoteFile = config.Files[file.Key];
                        FileInfo info = new FileInfo(filePath);

                        downloadingIndex++;

                        ConsoleHelper.WriteLine(AnsiColors.Yellow + "Fixing " +
                            AnsiColors.LightGray + '[' +
                            AnsiColors.BrightYellow + Math.Round((downloadingIndex / (double)fileCount) * 100, 1).ToString("0.0") + '%' +
                            AnsiColors.LightGray + "] " +
                            AnsiColors.Silver + file.Key);

                        await ServerHelper.DownloadFileAsync(file.Key, filePath, file.Value, config.ChunkSize);

                        info.Refresh();

                        FileStruct localData = new()
                        {
                            LastModified = info.LastWriteTimeUtc.Ticks,
                            Size = info.Length,
                            Hash = remoteFile.Hash
                        };

                        LocalConfig.Files[file.Key] = localData;
                        currentDownloaded += file.Value.Size;
                        UpdateTitle(currentDownloaded, totalToDownload);

                        downloadedIndex++;

                        ConsoleHelper.WriteLine(AnsiColors.Green + "Fixed " +
                            AnsiColors.LightGray + '[' +
                            AnsiColors.BrightGreen + Math.Round((downloadedIndex / (double)fileCount) * 100, 1).ToString("0.0") + '%' +
                            AnsiColors.LightGray + "] " +
                            AnsiColors.Silver + file.Key);
                    }
                    catch (Exception ex)
                    {
                        downloadingIndex--;

                        Console.WriteLine(AnsiColors.Red + "\nSomething went wrong retrying in " + (RetryTime++) + " seconds:");

                        if (ex is AggregateException)
                        {
                            Exception innerEx = ex.InnerException;

                            while (innerEx != null)
                            {
                                Console.WriteLine(AnsiColors.Red + $"\n\nInner exception:\n" + innerEx.ToString());

                                innerEx = innerEx.InnerException;
                            }
                        }
                        else
                        {
                            Console.WriteLine(ex.ToString());
                        }

                        await Task.Delay(RetryTime * 1000);
                        goto retry;
                    }
                });

                if (downloadingIndex > 0)
                {
                    Console.WriteLine();
                }
            }

            if (!configExist || filesToFix.Length > 0)
            {
                using (FileStream fs = File.Open(LocalConfigPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
                {
                    fs.SetLength(0);

                    Utils.Serialize(fs, LocalConfig);
                }
            }
        }

        /*public static KeyValuePair<string, FileStruct>[] GetFilesToFix(ConfigStructure serverConfig)
        {
            // Copy to array so we only enumerate once
            var allFiles = serverConfig.Files.ToArray();

            bool anyChecked = false;

            // Process in parallel via PLINQ
            var toFix = allFiles
                .AsParallel()
                .WithDegreeOfParallelism(Math.Min(Environment.ProcessorCount, 16))
                .SelectMany(kvp =>
                {
                    var list = new List<KeyValuePair<string, FileStruct>>();

                    string fileKey = kvp.Key;
                    string fullPath = Path.Combine(UserConfigStruct.InstallPath, fileKey);

                    var info = new FileInfo(fullPath);

                    serverConfig.Files.TryGetValue(fileKey, out var remoteData);
                    LocalConfig.Files.TryGetValue(fileKey, out var localData);
                    localData ??= new FileStruct();

                    bool needsHashCheck =
                        !info.Exists ||
                        remoteData.Hash != localData.Hash ||
                        info.Length != remoteData.Size ||
                        info.LastWriteTimeUtc.Ticks != localData.LastModified;

                    if (needsHashCheck)
                    {
                        anyChecked = true;

                        ConsoleHelper.WriteLine(AnsiColors.LightBlue, "Checking ", AnsiColors.LightGray, fileKey);

                        if (info.Exists)
                        {
                            uint hash = Hasher.Hash(fullPath);

                            if (hash == remoteData.Hash)
                            {
                                // If size mismatch, truncate or pad locally
                                if (info.Length != remoteData.Size)
                                {
                                    if (info.Length > remoteData.Size)
                                        File.Delete(fullPath);
                                    else
                                        using (var fs = File.OpenWrite(fullPath))
                                            fs.SetLength(remoteData.Size);

                                    info.Refresh();
                                }

                                // Update local record
                                var updated = new FileStruct
                                {
                                    Hash = hash,
                                    Size = info.Length,
                                    LastModified = info.LastWriteTimeUtc.Ticks
                                };

                                LocalConfig.Files[fileKey] = updated;

                                return list; // this file is up-to-date now
                            }
                        }

                        // If we get here, file really needs fixing
                        list.Add(kvp);
                    }

                    return list;
                })
                .ToArray();

            if (anyChecked)
                Console.WriteLine();

            return toFix;
        }*/


        public static KeyValuePair<string, FileStruct>[] GetFilesToFix(ConfigStructure serverConfig)
        {
            ConcurrentBag<KeyValuePair<string, FileStruct>> filesToFix = [];

            bool checkedFiles = false;

#if DEBUG
            //Console.WriteLine(JsonConvert.SerializeObject(serverConfig));
#endif

            var files = serverConfig.Files.ToArray();

            Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = Math.Min(Environment.ProcessorCount, 16) }, keyPair =>
            {
                string file = keyPair.Key;

                string filePath = Path.Combine(UserConfigStruct.InstallPath, file);

                FileInfo info = new FileInfo(filePath);

                FileStruct remoteData = serverConfig.Files[file];
                FileStruct localData = null;

                bool lcExist = LocalConfig.Files.TryGetValue(file, out localData);

                if (!lcExist)
                    localData = new FileStruct();

                bool modifiedSure = !lcExist || !info.Exists || remoteData.Hash != localData.Hash || info.Length != remoteData.Size;

                if (modifiedSure || info.LastWriteTimeUtc.Ticks != localData.LastModified)
                {
                    if (info.Exists)
                    {
                        checkedFiles = true;

                        ConsoleHelper.WriteLine(AnsiColors.LightBlue + "Checking " + AnsiColors.LightGray + file);

                        uint hash = Hasher.Hash(filePath);

                        if (hash == remoteData.Hash)
                        {
                            if (info.Length != remoteData.Size)
                            {
                                if (info.Length > remoteData.Size)
                                {
                                    File.Delete(filePath);
                                }
                                else
                                {
                                    using (FileStream fs = File.OpenWrite(filePath))
                                    {
                                        fs.SetLength(remoteData.Size);
                                    }

                                    info.Refresh();
                                }
                            }

                            keyPair.Value.LastModified = info.LastWriteTimeUtc.Ticks;
                            keyPair.Value.Size = info.Length;
                            keyPair.Value.Hash = hash;

                            LocalConfig.Files[keyPair.Key] = keyPair.Value;
                            return;
                        }
                        //else throw new InvalidDataException("WTF downloaded data doesnt match expected one in " + file);
                    }

                    filesToFix.Add(keyPair);
                }
            });

            if (checkedFiles)
                Console.WriteLine();

            return filesToFix.ToArray();
        }
    }
}