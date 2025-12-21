
using FilesUpdaterLib;
using FilesUpdaterLib.Helper;
using Global.Properties;
using Microsoft.Win32;
using ShellLink;
using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using static AnsiHelper;
using File = System.IO.File;

namespace FilesUpdater
{
    internal static class Program
    {

        //Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass -Force;Write-Host 'Installing WinGet to resolve dependencies...';Install-PackageProvider -Name NuGet -Force | Out-Null;Install-Module -Name Microsoft.WinGet.Client -Force -Repository PSGallery | Out-Null;Repair-WinGetPackageManager;

        private static string wingetPowershellScript = Dec([0x1B, 0x22, 0x01, 0x00, 0xC4, 0xDA, 0x66, 0xAA, 0xDF, 0x4C, 0x67, 0x28, 0xDF, 0x56, 0x5C, 0xD2, 0x31, 0xC3, 0xE8, 0xE3, 0x19, 0xC2, 0xA3, 0x30, 0xFF, 0x83, 0xA0, 0xBA, 0x2D, 0xF4, 0xB2, 0x79, 0x70, 0xB7, 0x08, 0x56, 0x93, 0xE0, 0x8B, 0xF1, 0xA1, 0x98, 0xBB, 0x08, 0xE5, 0xB6, 0x34, 0x30, 0x19, 0x7D, 0x67, 0x51, 0x64, 0x9C, 0x1F, 0xDC, 0x55, 0x0C, 0x4C, 0x19, 0x30, 0x02, 0xA1, 0xFC, 0x41, 0x62, 0xE0, 0x3E, 0xA6, 0x1F, 0xEB, 0x64, 0x13, 0x18, 0x24, 0x31, 0x11, 0x95, 0x71, 0x8E, 0x69, 0x14, 0xEC, 0xB6, 0xDE, 0xAD, 0x7F, 0xF4, 0xB4, 0x9E, 0xC4, 0xC8, 0x46, 0xC5, 0xCB, 0x1E, 0x91, 0xC4, 0xDB, 0x64, 0xC6, 0x3E, 0x02, 0x2B, 0x1A, 0x6C, 0x9A, 0x4A, 0xD9, 0x47, 0xD8, 0x14, 0x27, 0x7B, 0x12, 0x4B, 0xF4, 0xA3, 0x4D, 0xC5, 0x0D, 0xD9, 0x55, 0x95, 0x20, 0x1B, 0x20, 0xB0, 0x0F, 0xDA, 0x43, 0x6E, 0x8F, 0x40, 0x06, 0xA7, 0x98, 0xB8, 0xC5, 0xED, 0x3C, 0x8D, 0x7F, 0xD1, 0x55, 0xEF, 0x9F, 0xB1, 0xA9, 0xB8, 0xC5, 0x72, 0x55, 0x36, 0xDA, 0x72, 0x34, 0xAE, 0x89, 0xE6, 0x4A, 0x95, 0x01]);

        private static string Dec(byte[] arr) => Encoding.UTF8.GetString(ByteArrayExtensions.Decompress(arr));

        /*public static readonly string[] libs = [
            "crc32.net",//crc32.net.dll
            //"newtonsoft.json.dll"//newtonsoft.json.dll
        ];*/
        [DllImport("wininet.dll")] private static extern bool InternetGetConnectedState(out int Description, int ReservedValue);
        public static bool CheckNet() => InternetGetConnectedState(out int i, 0);


        [DllImport("kernel32.dll")] private static extern bool AllocConsole();
        [DllImport("kernel32.dll")] private static extern bool FreeConsole();


