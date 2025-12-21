using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace VelocisLauncher
{
    internal class LibraryLoader
    {
        public static void LoadNativeDlls(string targetFolder, string[] dllNames)
        {
            var assembly = Assembly.GetExecutingAssembly();

            foreach (string dllName in dllNames)
            {
                string destinationPath = Path.Combine(targetFolder, dllName);

                if (!File.Exists(destinationPath))
                {
                    string resourceName = $"{typeof(Program).Namespace}.{dllName}";

                    using (var stream = assembly.GetManifestResourceStream(resourceName))
                    {
                        if (stream == null)
                        {
                            var resources = string.Join(", ", assembly.GetManifestResourceNames());

                            throw new Exception($"Could not find embedded resource '{resourceName}'. Found: {resources}");
                        }

                        using (var fileStream = File.Create(destinationPath))
                        {
                            stream.CopyTo(fileStream);
                        }
                    }
                }

                try
                {
                    NativeLibrary.Load(destinationPath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load {dllName}: {ex.Message}");
                }
            }
        }
    }
}
