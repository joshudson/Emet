using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Emet.VB {
#if NET40
	internal static class Buffer {
		[DllImport("kernel32.dll")]
		private static unsafe extern void CopyMemory(void *dest, void *src, UIntPtr length);

		internal static unsafe void MemoryCopy(void *source, void *destination, long length1, long length2)
		{
			if (length1 != length2) throw new InvalidProgramException("Mismatched lengths in internal call to MemoryCopy. Check for heap corruption.");
			CopyMemory(destination, source, new UIntPtr((UInt64)length1));
		}
	}
#endif

	///<summary>Provides CopyMemory</summary>
	[Microsoft.VisualBasic.CompilerServices.StandardModule]
	public static class MemoryOperations {
		///<summary>When invoked via reflection, returns the managed size of a type. This is not the same as the native size returned by Marshal.SizeOf()</summary>
		public static unsafe int SizeOfStruct<T>() where T: unmanaged => sizeof(T);

		public static unsafe void CopyMemory(IntPtr destination, IntPtr source, long length)
		{
			if (length == 0) return;
			if (source == IntPtr.Zero) throw new ArgumentNullException(nameof(source));
			if (destination == IntPtr.Zero) throw new ArgumentNullException(nameof(destination));
			Buffer.MemoryCopy((void *)source, (void *)destination, length, length);
		}

		public static unsafe void CopyMemory(byte[] destination, long offset, IntPtr source, long length)
		{
			if (length == 0) return;
			if (source == IntPtr.Zero) throw new ArgumentNullException(nameof(source));
			if (destination is null) throw new ArgumentNullException(nameof(destination));
#if SHORTLENGTH
			if (destination.Length < offset + length)
#else
			if (destination.LongLength < offset + length)
#endif
				throw new IndexOutOfRangeException("Tried to copy outside of destination");
			fixed(byte *d = destination)
				Buffer.MemoryCopy((void *)source, (void *)(d + offset), length, length);
		}

		public static unsafe void CopyMemory(IntPtr destination, byte[] source, long offset, long length)
		{
			if (length == 0) return;
			if (source is null) throw new ArgumentNullException(nameof(source));
			if (destination == IntPtr.Zero) throw new ArgumentNullException(nameof(destination));
#if SHORTLENGTH
			if (source.Length < offset + length)
#else
			if (source.LongLength < offset + length)
#endif
				throw new IndexOutOfRangeException("Tried to copy outside of destination");
			fixed(byte *s = source)
				Buffer.MemoryCopy((void *)(s + offset), (void *)destination, length, length);
		}

		public static unsafe void CopyMemory(byte[] destination, long destinationoffset, byte[] source, long sourceoffset, long length)
		{
			if (length == 0) return;
			if (source is null) throw new ArgumentNullException(nameof(source));
			if (destination is null) throw new ArgumentNullException(nameof(destination));
#if SHORTLENGTH
			if (destination.Length < destinationoffset + length)
				throw new IndexOutOfRangeException("Tried to copy outside of destination");
			if (source.Length < sourceoffset + length)
				throw new IndexOutOfRangeException("Tried to copy outside of destination");
#else
			if (destination.LongLength < destinationoffset + length)
				throw new IndexOutOfRangeException("Tried to copy outside of destination");
			if (source.LongLength < sourceoffset + length)
				throw new IndexOutOfRangeException("Tried to copy outside of destination");
#endif
			fixed(byte *d = destination)
			fixed(byte *s = source)
				Buffer.MemoryCopy((void *)(s + sourceoffset), (void *)(d + destinationoffset), length, length);
		}

#if NET30
		public static unsafe void CopyMemory(Memory<byte> destination, IntPtr source, long length)
		{
			if (length == 0) return;
			if (source == IntPtr.Zero) throw new ArgumentNullException(nameof(source));
			if (destination.Length > length) throw new IndexOutOfRangeException("Tried to copy outside of destination");
			fixed(byte *d = destination.Span)
				Buffer.MemoryCopy((void *)source, (void *)d, length, length);
		}

		public static unsafe void CopyMemory(IntPtr destination, Memory<byte> source, long length)
		{
			if (length == 0) return;
			if (destination == IntPtr.Zero) throw new ArgumentNullException(nameof(destination));
			if (source.Length > length) throw new IndexOutOfRangeException("Tried to copy outside of source");
			fixed(byte *s = source.Span)
				Buffer.MemoryCopy((void *)s, (void *)destination, length, length);
		}

		public static unsafe void CopyMemory(Memory<byte> destination, Memory<byte> source)
		{
			if (source.Length != destination.Length) throw new IndexOutOfRangeException((source.Length > destination.Length)
				? "Tried to copy outside of destination"
				: "Tried to copy outside of source");
			fixed (byte *s = source.Span)
			fixed (byte *d = destination.Span)
				Buffer.MemoryCopy((void *)s, (void *)d, source.Length, destination.Length);
		}
#endif
	}
}