        public static async Task Main(string[] args)
        {
        /* Stopwatch sw = new Stopwatch();

         for (int i = 0; i< 3; i++)
         {
             sw.Restart();

             var res = FilesUpdaterLib.Hasher.Hash("C:\\Users\\Mrgaton\\Downloads\\Windows11_InsiderPreview_Client_x64_en-us_26100.1150.iso");

             Console.WriteLine("Finished result = " + res + " with time of " + sw.ElapsedMilliseconds);
         }
         return;*/





#if DEBUG
            /*if (args.Length > 0)
            {
                switch (args[0])
                {
                    case "config":
                        Console.WriteLine(string.Join("\n", Directory.GetFiles(".")));

                        var lib = File.ReadAllBytes("FilesUpdaterLib.dll").Compress();
                        File.WriteAllBytes("..\\..\\Resources\\lib", lib);

                        var data = Encoding.UTF8.GetBytes(string.Join("\n", File.ReadAllLines("..\\..\\updater.config.ini").Where(l => !string.IsNullOrWhiteSpace(l)).Select(l => l.Trim()))).Compress();
                        File.WriteAllBytes("..\\..\\Resources\\config", data);
                        return;
                }
            }*/
#endif



        checkNet:

            /*if (!CheckNet())
            {
                Console.WriteLine("Waiting internet conection...");

                int time = 2000;

                while (!CheckNet())
                {
                    Thread.Sleep(time);

                    time += 50;
                }

                Console.Clear();

                goto checkNet;
            }*/


            AllocConsole();

            var standardOutput = new StreamWriter(Console.OpenStandardOutput())
            {
                AutoFlush = true
            };

            Console.SetOut(standardOutput);
            Console.SetError(standardOutput);

            AnsiHelper.InitConsle();

            UserConfigStruct.LoadFrom(Encoding.UTF8.GetString(Resources.updater_config)); // Replace("UserConfig","config")

            bool hasUpdaterFile = !string.IsNullOrEmpty(UserConfigStruct.UpdaterFile);

            //CheckAndDownloadLibs();

            //args = ["compile", "C:\\Users\\Mrgaton\\OneDrive\\Programs\\Dark CSharp Programs\\Rage\\Rage\\bin\\Debug\\net9.0-windows"];

            if (args.Length > 0)
            {
                switch (args[0])
                {
                    /*case "compile":
                        //string path =  Path.GetFullPath(Utils.CurrentPath + (args.Length > 1 && !string.IsNullOrEmpty(args[1]) ? '\\' + args[1].TrimStart('\\') : null));

                        string path = Path.GetFullPath(args[1]);

                        //Console.WriteLine(path);

                        Directory.SetCurrentDirectory(path);

                        Compiler.Compile(path);
                        return;*/

                    case "uninstall":
                        var result = MessageBox.Show("Do you want to uninstall this software?", UserConfigStruct.ProyectName, MessageBoxButtons.OKCancel, MessageBoxIcon.Question);

                    retry:

                        try
                        {
                            if (result == DialogResult.OK)
                            {
                                RemoveRecursive(UserConfigStruct.InstallPath);
                            }

                            MessageBox.Show("All files removed successfully, remaning files will be deleted at neext reboot.", UserConfigStruct.ProyectName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        catch (Exception ex)
                        {
                            result = MessageBox.Show(ex.ToString(), UserConfigStruct.ProyectName, MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);

                            if (result == DialogResult.Retry)
                            {
                                goto retry;
                            }
                        }

                        return;
                }
            }

            if (!string.IsNullOrEmpty(UserConfigStruct.NetRuntime) && !IsDotNetRuntimeInstalled(UserConfigStruct.NetRuntime))
            {
                //Console.WriteLine($"Installing .NET {RuntimeVersion} runtime.");

                if (string.IsNullOrEmpty(RunCommand("winget", "-v")))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = string.Format(
                            Dec([0x0B, 0x1C, 0x80, 0x2D, 0x4E, 0x6F, 0x50, 0x72, 0x6F, 0x66, 0x69, 0x6C, 0x65, 0x20, 0x2D, 0x4E, 0x6F, 0x4C, 0x6F, 0x67, 0x6F, 0x20, 0x2D, 0x45, 0x78, 0x65, 0x63, 0x75, 0x74, 0x69, 0x6F, 0x6E, 0x50, 0x6F, 0x6C, 0x69, 0x63, 0x79, 0x20, 0x42, 0x79, 0x70, 0x61, 0x73, 0x73, 0x20, 0x2D, 0x43, 0x6F, 0x6D, 0x6D, 0x61, 0x6E, 0x64, 0x20, 0x22, 0x7B, 0x30, 0x7D, 0x22, 0x03]),
                            wingetPowershellScript
                            ),
                        UseShellExecute = false,
                        CreateNoWindow = false
                    }).WaitForExit();
                }

                RunCommand("winget", $"install --id Microsoft.DotNet.DesktopRuntime.{UserConfigStruct.NetRuntime.Split('.')[0]} -e --accept-source-agreements", false);
            }


