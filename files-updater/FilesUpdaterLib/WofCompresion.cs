using System;
using System.IO;
using System.Runtime.InteropServices;

namespace FilesUpdaterLib
{
    internal static class WofCompresion
    {
        [DllImport("WofUtil.dll")]
        private static extern int WofSetFileDataLocation(
            IntPtr FileHandle,
            uint Provider,
            IntPtr External,
            uint Length
        );

        public enum CompressionAlgorithm : uint
        {
            NONE = uint.MaxValue,
            XPRESS4K = 0,
            XPRESS8K = 1,
            XPRESS16K = 2,
            LZX = 3,
        }

        public struct WofFileCompressionInfoV1
        {
            public CompressionAlgorithm Algorithm;
            public uint Flags;
        }

        private const uint WOF_PROVIDER_WIM = 2;

        public static int WOFCompressFile(string path, CompressionAlgorithm alg = CompressionAlgorithm.LZX)
        {
            if (alg == CompressionAlgorithm.NONE)
                return 0;

            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                IntPtr hFile = fs.SafeFileHandle.DangerousGetHandle();

                return WOFCompressFile(hFile, alg);
            }
        }

        public static int WOFCompressFile(IntPtr hFile, CompressionAlgorithm alg)
        {
            if (alg == CompressionAlgorithm.NONE)
                return 0;

            WofFileCompressionInfoV1 externalFileInfo = new WofFileCompressionInfoV1
            {
                Algorithm = alg,
                Flags = 0
            };

            int size = Marshal.SizeOf(externalFileInfo);
            IntPtr externalFileInfoPtr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(externalFileInfo, externalFileInfoPtr, false);

            try
            {
                int res = WofSetFileDataLocation(hFile, WOF_PROVIDER_WIM, externalFileInfoPtr, (uint)size);

                if (res != 0 && res != -2147024552)
                {
                    int error = Marshal.GetLastWin32Error();

                    Console.Write("Error: " + res);

                    throw new System.ComponentModel.Win32Exception(error);
                }

                return res;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.ToString());

                return -1;
            }
            finally
            {
                if (externalFileInfoPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(externalFileInfoPtr);
                }
            }
        }
    }
}