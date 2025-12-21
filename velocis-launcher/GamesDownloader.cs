using System;
using System.Collections.Generic;
using System.Text;

namespace VelocisLauncher
{
    internal class GamesDownloader
    {
        public static void DownloadGame(int gameId, string downloadUrl, string destinationPath)
        {
            Console.WriteLine($"Starting download for game ID {gameId} from {downloadUrl} to {destinationPath}...");


        }
    }
}