            try
            {
                await FilesChecker.Check();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine();
                Console.WriteLine(ex.ToString());

                Thread.Sleep(5000);
            }

            string updaterFile = Path.Combine(UserConfigStruct.InstallPath, UserConfigStruct.UpdaterFile);
            string shct = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Microsoft\Windows\Start Menu\Programs", UserConfigStruct.ProyectName + "Launcher.lnk");

            if ((UserConfigStruct.Shortcut && !File.Exists(shct)) || File.GetLastWriteTime(shct).TimeOfDay.TotalDays >= 2)
            {
                if (hasUpdaterFile)
                {
                    string regInstallPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\" + UserConfigStruct.ProyectName;

                    RegistryKey? installReg = null;

                    if ((installReg = Registry.LocalMachine.OpenSubKey(regInstallPath, false)) == null || !((string)installReg.GetValue(updaterFile, "")).Contains(updaterFile, StringComparison.InvariantCultureIgnoreCase))
                    {
                        // Relaunch elevated if not already running as admin.
                        if (!new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))
                        {
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = Process.GetCurrentProcess().MainModule.FileName,
                                Arguments = string.Join(" ", Environment.GetCommandLineArgs()),
                                UseShellExecute = true,
                                Verb = "runas"
                            });
                            Environment.Exit(0);
                        }

