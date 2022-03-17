/* vi:ts=2
 */

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CancellationToken = System.Threading.CancellationToken;

namespace Emet.VB {
	///<summary>
	///This is the abstract base class for providing a wrapped stream. It overrides all four variants of Read() and Write()
	///as well as Close() and Dispose(). Unfortunately, this only really works for a few select kinds of wrapped streams
	///because there's no way to manipulate the data in the buffer of ReadAsync(Span(Of Byte)) in Visual Basic.
	///The Adjust method family should be overridden to provide range-based functionality. Position, Length, and SetLength()
	///throw, but may be replaced by the derived class. Seek is pre-implemented in terms of Position.
	///</summary>
	public abstract class WrappedStreamBase : Stream
	{
		protected Stream BackingStream { 
			[MethodImpl(MethodImplOptions.AggressiveInlining)] get;
			[MethodImpl(MethodImplOptions.AggressiveInlining)] set;
		}

#if NET45 || NET10
		// Turns out these don't even allocate memory.
		///<summary>Convenient access to a truthy task</summary>
		protected static Task<bool> TrueResult { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; } = Task.FromResult(true);
		///<summary>Convenient access to a falsy task</summary>
		protected static Task<bool> FalseResult { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; } = Task.FromResult(false);
#endif

		///<summary>When called from a derived class, constructs the wrapped stream base, initally backed by this stream</summary>
		protected WrappedStreamBase(Stream backingStream) => this.BackingStream = backingStream;

		///<summary>When overridden in a derived class, adjusts the amout of data to read</summary>
		///<param name="size">the number of bytes to read</param>
		protected abstract void AdjustBeforeRead(ref int size);
		///<summary>When overridden in a dreived class, adjusts the amout of data to write</summary>
		///<param name="size">the number of bytes to write</param>
		///<remarks>part-writes are impossible but making the write smaller will chunk the write</remarks>
		protected abstract void AdjustBeforeWrite(ref int size);
		///<summary>When overridden in a derived class, adjusts the internal state of the derived class after read</summary>
		///<param name="size">the number of bytes that were read</param>
		///<param name="rqsize">the number of bytes that were requested to be read</param>
		///<returns>whether or not to continue the read if there is more room</returns>
		protected virtual bool AdjustAfterRead(int size, int rqsize) { return false; }
		///<summary>When overridden in a derived class, adjusts the internal state of the derived class after write</summary>
		///<param name="size">the number of bytes that were read</param>
		///<remarks>Part writes are impossible so the loop can't be bailed out of</remarks>
		protected virtual void AdjustAfterWrite(int size) {}
		///<summary>When overridden in a derived class, adjusts the internal state of the derived class after a failed read</summary>
		protected virtual void AdjustAfterReadFailed(IOException ex) {}
		///<summary>When overridden in a derived class, adjusts the internal state of the derived class after a failed write</summary>
		protected virtual void AdjustAfterWriteFailed(IOException ex) {}

#if NET45 || NET10

		///<summary>Adjusts the amout of data to read asynchronously</summary>
		///<param name="size">the number of bytes to read</param>
		///<param name="cancellationToken">the token to cancel async IO</param>
		///<returns>the number of bytes to read</returns>
		///<remarks>the default implementation calls AdjustBeforeRead</remarks>
		protected virtual Task<int> AdjustBeforeReadAsync(int size, CancellationToken cancellationToken) { AdjustBeforeRead(ref size); return Task.FromResult(size); }
		///<summary>Adjusts the amout of data to write asynchronously</summary>
		///<param name="size">the number of bytes to write</param>
		///<param name="cancellationToken">the token to cancel async IO</param>
		///<returns>the number of bytes to write</returns>
		///<remarks>the default implementation calls AdjustBeforeWrite</remarks>
		protected virtual Task<int> AdjustBeforeWriteAsync(int size, CancellationToken cancellationToken) { AdjustBeforeWrite(ref size); return Task.FromResult(size); }
		///<summary>Adjusts the internal state after read</summary>
		///<param name="size">the number of bytes read</param>
		///<param name="rqsize">the number of bytes requested</param>
		///<param name="cancellationToken">the token to cancel async IO</param>
		///<remarks>the default implementation calls AdjustAfterRead</remarks>
		protected virtual Task<bool> AdjustAfterReadAsync(int size, int rqsize, CancellationToken cancellationToken) { return AdjustAfterRead(size, rqsize) ? TrueResult : FalseResult; }
#if NET45
#pragma warning disable 1998
		///<summary>Adjusts the internal state after write</summary>
		///<param name="size">the number of bytes written</param>
		///<param name="cancellationToken">the token to cancel async IO</param>
		///<remarks>the default implementation calls AdjustAfterWrite</remarks>
		protected virtual async Task AdjustAfterWriteAsync(int size, CancellationToken cancellationToken) { AdjustAfterWrite(size); }
#pragma warning restore 1998
#else
		///<summary>Adjusts the internal state after write</summary>
		///<param name="size">the number of bytes written</param>
		///<param name="cancellationToken">the token to cancel async IO</param>
		///<remarks>the default implementation calls AdjustAfterWrite</remarks>
		protected virtual Task AdjustAfterWriteAsync(int size, CancellationToken cancellationToken) { AdjustAfterWrite(size); return Task.CompletedTask; }
#endif
#endif

