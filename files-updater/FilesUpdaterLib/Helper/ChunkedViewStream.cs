using System;
using System.IO;

namespace FilesUpdaterLib.Helper
{
    /// <summary>
    /// A wrapper stream that restricts access to a specific segment (chunk) of a root stream.
    /// Thread-safe for sharing a single Root Stream across multiple ChunkViews.
    /// </summary>
    public class ChunkedViewStream : Stream
    {
        private readonly Stream _rootStream;
        private readonly long _absoluteStart;
        private readonly long _absoluteEnd;
        private readonly long _chunkSize;
        private readonly bool _leaveOpen;
        private long _localPos; // 0-based position within the chunk

        /// <summary>
        /// Creates a virtual stream restricted to a specific chunk index.
        /// </summary>
        /// <param name="rootStream">The underlying stream (must be Seekable).</param>
        /// <param name="chunkIndex">The zero-based index of the chunk.</param>
        /// <param name="chunkSizeBytes">The fixed size of each chunk in bytes.</param>
        /// <param name="leaveOpen">If true, disposing this stream does not close the root stream.</param>
        public ChunkedViewStream(Stream rootStream, long chunkIndex, long chunkSizeBytes, bool leaveOpen = true)
        {
            if (rootStream == null) throw new ArgumentNullException(nameof(rootStream));
            if (!rootStream.CanSeek) throw new ArgumentException("Root stream must support Seeking.", nameof(rootStream));
            if (chunkIndex < 0) throw new ArgumentOutOfRangeException(nameof(chunkIndex), "Chunk index cannot be negative.");
            if (chunkSizeBytes <= 0) throw new ArgumentOutOfRangeException(nameof(chunkSizeBytes), "Chunk size must be positive.");

            _rootStream = rootStream;
            _chunkSize = chunkSizeBytes;
            _absoluteStart = chunkIndex * chunkSizeBytes;
            _absoluteEnd = _absoluteStart + chunkSizeBytes;
            _localPos = 0;
            _leaveOpen = leaveOpen;
        }

        // --- Essential Stream Overrides ---

        public override bool CanRead => _rootStream.CanRead;
        public override bool CanSeek => _rootStream.CanSeek;
        public override bool CanWrite => _rootStream.CanWrite;
        public override long Length => _chunkSize;

        public override long Position
        {
            get => _localPos;
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), "Position cannot be negative.");
                // Note: Streams allow seeking past the end (subsequent writes extend the file), 
                // but here we strictly cap it to the chunk size for safety.
                if (value > _chunkSize) value = _chunkSize;
                _localPos = value;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_localPos >= _chunkSize) return 0; // End of chunk

            // Don't read past the end of the chunk
            long remaining = _chunkSize - _localPos;
            int bytesToRead = (int)Math.Min(count, remaining);

            // LOCKING is mandatory because _rootStream.Position is shared state.
            lock (_rootStream)
            {
                _rootStream.Seek(_absoluteStart + _localPos, SeekOrigin.Begin);
                int bytesRead = _rootStream.Read(buffer, offset, bytesToRead);
                _localPos += bytesRead;
                return bytesRead;
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (_localPos >= _chunkSize) 
                throw new IOException("Cannot write past the end of the chunk.");

            // Don't write past the end of the chunk
            long remaining = _chunkSize - _localPos;
            int bytesToWrite = (int)Math.Min(count, remaining);

            // Optional: Fail hard if data is too big, rather than truncating silently
            if (bytesToWrite < count)
            {
                // Uncomment next line if you prefer an error over truncation
                // throw new IOException($"Attempt to write {count} bytes exceeds chunk remaining size of {remaining}.");
            }

            lock (_rootStream)
            {
                _rootStream.Seek(_absoluteStart + _localPos, SeekOrigin.Begin);
                _rootStream.Write(buffer, offset, bytesToWrite);
                _localPos += bytesToWrite;
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            long newPos;
            switch (origin)
            {
                case SeekOrigin.Begin:
                    newPos = offset;
                    break;
                case SeekOrigin.Current:
                    newPos = _localPos + offset;
                    break;
                case SeekOrigin.End:
                    newPos = _chunkSize + offset;
                    break;
                default:
                    throw new ArgumentException("Invalid SeekOrigin");
            }

            Position = newPos; // Uses the logic in the Position setter
            return _localPos;
        }

        public override void Flush()
        {
            lock (_rootStream)
            {
                _rootStream.Flush();
            }
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException("Resizing a specific chunk view is not supported. Size is fixed.");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_leaveOpen)
            {
                _rootStream.Close();
            }

            base.Dispose(disposing);
        }
    }
}