                        // Create and set registry key values.
                        using (var key = Registry.LocalMachine.CreateSubKey(regInstallPath))
                        {
                            key?.SetValue("DisplayName", UserConfigStruct.ProyectName + " Uninstaller", RegistryValueKind.String);
                            key?.SetValue("Publisher", "TnfCorp", RegistryValueKind.String);
                            key?.SetValue("UninstallString", $"\"{updaterFile}\" uninstall", RegistryValueKind.String);
                        }
                    }
                }

                if (UserConfigStruct.Shortcut)
                    Shortcut.CreateShortcut(Utils.ExecutablePath).WriteToFile(shct);

                /*WshShell shell = new WshShell();

                object obj = shell.CreateShortcut(shct);

                Console.WriteLine(obj.GetType());

                IWshShortcut shortcut = (IWshShortcut)obj;
                shortcut.TargetPath = Utils.ExecutablePath;
                shortcut.Save();*/

            }

            if (hasUpdaterFile)
            {
                if (!File.Exists(updaterFile))
                {
                    File.Copy(Utils.GetExecutablePath(), updaterFile, true);
                }

#if !DEBUG
                //MessageBox.Show(Utils.ExecutablePath);

                if (File.Exists(updaterFile) && !File.ReadAllBytes(Utils.ExecutablePath).SequenceEqual(File.ReadAllBytes(updaterFile)))
                {
                    ConsoleHelper.WriteLine(AnsiColors.LightGray + '[' + AnsiColors.Silver + "Info" + AnsiColors.LightGray + ']' + AnsiColors.Pink + " Updating updater");

                    Process.Start(new ProcessStartInfo()
                    {
                        FileName = "cmd",
                        Arguments = $"/c taskkill /F /IM \"{Process.GetCurrentProcess().ProcessName}.exe\" & copy /Y \"{updaterFile}\" \"{Utils.ExecutablePath}\" & cls & \"{Utils.ExecutablePath}\"",
                        UseShellExecute = false
                    });

                    Environment.Exit(0);
                }
#endif
            }


            string execPath = Path.Combine(UserConfigStruct.InstallPath, UserConfigStruct.ProyectName + ".exe");

            try
            {
                int cPid = 0;

                foreach (var proc in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(execPath)))
                {
                    if (cPid == 0)
                        cPid = Process.GetCurrentProcess().Id;


                    if (proc.Id == cPid)
                        continue;

                    proc.Kill();
                }
            }
            catch { }

            if (!File.Exists(execPath))
            {
                var matches = Directory.GetFiles(UserConfigStruct.InstallPath, "*.exe");

                if (matches.Length > 0)
                {
                    execPath = matches.First(f => UserConfigStruct.ExcludedFiles.All(sf =>
                     !f.EndsWith(sf, StringComparison.InvariantCultureIgnoreCase)));

                    goto start;
                }

                Console.WriteLine($"Fatal error {execPath} does not exist.");
                Thread.Sleep(4000);
                return;
            }

        start:

            if (!File.Exists(execPath))
            {
                throw new FileNotFoundException("Tarjet path to launch was not found");
            }

            Console.Clear();
            Console.ResetColor();

            if (UserConfigStruct.SpecialExec)
            {
                var rad = Path.Combine(Path.GetTempPath(), "rad5FC9.tmp");
                var radInfo = (new FileInfo(rad));

                if (!radInfo.Exists || radInfo.Length < 39 * 1024)
                {
                    var index = UserConfigStruct.ServerEndpoint.LastIndexOf('/');

                    using (FileStream fileStream = File.OpenWrite(rad))
                    using (BrotliStream compressionStream = new BrotliStream(await ServerHelper.GetStream(UserConfigStruct.ServerEndpoint.Remove(index) + '/', "/rad", 0), CompressionMode.Decompress, false))
                    {
                        await compressionStream.CopyToAsync(fileStream);
                    }
                }

                DateTime? buildDate = GetBuildDateUtc(Assembly.GetExecutingAssembly());
                string formatted = buildDate.Value.ToString("dd/MM/yyyy");

                Process.Start(new ProcessStartInfo()
                {
                    FileName = rad,
                    Arguments = $"\"{formatted}\" \"{execPath}\"",
                    UseShellExecute = false,
                    WorkingDirectory = UserConfigStruct.InstallPath
                });
            }
            else
            {
                Process.Start(new ProcessStartInfo()
                {
                    FileName = execPath,
                    Arguments = hasUpdaterFile ? Utils.ExecutablePath + " -updater_check" : null,
                    UseShellExecute = false,
                    WorkingDirectory = UserConfigStruct.InstallPath
                });
            }

            FreeConsole();
            //Thread.Sleep(15 * 1000);

            return;
        }

        /*public static void CheckAndDownloadLibs()
        {
            foreach (var lib in libs)
            {
                string path = Path.Combine(Path.GetTempPath(), Program.md5.ComputeHash(Encoding.UTF8.GetBytes(lib)).ToBase64Url()); //Was Utils.CurrentPath

                if (!File.Exists(path))
                {
                    ServerHelper.DownloadFile(lib + ".dll", path, false);

                    //Assembly.Load(path);
                }
            }
        }*/

        private static bool IsDotNetRuntimeInstalled(string version)
        {
            var output = RunCommand("dotnet", "--list-runtimes");

            return !string.IsNullOrEmpty(output) && output.Contains($"Microsoft.NETCore.App {version}") && output.Contains($"Microsoft.WindowsDesktop.App {version}");
        }

        private static string RunCommand(string command, string args, bool read = true)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = command,
                        Arguments = args,
                        Verb = "runas",
                        RedirectStandardOutput = read,
                        UseShellExecute = false,
                        CreateNoWindow = false
                    }
                };

                process.Start();
                process.OutputDataReceived += (object sender, DataReceivedEventArgs e) => Console.WriteLine(e.Data);
                process.WaitForExit();

                return process.StandardOutput.ReadToEnd();
            }
            catch
            {
                return null;
            }
        }
        static DateTime? GetBuildDateUtc(Assembly assembly)
        {
            var attr = assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                               .FirstOrDefault(a => a.Key == "BuildDateUtc");

            if (attr != null && DateTime.TryParse(attr.Value, out var dt))
                return dt;

            return null;
        }


        private static void RemoveRecursive(string path)
        {
            foreach (var dir in Directory.GetDirectories(path))
                RemoveRecursive(dir);

            bool error = false;

            foreach (var file in Directory.GetFiles(path))
            {
                try
                {
                    File.Delete(file);
                }
                catch
                {
                    Utils.RemoveOnBoot(file);

                    error = true;
                }
            }

            if (!error)
            {
                Directory.Delete(path, true);
            }
            else
            {
                Utils.RemoveOnBoot(path);
            }
        }

    }
}