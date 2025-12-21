using Photino.NET;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

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

            Directory.CreateDirectory(DataPath);

            Console.WriteLine("Launching Photino...");

            var window = new PhotinoWindow()
                .SetTitle("Velocis App")
                .SetChromeless(true)
                .SetUseOsDefaultSize(false)
                .SetUseOsDefaultLocation(false)
                .SetSize(768 * 2, 512 * 2)
                .SetResizable(true)
                .SetDevToolsEnabled(Debug)
                .SetContextMenuEnabled(false)
                .SetTemporaryFilesPath(DataPath)

                .SetTemporaryFilesPath(DataPath)
                .Center()
                
                .Load(args.Length > 0 ? args[0] : "https://google.com/");

            
            window.SetChromeless(true);

            window.SetLogVerbosity(0);

            window.RegisterWebMessageReceivedHandler((_, e) =>
            {
                Console.WriteLine($"Message from web content: {e}");

                if (e[0] != '{' || e[e.Length - 1] != '}')
                    return;

                var obj = JsonNode.Parse(e);

                switch ((string)obj["type"])
                {
                    case "close":
                        window.Close();
                        break;

                    case "setTitle":
                            var title = (string)obj["title"];
                            window.SetTitle(title);
                        break;

                        case "setSize":
                            var width = (int)obj["width"];
                            var height = (int)obj["height"];
                            window.SetSize(width, height);
                        break;

                        case "setPosition":
                            var x = (int)obj["x"];
                            var y = (int)obj["y"];
                            window.MoveTo(x, y);
                        break;

                        case "minimize":
                            window.SetMinimized(true);
                        break;

                        case "maximize":
                            window.SetMaximized(true);
                        break;
                      
                        case "navigate":
                            var url = (string)obj["url"];
                            window.Load(url);
                        break; 

                        case "log":
                            var message = (string)obj["message"];
                            Console.WriteLine($"Log from web content: {message}");
                        break;

                        case "alert":
                            window.SendNotification((string)obj["title"], (string)obj["message"]);
                        break;
                }

                window.SendWebMessage(e);
            });

            window.WaitForClose();
        }
       
    }
}
