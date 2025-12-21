using System;
using System.Collections.Generic;
using System.Linq;

namespace FilesUpdaterLib
{
    public static class UserConfigStruct
    {
        public static bool Loaded = false;

        public static string ServerEndpoint;

        public static string NetRuntime = string.Empty;

        public static string ProyectName;

        public static string UpdaterFile;

        public static bool RemoveEmptyDirs  = true;

        public static string InstallPath;

        public static HashSet<string> ExcludedExtensions = [];
        public static HashSet<string> ExcludedFiles = [];
        public static HashSet<string> ExcludedDirectories = [];

        public static bool HighCompression = true;
        public static bool Shortcut = true;
        public static bool SpecialExec = false;


        //public IDictionary<string, string> Env { get; set; } = new
        //<string, string>() { { "example", "1234" } };

        //public string[] ExcludedExtensions { get; set; } = [];

        public static void LoadFrom(string data)
        {
            try
            {
                var reader = new IniReader(data);

                ProyectName = reader.GetValue("ProyectName");
                NetRuntime = reader.GetValue("NetRuntime");
                ServerEndpoint = reader.GetValue("ServerEndpoint");

                UpdaterFile = reader.GetValue("UpdaterFile");

                bool.TryParse(reader.GetValue("RemoveEmptyDirs"), out RemoveEmptyDirs);

                InstallPath = Environment.ExpandEnvironmentVariables(reader.GetValue("InstallPath"));

                ExcludedExtensions = new(ParseArray(reader.GetValue("ExcludedExtensions").ToLower()).Union(Utils.StaticExcludedExtensions), StringComparer.OrdinalIgnoreCase);
                
                ExcludedFiles = new((ParseArray(reader.GetValue("ExcludedFiles").ToLower()).Union(Utils.StaticExcludedFiles)), StringComparer.OrdinalIgnoreCase);
                ExcludedDirectories = new((ParseArray(reader.GetValue("ExcludedDirectories").ToLower()).Union(Utils.StaticExcludedDirectories)), StringComparer.OrdinalIgnoreCase);

#if DEBUG
                if (ExcludedFiles.Any(f => f.StartsWith('/') || f.StartsWith('\\') || ExcludedDirectories.Any(d => d.StartsWith('/') || d.StartsWith('\\'))))
                    throw new Exception("one of file exclusion or dir exclusion entry starts with / or \\");
#endif

                bool.TryParse(reader.GetValue("HighCompression"), out HighCompression);
                bool.TryParse(reader.GetValue("Shortcut"), out Shortcut);
                bool.TryParse(reader.GetValue("SpecialExec"), out SpecialExec);
        
                //config.ExcludedExtensions=ParseArray(ReadIni("ExcludedExtensions", file));

                Loaded = true;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.ToString());
            }
        }

        //private static string[] CleanPaths(IEnumerable<string> arr) => arr.Select(f => f.Replace('/', '\\').Trim('\\').ToLower()).ToArray(); // .Concat((Compiler.UserConfig ?? FilesChecker.UserConfig).ExcludedFiles)

        private static string[] ParseArray(string b) => 
            b.Split(';').Select(e => e.Replace('/', '\\').Trim('\"', ' ')).Where(e => e != "").ToArray();
    }
}