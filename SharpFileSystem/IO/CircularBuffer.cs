using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace SharpFileSystem.IO
{
	// CircularBuffer from http://circularbuffer.codeplex.com/.
	public class CircularBuffer<T> : ICollection<T>, ICollection
	{
		T[] _buffer;
		int _capacity;
		int _head;

		[NonSerialized] object _syncRoot;
		int _tail;

		public CircularBuffer(int capacity, bool allowOverflow = false)
		{
			if (capacity < 0)
				throw new ArgumentException("capacity must be greater than or equal to zero.",
					"capacity");

			_capacity = capacity;
			Size = 0;
			_head = 0;
			_tail = 0;
			_buffer = new T[capacity];
			AllowOverflow = allowOverflow;
		}

		bool AllowOverflow { get; }

		public int Capacity
		{
			get => _capacity;
			// ReSharper disable once UnusedMember.Global
			set
			{
				if (value == _capacity)
					return;

				if (value < Size)
					throw new ArgumentOutOfRangeException("value",
						"value must be greater than or equal to the buffer size.");

				var dst = new T[value];
				if (Size > 0)
					CopyTo(dst);
				_buffer = dst;

				_capacity = value;
			}
		}

		public int Size { get; private set; }

		public bool Contains(T item)
		{
			var bufferIndex = _head;
			var comparer = EqualityComparer<T>.Default;
			for (var i = 0; i < Size; i++, bufferIndex++)
			{
				if (bufferIndex == _capacity)
					bufferIndex = 0;

				if (item == null && _buffer[bufferIndex] == null)
					return true;
				if (_buffer[bufferIndex] != null &&
				    comparer.Equals(_buffer[bufferIndex], item))
					return true;
			}

			return false;
		}

		public void Clear()
		{
			Size = 0;
			_head = 0;
			_tail = 0;
		}

		public void CopyTo(T[] array, int arrayIndex = 0) { CopyTo(array, arrayIndex, Size); }

		#region IEnumerable<T> Members

		IEnumerator<T> IEnumerable<T>.GetEnumerator() { return GetEnumerator(); }

		#endregion

		#region IEnumerable Members

		IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

		#endregion

		// ReSharper disable once UnusedMember.Global
		public int Put(T[] src) { return Put(src, 0, src.Length); }

		public int Put(T[] src, int offset, int count)
		{
			var realCount = AllowOverflow ? count : Math.Min(count, _capacity - Size);
			var srcIndex = offset;
			for (var i = 0; i < realCount; i++, _tail++, srcIndex++)
			{
				if (_tail == _capacity)
					_tail = 0;
				_buffer[_tail] = src[srcIndex];
			}

			Size = Math.Min(Size + realCount, _capacity);
			return realCount;
		}

		void Put(T item)
		{
			if (!AllowOverflow && Size == _capacity)
				throw new InternalBufferOverflowException("Buffer is full.");

			_buffer[_tail] = item;
			if (_tail++ == _capacity)
				_tail = 0;
			Size++;
		}

		// ReSharper disable once UnusedMember.Global
		public void Skip(int count)
		{
			_head += count;
			if (_head >= _capacity)
				_head -= _capacity;
		}

		// ReSharper disable once UnusedMember.Global
		public T[] Get(int count)
		{
			var dst = new T[count];
			Get(dst);
			return dst;
		}

		// ReSharper disable once UnusedMethodReturnValue.Local
		int Get(T[] dst) { return Get(dst, 0, dst.Length); }

		public int Get(T[] dst, int offset, int count)
		{
			var realCount = Math.Min(count, Size);
			var dstIndex = offset;
			for (var i = 0; i < realCount; i++, _head++, dstIndex++)
			{
				if (_head == _capacity)
					_head = 0;
				dst[dstIndex] = _buffer[_head];
			}

			Size -= realCount;
			return realCount;
		}

		// ReSharper disable once UnusedMethodReturnValue.Local
		T Get()
		{
			if (Size == 0)
				throw new InvalidOperationException("Buffer is empty.");

			var item = _buffer[_head];
			if (_head++ == _capacity)
				_head = 0;
			Size--;
			return item;
		}

		void CopyTo(T[] array, int arrayIndex, int count)
		{
			if (count > Size)
				throw new ArgumentOutOfRangeException("count", "count cannot be greater than the buffer size.");

			var bufferIndex = _head;
			for (var i = 0; i < count; i++, bufferIndex++, arrayIndex++)
			{
				if (bufferIndex == _capacity)
					bufferIndex = 0;
				array[arrayIndex] = _buffer[bufferIndex];
			}
		}

		IEnumerator<T> GetEnumerator()
		{
			var bufferIndex = _head;
			for (var i = 0; i < Size; i++, bufferIndex++)
			{
				if (bufferIndex == _capacity)
					bufferIndex = 0;

				yield return _buffer[bufferIndex];
			}
		}

		// ReSharper disable once UnusedMember.Global
		public T[] GetBuffer() { return _buffer; }

		// ReSharper disable once UnusedMember.Global
		public T[] ToArray()
		{
			var dst = new T[Size];
			CopyTo(dst);
			return dst;
		}

		#region ICollection<T> Members

		int ICollection<T>.Count => Size;

		bool ICollection<T>.IsReadOnly => false;

		void ICollection<T>.Add(T item) { Put(item); }

		bool ICollection<T>.Remove(T item)
		{
			if (Size == 0)
				return false;

			Get();
			return true;
		}

		#endregion

		#region ICollection Members

		int ICollection.Count => Size;

		bool ICollection.IsSynchronized => false;

		object ICollection.SyncRoot
		{
			get
			{
				if (_syncRoot == null)
					Interlocked.CompareExchange(ref _syncRoot, new object(), null);
				return _syncRoot;
			}
		}

		void ICollection.CopyTo(Array array, int arrayIndex) { CopyTo((T[]) array, arrayIndex); }

		#endregion
	}
}
