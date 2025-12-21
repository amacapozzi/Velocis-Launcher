using FilesUpdaterLib.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using static AnsiHelper;
using static FilesUpdaterLib.WofCompresion;

namespace FilesUpdaterLib
{
    public static class ServerHelper
    {
        private static HttpClient client = new HttpClient(new HttpClientHandler()
        {
            AutomaticDecompression = DecompressionMethods.None,
            AllowAutoRedirect = true,
            SslProtocols = System.Security.Authentication.SslProtocols.Tls13 | System.Security.Authentication.SslProtocols.Tls12,
            MaxAutomaticRedirections = 2,
            ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
            {
                using var certificate = new X509Certificate2(cert);

                var valid = sslPolicyErrors == System.Net.Security.SslPolicyErrors.None && certificate.Verify();

                if (!valid)
                {
                    Console.WriteLine();
                    Console.WriteLine(AnsiColors.BrightRed + "=== Wrong Certificate Information ===" + AnsiColors.LightRed);
                    Console.WriteLine($"Subject: {certificate.Subject}");
                    Console.WriteLine($"Issuer: {certificate.Issuer}");
                    Console.WriteLine($"Serial Number: {certificate.SerialNumber}");
                    Console.WriteLine($"Thumbprint: {certificate.Thumbprint}");
                    Console.WriteLine($"Not Before: {certificate.NotBefore}");
                    Console.WriteLine($"Not After: {certificate.NotAfter}");
                    Console.WriteLine($"Friendly Name: {certificate.FriendlyName}");
                    Console.WriteLine($"Version: {certificate.Version}");
                }

                return valid;
            },

            UseProxy = false,
            MaxConnectionsPerServer = int.MaxValue,
            MeterFactory = null,
            CheckCertificateRevocationList = false,
            ClientCertificateOptions = ClientCertificateOption.Manual,
            //CookieContainer = null,
            DefaultProxyCredentials = null,
            PreAuthenticate = false,
            UseCookies = false,
            UseDefaultCredentials = false,
            Proxy = null,
            Credentials = null
        })
        {
            DefaultRequestVersion = HttpVersion.Version30,
            DefaultVersionPolicy = HttpVersionPolicy.RequestVersionExact,
            DefaultRequestHeaders = { },
            Timeout = TimeSpan.FromMinutes(10)
        };

