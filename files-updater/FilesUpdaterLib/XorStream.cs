using System.IO;

namespace FilesUpdaterLib
{/*
    internal class XorStream : Stream
    {
        private readonly Stream _baseStream;
        private readonly byte[][] _keys;
        private int[] _keyIndexs;

        public XorStream(Stream baseStream, byte[][] keys)
        {
            _baseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
            if (keys == null || keys.Any(k => k.Length == 0))
                throw new ArgumentException("Key cannot be null or empty", nameof(keys));

            _keys = keys;
            _keyIndexs = new int[keys.Length];
        }

        public override bool CanRead => _baseStream.CanRead;
        public override bool CanSeek => _baseStream.CanSeek;
        public override bool CanWrite => _baseStream.CanWrite;
        public override long Length => _baseStream.Length;

        public override long Position
        {
            get => _baseStream.Position;
            set => _baseStream.Position = value;
        }

        public override void Flush() => _baseStream.Flush();

        public override int Read(byte[] buffer, int offset, int count)
        {
            int bytesRead = _baseStream.Read(buffer, offset, count);
            XorBuffer(buffer, offset, bytesRead);
            return bytesRead;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            int bytesRead = await _baseStream.ReadAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
            XorBuffer(buffer, offset, bytesRead);
            return bytesRead;
        }

    
        public override void Write(byte[] buffer, int offset, int count)
        {
            XorBuffer(buffer, offset, count);
            _baseStream.Write(buffer, 0, count);
        }    private void XorBuffer(byte[] buffer, int offset, int count)
        {
            for (int i = 0; i < count; i++)
            {
                for (int k = 0; k < _keys.Length; k++)
                {
                    buffer[offset + i] ^= _keys[k][_keyIndexs[k]];
                    _keyIndexs[k] = (_keyIndexs[k] + 1) % _keys[k].Length;
                }
            }
        }


        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            XorBuffer(buffer, offset, count);
            await _baseStream.WriteAsync(buffer, 0, count, cancellationToken).ConfigureAwait(false);
        }

        public override long Seek(long offset, SeekOrigin origin) =>
            _baseStream.Seek(offset, origin);

        public override void SetLength(long value) =>
            _baseStream.SetLength(value);
        public override async Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
        {
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));
            if (bufferSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(bufferSize));

            byte[] buffer = new byte[bufferSize];
            int bytesRead;
            while ((bytesRead = await ReadAsync(buffer, 0, buffer.Length, cancellationToken)
                                    .ConfigureAwait(false)) != 0)
            {
                await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken)
                                 .ConfigureAwait(false);
            }
        }
    }*/
}