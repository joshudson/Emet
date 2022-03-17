/* vi:ts=2
 */

#if NET30

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Emet.VB.Extensions {
	///<summary>Provides extensions on Memory and ReadOnlyMemory so that VB has a chance at using Memory APIs even though it can't access Memory.Span</summary>
	[Microsoft.VisualBasic.CompilerServices.StandardModule]
	public static class MemoryExtensions {
		///<summary>Appends the source to the destination</summary>
		///<param name="source">the memory to copy from</param>
		///<param name="destination">the list to append to</param>
		///<exception cref="System.ArgumentNullException">Thrown when source.Length > 0 and destination is null</exception>
		public static void AppendTo<T>(this ReadOnlyMemory<T> source, IList<T> destination)
		{
			int len = source.Length;
			if (len == 0) return;
			if (destination is null) throw new ArgumentNullException(nameof(destination));
			if (destination.Count < (len >> 1)) {
				// Avoid repeated grows
				var d2 = destination as List<T>;
				if (d2 is object) d2.Capacity += len;
			}
			var span = source.Span;
			for (int i = 0; i < len; i++)
				destination.Add(span[i]);
		}

		///<summary>Copies source to destination</summary>
		///<param name="source">the memory to copy from</param>
		///<param name="destination">the list (or array) to copy to</param>
		///<param name="offset">where in the list to start copying</param>
		///<exception cref="System.ArgumentNullException">Thrown when source.Length > 0 and destination is null</exception>
		///<exception cref="System.IndexOutOfRangeException">Thrown immediately if the copy won't fit</exception>
		public static void CopyTo<T>(this ReadOnlyMemory<T> source, IList<T> destination, int offset = 0)
		{
			if (source.Length == 0) return;
			if (destination is null) throw new ArgumentNullException(nameof(destination));
			if ((uint)source.Length + (uint)offset < (uint)destination.Count) throw new IndexOutOfRangeException();
			var span = source.Span;
			var limit = span.Length;
			for (int i = 0; i < limit; i++)
				destination[offset + i] = span[i];
		}

		///<summary>Copies source to destination</summary>
		///<param name="source">the memory to copy from</param>
		///<param name="destination">the memory to copy to</param>
		///<exception cref="System.IndexOutOfRangeException">thrown immediately when the source's length and destination's length aren't the same</exception>
		public static void CopyTo<T>(this ReadOnlyMemory<T> source, Memory<T> destination)
		{
			if (source.Length != destination.Length) throw new ArgumentOutOfRangeException();
			var s = source.Span;
			var d = destination.Span;
			var l = s.Length;
			for (int i = 0; i < l; i++)
				d[i] = s[i];
		}

		///<summary>Copies source to destination</summary>
		///<param name="source">the list to copy from</param>
		///<param name="destination">the memory to copy to</param>
		///<exception cref="System.ArgumentNullException">Thrown when destination.Length > 0 and source is null</exception>
		///<exception cref="System.IndexOutOfRangeException">Thrown immediately if source it too short</exception>
		public static void CopyTo<T>(this IReadOnlyList<T> source, Memory<T> destination)
			=> CopyTo(source, 0, destination);

		///<summary>Copies source to destination</summary>
		///<param name="source">the list to copy from</param>
		///<param name="offset">the location to start copying from</param>
		///<param name="destination">the memory to copy to</param>
		///<exception cref="System.ArgumentNullException">Thrown when destination.Length > 0 and source is null</exception>
		///<exception cref="System.IndexOutOfRangeException">Thrown immediately if source it too short</exception>
		public static void CopyTo<T>(this IReadOnlyList<T> source, int offset, Memory<T> destination)
		{
			if (destination.Length == 0) return;
			if (source is null) throw new ArgumentNullException(nameof(source));
			if ((uint)destination.Length + (uint)offset < (uint)source.Count) throw new IndexOutOfRangeException();
			var span = destination.Span;
			var l = span.Length;
			for (int i = 0; i < l; i++)
				span[i] = source[i + offset];
		}

		///<summary>Gets the pointer from a MemoryHandle as an IntPtr</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static unsafe IntPtr GetPointer(this System.Buffers.MemoryHandle source)
			=> (IntPtr)source.Pointer;

#if NET30
		public static unsafe int Read(this System.IO.Stream stream, IntPtr buffer, int count)
			=> stream.Read(new Span<byte>((byte *)buffer, count));

		public static unsafe void Write(this System.IO.Stream stream, IntPtr buffer, int count)
			=> stream.Write(new ReadOnlySpan<byte>((byte *)buffer, count));
#endif
	}
}

#endif
