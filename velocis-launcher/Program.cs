using Photino.NET;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace VelocisLauncher
{
    internal static class Program
    {
#if DEBUG
        private const bool Debug = true;
#else
        private const bool Debug = false;
#endif
        [DllImport("kernel32.dll")] private static extern bool AllocConsole();
        [DllImport("kernel32.dll")] private static extern bool FreeConsole();

        public static string DataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "VelocisLauncher");

        [STAThread]
        static void Main(string[] args)
        {
#if DEBUG
            AllocConsole();

            var standardOutput = new StreamWriter(Console.OpenStandardOutput())
            {
                AutoFlush = true
            };

            Console.SetOut(standardOutput);
            Console.SetError(standardOutput);

#endif

            string dllPath = Path.Combine(Path.GetTempPath(), "VelocisLauncherLibs");
            Directory.CreateDirectory(dllPath);

            LibraryLoader. LoadNativeDlls(dllPath, ["Photino.Native.dll", "WebView2Loader.dll"]);

            // Ensure the directory exists
            Directory.CreateDirectory(DataPath);

            Console.WriteLine("Launching Photino...");

            var window = new PhotinoWindow()
                .SetTitle("Velocis App")
                .SetSize(768, 512)
                .SetDevToolsEnabled(Debug)
                .SetFileSystemAccessEnabled(true)
                .SetSmoothScrollingEnabled(true)
                .SetUserAgent("VelocisLauncher/1.0")
                .SetResizable(true)
                .SetTemporaryFilesPath(DataPath)
                .Center()
                .Load(args.Length > 0 ? args[0] : "https://mold-willing-can-invitations.trycloudflare.com/");

            window.SetLogVerbosity(0);

            window.RegisterWebMessageReceivedHandler((_, e) =>
            {
                Console.WriteLine($"Message from web content: {e}");

                window.SendWebMessage(e);
            });

            window.WaitForClose();
        }
       
    }
}
