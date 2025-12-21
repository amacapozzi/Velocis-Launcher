using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace VelocisLauncher
{
    internal class DataManager
    {
        public class UserData
        {
            public string Username { get; set; }
            public string Token { get; set; }
            public Dictionary<int,(string Path,string SourceUrl)> DonwloadedGames { get; set; } // GameID -> (Path, SourceUrl)
        }

        private static string UserDataPath = Path.Combine(Program.DataPath, "userdata.json");

        public static UserData Data = GetData() ?? new UserData
        {
            Username = "",
            Token = "",
            DonwloadedGames = new Dictionary<int, (string Path, string SourceUrl)>()
        };

        public static UserData? GetData() => !File.Exists(UserDataPath) ? null : JsonSerializer.Deserialize<UserData>(File.ReadAllText(UserDataPath));
        public static void SaveData(UserData ud) => File.WriteAllText(UserDataPath, JsonSerializer.Serialize(ud));
    }
}