		///<summary>When overridden in a derived class, adjusts the internal state of the derived class after a canceled read</summary>
		protected virtual void AdjustAfterReadCanceled(OperationCanceledException ex) {}
		///<summary>When overridden in a derived class, adjusts the internal state of the derived class after a canceled write</summary>
		protected virtual void AdjustAfterWriteCanceled(OperationCanceledException ex) {}

		public override int Read(byte[] buffer, int offset, int size)
		{
			int accum = 0;
			int sofar = 0;
			if (size == 0) {
				accum = 1;
				AdjustBeforeRead(ref accum);
				if (accum == 0) return 0;
				try {
					accum = BackingStream.Read(buffer, offset, 0);
					AdjustAfterRead(0, 0);
				} catch (IOException ex) {
					AdjustAfterReadFailed(ex);
					throw;
				}
				return 0;
			}
			try {
				do {
					size -= sofar;
					if (size == 0) return accum;
					AdjustBeforeRead(ref size);
					if (size == 0) return accum;
					sofar = BackingStream.Read(buffer, offset, size);
					accum += sofar;
					offset += sofar;
				} while (AdjustAfterRead(sofar, size));
			} catch (IOException ex) {
				AdjustAfterReadFailed(ex);
				throw;
			}
			return accum;
		}

#if NET30
		public override int Read(Span<byte> buffer)
		{
			int accum = 0;
			if (buffer.Length == 0) {
				accum = 1;
				AdjustBeforeRead(ref accum);
				if (accum == 0) return 0;
				try {
					accum = BackingStream.Read(buffer.Slice(0, 0));
					AdjustAfterRead(0, 0);
				} catch (IOException ex) {
					AdjustAfterReadFailed(ex);
					throw;
				}
				return 0;
			}
			int sofar;
			int size;
			try {
				do {
					size = buffer.Length;
					if (size == 0) return accum;
					AdjustBeforeRead(ref size);
					if (size == 0) return accum;
					buffer = buffer.Slice(0, size);
					sofar = BackingStream.Read(buffer);
					accum += sofar;
					buffer = buffer.Slice(sofar);
				} while (AdjustAfterRead(sofar, size));
			} catch (IOException ex) {
				AdjustAfterReadFailed(ex);
				throw;
			}
			return accum;
		}
#endif

#if NET45 || NET10
		public async override Task<int> ReadAsync(byte[] buffer, int offset, int size, CancellationToken cancellationToken = default)
		{
			int accum = 0;
			int sofar = 0;
			if (size == 0) {
				accum = await AdjustBeforeReadAsync(1, cancellationToken);
				if (accum == 0) return 0;
				try {
					accum = await BackingStream.ReadAsync(buffer, offset, 0, cancellationToken);
					await AdjustAfterReadAsync(0, 0, cancellationToken);
				} catch (IOException ex) {
					AdjustAfterReadFailed(ex);
					throw;
				} catch (OperationCanceledException ex) {
					AdjustAfterReadCanceled(ex);
					throw;
				}
				return 0;
			}
			try {
				do {
					size -= sofar;
					if (size == 0) return accum;
					size = await AdjustBeforeReadAsync(size, cancellationToken);
					if (size == 0) return accum;
					sofar = await BackingStream.ReadAsync(buffer, offset, size, cancellationToken);
					accum += sofar;
					offset += sofar;
				} while (await AdjustAfterReadAsync(sofar, size, cancellationToken));
			} catch (IOException ex) {
				AdjustAfterReadFailed(ex);
				throw;
			} catch (OperationCanceledException ex) {
				AdjustAfterReadCanceled(ex);
				throw;
			}
			return accum;
		}
#endif

#if NET30
		public async override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
		{
			int accum = 0;
			int size;
			int sofar;
			if (buffer.Length == 0) {
				accum = await AdjustBeforeReadAsync(1, cancellationToken);
				if (accum == 0) return 0;
				try {
					accum = await BackingStream.ReadAsync(buffer.Slice(0, 0), cancellationToken);
					await AdjustAfterReadAsync(0, 0, cancellationToken);
				} catch (IOException ex) {
					AdjustAfterReadFailed(ex);
					throw;
				} catch (OperationCanceledException ex) {
					AdjustAfterReadCanceled(ex);
					throw;
				}
				return 0;
			}
			try {
				do {
					size = buffer.Length;
					if (size == 0) return accum;
					size = await AdjustBeforeReadAsync(size, cancellationToken);
					if (size == 0) return accum;
					buffer = buffer.Slice(0, size);
					sofar = await BackingStream.ReadAsync(buffer, cancellationToken);
					accum += sofar;
					buffer = buffer.Slice(sofar);
				} while (await AdjustAfterReadAsync(sofar, size, cancellationToken));
			} catch (IOException ex) {
				AdjustAfterReadFailed(ex);
				throw;
			} catch (OperationCanceledException ex) {
				AdjustAfterReadCanceled(ex);
				throw;
			}
			return accum;
		}
#endif

