using System;
using System.IO;

namespace SharpFileSystem.IO
{
	// ReSharper disable once UnusedMember.Global
	public class ConcatStream : Stream
	{
		readonly long[] _offsets;
		readonly Stream[] _streams;

		readonly long _length;

		int _streamIndex;

		public ConcatStream(params Stream[] streams)
		{
			_offsets = new long[streams.Length];
			_streams = streams;

			long offset = 0;
			_length = 0;
			for (var i = 0; i < _streams.Length; i++)
			{
				_offsets[i] = offset;
				offset += _streams[i].Length;
				_length += _streams[i].Length;
			}
		}

		// ReSharper disable once MemberCanBePrivate.Global
		public Stream CurrentStream => _streams[_streamIndex];
		// ReSharper disable once MemberCanBePrivate.Global
		public long CurrentStreamOffset => _offsets[_streamIndex];
		public override bool CanRead => true;
		public override bool CanSeek => true;
		public override bool CanWrite => false;
		public override long Length => _length;

		public override long Position
		{
			get => CurrentStreamOffset + CurrentStream.Position;
			set
			{
				if (value < CurrentStreamOffset)
					do
					{
						_streamIndex--;
					} while (value < CurrentStreamOffset);
				else if (value >= CurrentStreamOffset + CurrentStream.Length)
					do
					{
						_streamIndex++;
					} while (value >= CurrentStreamOffset + CurrentStream.Length && _streamIndex < _streams.Length - 1);

				CurrentStream.Position = value - CurrentStreamOffset;
			}
		}

		public override void Flush() { CurrentStream.Flush(); }

		public override int Read(byte[] buffer, int offset, int count)
		{
			int readBytes;
			do
			{
				readBytes = CurrentStream.Read(buffer, offset, count);
			} while (readBytes == 0 && _streamIndex < _streams.Length - 1);

			return readBytes;
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			switch (origin)
			{
				case SeekOrigin.Begin:
					return Position = offset;
				case SeekOrigin.Current:
					return Position = Position + offset;
				case SeekOrigin.End:
					return Position = Length - offset;
			}

			return Position;
		}

		public override void SetLength(long value) { throw new NotSupportedException(); }

		public override void Write(byte[] buffer, int offset, int count) { throw new NotSupportedException(); }

		public override void Close()
		{
			foreach (var s in _streams)
				s.Close();
			base.Close();
		}
	}
}