        private static string BaseUrl = UserConfigStruct.ServerEndpoint.TrimEnd('/') + '/';

    
        public static async Task<ConfigStructure> FetchConfig()
        {
            using (HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, BaseUrl + Utils.ConfigFileName))
            {
                using (HttpResponseMessage res = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead))
                {
#if DEBUG
                    byte[] data = await res.Content.ReadAsByteArrayAsync();

                    Console.WriteLine('\n' + AnsiColors.Pink + System.Text.Encoding.UTF8.GetString(data) + '\n');
                    Console.WriteLine('\n' + AnsiColors.Pink + System.Text.Encoding.UTF8.GetString(ByteArrayExtensions.Decompress(data)) + '\n');

                    Console.WriteLine(AnsiColors.Pink + "Compressed: " + AnsiColors.Silver + data.Length);
                    Console.WriteLine(AnsiColors.Pink + "ReCompressed: " + AnsiColors.Silver + ByteArrayExtensions.Compress(data).Length);
                    Console.WriteLine(AnsiColors.Pink + "DeCompressed: " + AnsiColors.Silver + ByteArrayExtensions.Decompress(data).Length);
#endif

                    if (res.StatusCode != HttpStatusCode.OK)
                        throw new InvalidDataException("Server response is vomit");

                    using (var s = await res.Content.ReadAsStreamAsync())
                    {
                        return Utils.Deserialize(s);
                    }
                }
            }
        }

        public static async Task<Stream> GetStream(string baseUrl, string relativePath, int chunk, CancellationToken ct = default)
        {
            using (HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, baseUrl + relativePath.Replace('\\', '/').Trim('/').ToLower() + '.' + chunk.ToString() + Utils.PackedExtension))
            {
                HttpResponseMessage res = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);

                return await res.Content.ReadAsStreamAsync(ct);
            }
        }
        private static async Task SaveStream(FileStream fileStream)
        {
            await fileStream.FlushAsync();

            WOFCompressFile(fileStream.SafeFileHandle.DangerousGetHandle(), GetAlg(fileStream.Length));
        }

        private const int BufferSize = 81920 * 4;
        private const int FileBufferSize = 8 * 1024 * 1024;

        public static async Task DownloadFileAsync(string relativePath, string outPath, FileStruct fs, int chunkSize, IProgress<long> progress = null, CancellationToken ct = default)
        {
            string directory = Path.GetDirectoryName(outPath);
            string progressPath = outPath + Utils.ProgressExtension;

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            try
            {
                int totalChunks = (int)(fs.Size / chunkSize) + (fs.Size % chunkSize > 0 ? 1 : 0);

                if (totalChunks <= 1)
                {
                    using (FileStream fileStream = new FileStream(outPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, FileBufferSize, FileOptions.SequentialScan))
                    using (BrotliStream compressionStream = new BrotliStream(await GetStream(BaseUrl, relativePath, 0, ct), CompressionMode.Decompress, false))
                    {
                        fileStream.SetLength(fs.Size);

                        await compressionStream.CopyToAsync(fileStream, BufferSize, ct);
                        progress?.Report(fs.Size);
                        await SaveStream(fileStream);
                    }
                }
                else
                {
                    long totalBytesWritten = 0;

                    using (SemaphoreSlim saveLock = new SemaphoreSlim(1, 1))
                    using (FileStream fileStream = new FileStream(outPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, FileBufferSize, FileOptions.Asynchronous | FileOptions.RandomAccess))
                    {
                        fileStream.SetLength(fs.Size);

                        HashSet<int> chunkIndices = new HashSet<int>(File.Exists(progressPath) ? BitMapStorage.LoadIndices(progressPath) : Enumerable.Range(0, totalChunks));

                        int chunksAlreadyDone = totalChunks - chunkIndices.Count;
                        if (chunksAlreadyDone > 0)
                        {
                            long estimatedDone = (long)chunksAlreadyDone * chunkSize;
                            totalBytesWritten = Math.Min(estimatedDone, fs.Size);
                            progress?.Report(totalBytesWritten);
                        }


                        ConsoleHelper.WriteLine("Downloading Chunks: " + string.Join(", ", chunkIndices));

                        var parallelOptions = new ParallelOptions
                        {
                            MaxDegreeOfParallelism = Math.Max(8, Environment.ProcessorCount),
                        };

                        await Parallel.ForEachAsync(chunkIndices, parallelOptions, async (i, ct) =>
                        {
                            using (Stream networkStream = await GetStream(BaseUrl, relativePath, i, ct))
                            using (BrotliStream compressionStream = new BrotliStream(networkStream, CompressionMode.Decompress))
                            using (ChunkedViewStream chunkStream = new ChunkedViewStream(fileStream, i, Utils.ChunkSize, true))
                            {
                                await compressionStream.CopyToAsync(chunkStream, BufferSize, ct);
                                await compressionStream.FlushAsync(ct);
                            }

                            long currentActualChunkSize = (i == totalChunks - 1) ? (fs.Size - ((long)i * chunkSize)) : chunkSize;
                            long currentTotal = Interlocked.Add(ref totalBytesWritten, currentActualChunkSize);

                            progress?.Report(currentTotal);


                            await saveLock.WaitAsync(ct);

                            try
                            {
                                chunkIndices.Remove(i);

                                BitMapStorage.SaveProgress(progressPath, totalChunks, chunkIndices);
                            }
                            finally
                            {
                                saveLock.Release();
                            }
                        });

                        await SaveStream(fileStream);
                    }
                }
            }
            catch
            {
                try
                {
                    if (File.Exists(outPath))
                        File.Delete(outPath);
                }
                catch { }

                Console.WriteLine(AnsiColors.Red + "\nSomething went wrong while decoding data on " + relativePath);

                throw;
            }
            /*finally
            {
                if (!string.IsNullOrEmpty(tempPath) && File.Exists(tempPath))
                {
                    try
                    {
                        File.Delete(tempPath);
                    }
                    catch { }
                }
            }*/
        }
        private static CompressionAlgorithm GetAlg(long size)
        {
            if (UserConfigStruct.HighCompression)
            {
                if (size < 20 * 1024 * 1024) //LZX for files smaller than 15 mb
                {
                    return CompressionAlgorithm.LZX;
                }
                else if (size < 50 * 1024 * 1024) //XPRESS16K for files smaller than 75 mb
                {
                    return CompressionAlgorithm.XPRESS16K;
                }
                else if (size < 100 * 1024 * 1024) //XPRESS8K for files smaller than 225 mb
                {
                    return CompressionAlgorithm.XPRESS8K;
                }
                else if (size < 500 * 1024 * 1024)
                {
                    return CompressionAlgorithm.XPRESS4K; //XPRESS4K for the rest of the files
                }
                else
                {
                    return CompressionAlgorithm.NONE;
                }
            }
            else
            {
                if (size < 10 * 1024 * 1024) //LZX for files smaller than 15 mb
                {
                    return CompressionAlgorithm.LZX;
                }
                else if (size < 100 * 1024 * 1024) //XPRESS16K for files smaller than 75 mb
                {
                    return CompressionAlgorithm.XPRESS16K;
                }
                else if (size < 200 * 1024 * 1024) //XPRESS8K for files smaller than 225 mb
                {
                    return CompressionAlgorithm.XPRESS8K;
                }
                else if (size < 1000 * 1024 * 1024)
                {
                    return CompressionAlgorithm.XPRESS4K; //XPRESS4K for the rest of the files
                }
                else
                {
                    return CompressionAlgorithm.NONE;
                }
            }
        }

        /*public sealed class ConcatenatedStream : Stream
        {
            private readonly string _streamsPath;
            private readonly int _streamCount;

            private Stream _currentStream;
            private int _currentReaded = 0;
            private int _currentIndex = 0;

            // If GetStream/ BaseUrl are members of the outer class, keep calling them as before.
            // This class assumes a Task<Stream> GetStream(string baseUrl, string path, int index) exists.
            private Task<Stream> _prefetchTask;

            public ConcatenatedStream(string relativePath, int count)
            {
                if (count < 1)
                    throw new ArgumentOutOfRangeException(nameof(count), "count must be >= 1");

                _currentReaded = 0;
                _streamCount = count;
                _streamsPath = relativePath;

                // initialize current stream synchronously (blocking) — acceptable for constructor,
                // but consider exposing an async factory if you want async construction.
                _currentStream = GetNextStreamAsync().GetAwaiter().GetResult();
            }

            private async Task<Stream> GetNextStreamAsync()
            {
                // Reset per-stream read counter when we actually switch to a new stream.
                _currentReaded = 0;

                if (_prefetchTask != null)
                {
                    try
                    {
                        var next = await _prefetchTask.ConfigureAwait(false);
                        _prefetchTask = null;
                        return next;
                    }
                    catch
                    {
                        // if prefetch failed, clear it and rethrow so caller can decide.
                        _prefetchTask = null;
                        throw;
                    }
                }

                if (_currentIndex < _streamCount)
                {
                    // consume the next index
                    return await GetStream(BaseUrl, _streamsPath, _currentIndex++).ConfigureAwait(false);
                }

                return null; // no more streams
            }

            private void PrefetchNextStream()
            {
                // Start prefetch only if not already started and there are more streams remaining.
                if (_prefetchTask == null && _currentIndex < _streamCount)
                {
                    // Capture the index we intend to load so we don't race with the consumer.
                    int indexToFetch = _currentIndex++;
                    _prefetchTask = Task.Run(async () =>
                    {
                        var next = await GetStream(BaseUrl, _streamsPath, indexToFetch).ConfigureAwait(false);

                        // Try to "kick" the request/underlying buffer. Some stream implementations
                        // may start network work on creation, others on first read. Reading zero bytes
                        // is a no-op for many streams; calling Read/ReadAsync with a 0-length
                        // buffer is allowed, but some custom streams may not like it, so it's optional.
                        try
                        {
                            // best-effort: attempt a minimal synchronous read if it's supported.
                            next.Read(Array.Empty<byte>(), 0, 0);
                        }
                        catch
                        {
                            // ignore any trouble here — prefetch is best-effort
                        }

                        return next;
                    });
                }
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (buffer == null) throw new ArgumentNullException(nameof(buffer));
                if (offset < 0 || count < 0 || offset + count > buffer.Length) throw new ArgumentOutOfRangeException();

                while (true)
                {
                    if (_currentStream == null) return 0;

                    int bytesRead;
                    try
                    {
                        bytesRead = _currentStream.Read(buffer, offset, count);
                    }
                    catch
                    {
                        // on read errors, dispose and rethrow (or you could attempt to skip to next stream)
                        _currentStream?.Dispose();
                        _currentStream = GetNextStreamAsync().GetAwaiter().GetResult();
                        throw;
                    }

                    if (bytesRead > 0)
                    {
                        _currentReaded += bytesRead;

                        // if we've consumed more than the threshold, begin prefetch of next stream
                        if (_currentReaded > Utils.ChunkSize / 2)
                        {
                            PrefetchNextStream();
                        }

                        return bytesRead;
                    }
                    else
                    {
                        // current stream reached EOF (0 bytes). Move to next stream and continue loop.
                        _currentStream?.Dispose();
                        _currentStream = GetNextStreamAsync().GetAwaiter().GetResult();

                        // loop to attempt to read from the new stream (or return 0 if none)
                        continue;
                    }
                }
            }

            public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            {
                if (_currentStream == null) return 0;

                while (true)
                {
                    if (_currentStream == null) return 0;

                    int bytesRead;
                    try
                    {
                        bytesRead = await _currentStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                    }
                    catch
                    {
                        // on read errors, dispose and move to next stream before rethrowing
                        _currentStream?.Dispose();
                        _currentStream = await GetNextStreamAsync().ConfigureAwait(false);
                        throw;
                    }

                    if (bytesRead > 0)
                    {
                        _currentReaded += bytesRead;

                        if (_currentReaded > Utils.ChunkSize / 2)
                        {
                            PrefetchNextStream();
                        }

                        return bytesRead;
                    }
                    else
                    {
                        // EOF - switch to next stream and retry
                        _currentStream?.Dispose();
                        _currentStream = await GetNextStreamAsync().ConfigureAwait(false);

                        // If no next stream, return 0
                        if (_currentStream == null) return 0;

                        // otherwise loop and try again
                        continue;
                    }
                }
            }

            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                return ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
            }

            #region Stream required members

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => throw new NotSupportedException();
            public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
            public override void Flush() { }
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    try { _currentStream?.Dispose(); } catch { }
                    // if you also want to cancel an in-flight prefetch, you'd need a cancellation mechanism.
                    _prefetchTask = null;
                }

                base.Dispose(disposing);
            }

            #endregion
        }*/

    }
}