		public override void Write(byte[] buffer, int offset, int size)
		{
			try {
				while (size > 0) {
					int size2 = size;
					AdjustBeforeWrite(ref size2);
					if (size2 == 0) throw new InvalidOperationException("AdjustBeforeWrite() Didn't make any progress.");
					BackingStream.Write(buffer, offset, size2);
					size -= size2;
					offset += size2;
					AdjustAfterWrite(size2);
				}
			} catch (IOException ex) {
				AdjustAfterWriteFailed(ex);
				throw;
			}
		}

#if NET30
		public override void Write(ReadOnlySpan<byte> buffer)
		{
			int size = buffer.Length;
			try {
				while (size > 0) {
					int size2 = size;
					AdjustBeforeWrite(ref size2);
					if (size2 == 0) throw new InvalidOperationException("AdjustBeforeWrite() Didn't make any progress.");
					BackingStream.Write(buffer.Slice(0, size2));
					AdjustAfterWrite(size2);
					if (size == size2) break;
					buffer = buffer.Slice(size2, size - size2);
					size = buffer.Length;
				}
			} catch (IOException ex) {
				AdjustAfterWriteFailed(ex);
				throw;
			} catch (OperationCanceledException ex) {
				AdjustAfterWriteCanceled(ex);
				throw;
			}
		}
#endif

#if NET45 || NET10
		public async override Task WriteAsync(byte[] buffer, int offset, int size, CancellationToken cancellationToken = default)
		{
			try {
				while (size > 0) {
					int size2 = await AdjustBeforeWriteAsync(size, cancellationToken);
					if (size2 == 0) throw new InvalidOperationException("AdjustBeforeWriteAsync() Didn't make any progress.");
					await BackingStream.WriteAsync(buffer, offset, size2, cancellationToken);
					await AdjustAfterWriteAsync(size2, cancellationToken);
					size -= size2;
					offset += size2;
				}
			} catch (IOException ex) {
				AdjustAfterWriteFailed(ex);
				throw;
			} catch (OperationCanceledException ex) {
				AdjustAfterWriteCanceled(ex);
				throw;
			}
		}
#endif

#if NET30
		public async override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
		{
			int size = buffer.Length;
			try {
				while (size > 0) {
					int size2 = size;
					size2 = await AdjustBeforeWriteAsync(size2, cancellationToken);
					if (size2 == 0) throw new InvalidOperationException("AdjustBeforeWriteAsync() Didn't make any progress.");
					await BackingStream.WriteAsync(buffer.Slice(0, size2), cancellationToken);
					await AdjustAfterWriteAsync(size2, cancellationToken);
					buffer = buffer.Slice(size2);
					size = buffer.Length;
				}
			} catch (IOException ex) {
				AdjustAfterWriteFailed(ex);
				throw;
			} catch (OperationCanceledException ex) {
				AdjustAfterWriteCanceled(ex);
				throw;
			}
		}
#endif

		public override void Flush() => BackingStream.Flush();

#if NET20 || NET40
		public override void Close() => BackingStream?.Close();
#endif

		protected override void Dispose(bool disposing) { if (disposing) BackingStream?.Dispose(); }

#if NET30
		public override ValueTask DisposeAsync() { return BackingStream?.DisposeAsync() ?? default; }
#endif

		// These just don't do anything useful. Provider can do something interesting if desired.

		public override long Position { get => throw new NotSupportedException("Non-seekable stream"); set => throw new NotSupportedException("Non-seekable stream"); }

		public override long Seek(long offset, SeekOrigin origin)
		{
			switch (origin) {
				case SeekOrigin.Begin: break;
				case SeekOrigin.Current: offset = Position + offset; break;
				case SeekOrigin.End: offset = Length + offset; break;
				default: throw new ArgumentOutOfRangeException(nameof(origin));
			}
			Position = offset;
			return offset;
		}

		public override long Length { get => throw new NotSupportedException("Non-seekable stream"); }

		public override void SetLength(long value) => throw new NotSupportedException("Non-seekable stream");
	}
}
