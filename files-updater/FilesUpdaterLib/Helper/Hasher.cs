using System.IO;

namespace FilesUpdaterLib
{
    public static class Hasher
    {
        public const int BufferSize = 1 * 1024 * 1024;

        public static uint Hash(string filePath)
        {
            using (FileStream fs = File.OpenRead(filePath))
            {
                return Hash(fs);
            }
        }
        public static uint Hash(byte[] buffer) => Crc32CAlgorithm.Append(0, buffer, 0, buffer.Length);
        public static uint Hash(FileStream fs)
        {
            uint hash = 0;
            int readed = 0;

            byte[] buffer = new byte[BufferSize];

            while ((readed = fs.Read(buffer, 0, BufferSize)) > 0)
            {
                hash = Crc32CAlgorithm.Append(hash, buffer, 0, readed);
            }

            return hash;
        }
    }
}