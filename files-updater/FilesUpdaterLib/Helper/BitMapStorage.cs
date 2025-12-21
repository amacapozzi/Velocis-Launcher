using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace FilesUpdaterLib.Helper
{
    public static class BitMapStorage
    {
        public static void SaveProgress(string filePath, int totalItems, IEnumerable<int> completedIndices)
        {
            int byteCount = (totalItems + 7) >>> 3;
            byte[] buffer = new byte[byteCount];

            foreach (int index in completedIndices)
            {
                if (index >= 0 && index < totalItems)
                {
                    buffer[index >>> 3] |= (byte)(1 << (index & 7));
                }
            }

            File.WriteAllBytes(filePath, buffer);
        }
        public static IEnumerable<int> LoadIndices(string filePath)
        {
            if (!File.Exists(filePath)) yield break;

            byte[] fileBytes = File.ReadAllBytes(filePath);
            int length = fileBytes.Length;

            int ulongCount = length / 8;

            for (int i = 0; i < ulongCount; i++)
            {
                ulong currentWord = Unsafe.ReadUnaligned<ulong>(ref fileBytes[i * 8]);

                if (currentWord == 0) continue;

                int baseIndex = i * 64;

                while (currentWord != 0)
                {
                    int trailingZeros = BitOperations.TrailingZeroCount(currentWord);
                    yield return baseIndex + trailingZeros;

                    currentWord &= (currentWord - 1);
                }
            }

            int processedBytes = ulongCount * 8;

            for (int i = processedBytes; i < length; i++)
            {
                byte currentByte = fileBytes[i];
                if (currentByte == 0) continue;

                int baseIndex = i * 8;
                while (currentByte != 0)
                {
                    int trailingZeros = BitOperations.TrailingZeroCount(currentByte);
                    yield return baseIndex + trailingZeros;
                    currentByte &= (byte)(currentByte - 1);
                }
            }
        }
    }
}
