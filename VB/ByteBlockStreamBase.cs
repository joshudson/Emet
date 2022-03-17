/* vi:ts=2
 */

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Emet.VB.Extensions;
using CancellationToken = System.Threading.CancellationToken;

namespace Emet.VB {
	///<summary>This stream provides VB .NET with the ability to implement the new high-speed IO methods via IntPtr instead of Span.</summary>
	public abstract class ByteBlockStreamBase : Stream {

		///<summary>When overridden in a derived class, provides the functionality to read from the stream.</summary>
		///<param name="buffer">The unmanaged pointer to the buffer to read from; this will only be NULL if count is zero.</param>
		///<param name="count">The number of bytes to read</param>
		///<remarks>The CopyMemory methods in the Emet.VB namespace can be used to fill the buffer.</remarks>
		protected abstract int Read(IntPtr buffer, int count);
		///<summary>When overridden in a derived class, provides the functionality to write to the stream.</summary>
		///<param name="buffer">The unmanaged pointer to the buffer to write to; this will only be NULL if count is zero.</param>
		///<param name="count">The number of bytes to write</param>
		///<remarks>The CopyMemory methods in the Emet.VB namespace can be used to consume the buffer.</remarks>
		protected abstract void Write(IntPtr buffer, int count);
#if NET45 || NET10
		///<summary>When overridden in a derived class, provides the functionality to read from the stream asynchronously.</summary>
		///<param name="buffer">The unmanaged pointer to the buffer to read from; this will only be NULL if count is zero.</param>
		///<param name="count">The number of bytes to read</param>
		///<param name="cancellationToken">The read cancellation token</param>
		protected virtual Task<int> ReadAsync(IntPtr buffer, int count, CancellationToken cancellationToken)
			=> Task.Run(() => Read(buffer, count));
		///<summary>When overridden in a derived class, provides the functionality to write to the stream asynchronously.</summary>
		///<param name="buffer">The unmanaged pointer to the buffer to write to; this will only be NULL if count is zero.</param>
		///<param name="count">The number of bytes to write</param>
		///<param name="cancellationToken">The write cancellation token</param>
		protected virtual Task WriteAsync(IntPtr buffer, int count, CancellationToken cancellationToken)
			=> Task.Run(() => Write(buffer, count));
#endif

		public override unsafe int Read(byte[] buffer, int offset, int count)
		{
			if (buffer is null) {
				if (count > 0) throw new ArgumentNullException(nameof(buffer));
				return Read(IntPtr.Zero, 0);
			}
			fixed(byte *b = buffer)
				return Read((IntPtr)b + offset, count);
		}

		public override unsafe void Write(byte[] buffer, int offset, int count)
		{
			if (buffer is null) {
				if (count > 0) throw new ArgumentNullException(nameof(buffer));
				Write(IntPtr.Zero, 0);
				return;
			}
			fixed(byte *b = buffer)
				Write((IntPtr)b + offset, count);
		}

#if NET45 || NET10
		public async override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
		{
			var handle = (GCHandle)default;
			try {
				IntPtr b = IntPtr.Zero;
				if (buffer is null) {
					if (count > 0) throw new ArgumentNullException(nameof(buffer));
				} else {
					handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
					b = handle.AddrOfPinnedObject() + offset;
				}
				return await ReadAsync(b, count, cancellationToken);
			} finally {
				if (handle.IsAllocated) handle.Free();
			}
		}

		public async override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
		{
			var handle = (GCHandle)default;
			try {
				IntPtr b = IntPtr.Zero;
				if (buffer is null) {
					if (count > 0) throw new ArgumentNullException(nameof(buffer));
				} else {
					handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
					b = handle.AddrOfPinnedObject() + offset;
				}
				await WriteAsync(b, count, cancellationToken);
			} finally {
				if (handle.IsAllocated) handle.Free();
			}
		}
#endif

#if NET30
		public override unsafe int Read(Span<byte> buffer)
		{
			fixed (byte *b = buffer)
				return Read((IntPtr)b, buffer.Length);
		}

		public override unsafe void Write(ReadOnlySpan<byte> buffer)
		{
			fixed (byte *b = buffer)
				Write((IntPtr)b, buffer.Length);
		}

		public async override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
		{
			// Using block miscompiles
			var handle = buffer.Pin();
			try {
				return await ReadAsync(handle.GetPointer(), buffer.Length, cancellationToken);
			} finally {
				handle.Dispose();
			}
		}

		public async override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
		{
			// Using block miscompiles
			var handle = buffer.Pin();
			try {
				await WriteAsync(handle.GetPointer(), buffer.Length, cancellationToken);
			} finally {
				handle.Dispose();
			}
		}
#endif
	}

	///<summary>Adds redirection routines to ByteBlockStreamBase so that some calls can be sent to another stream</summary>
	public abstract class ByteBlockStreamAndRedirectBase : ByteBlockStreamBase {

		protected Stream BackingStream {get;set;}

		protected bool RedirectReads {get;set;}

		protected bool RedirectWrites {get;set;}

		public ByteBlockStreamAndRedirectBase(Stream backingStream) => BackingStream = backingStream;

		public override int Read(byte[] buffer, int offset, int count)
			=> RedirectReads ? BackingStream.Read(buffer, offset, count) : base.Read(buffer, offset, count);

		public override void Write(byte[] buffer, int offset, int count)
		{
			if (RedirectWrites)
				BackingStream.Write(buffer, offset, count);
			else
				base.Write(buffer, offset, count);
		}

		public override void Flush() => BackingStream.Flush();

#if NET45 || NET10
		public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
			=> RedirectReads ? BackingStream.ReadAsync(buffer, offset, count, cancellationToken) : base.ReadAsync(buffer, offset, count, cancellationToken);

		public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
			=> RedirectWrites ? BackingStream.WriteAsync(buffer, offset, count, cancellationToken) : base.WriteAsync(buffer, offset, count, cancellationToken);

		public override Task FlushAsync(CancellationToken cancellationToken) => BackingStream.FlushAsync(cancellationToken);
#endif

#if NET30
		public override int Read(Span<byte> buffer)
			=> RedirectReads ? BackingStream.Read(buffer) : base.Read(buffer);

		public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
			=> RedirectReads ? BackingStream.ReadAsync(buffer, cancellationToken) : base.ReadAsync(buffer, cancellationToken);

		public override void Write(ReadOnlySpan<byte> buffer)
		{
			if (RedirectWrites)
				BackingStream.Write(buffer);
			else
				base.Write(buffer);
		}

		public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
			=> RedirectWrites ? BackingStream.WriteAsync(buffer, cancellationToken) : base.WriteAsync(buffer, cancellationToken);
#endif

		protected override void Dispose(bool disposing)
		{
			try {
				if (disposing) BackingStream.Dispose();
			} finally {
				base.Dispose(disposing);
			}
		}
	}
}
