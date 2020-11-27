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

		// Turns out these don't even allocate memory.
		protected static Task<bool> TrueResult { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; } = Task.FromResult(true);
		protected static Task<bool> FalseResult { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; } = Task.FromResult(false);

		protected WrappedStreamBase(Stream backingStream) => this.BackingStream = backingStream;

		protected abstract void AdjustBeforeRead(ref int size);
		protected abstract void AdjustBeforeWrite(ref int size);
		protected virtual bool AdjustAfterRead(int size, int rqsize) { return false; }
		protected virtual void AdjustAfterWrite(int size) {}
		protected virtual void AdjustAfterReadFailed(IOException ex) {}
		protected virtual void AdjustAfterWriteFailed(IOException ex) {}

		protected virtual Task<int> AdjustBeforeReadAsync(int size, CancellationToken cancellationToken) { AdjustBeforeRead(ref size); return Task.FromResult(size); }
		protected virtual Task<int> AdjustBeforeWriteAsync(int size, CancellationToken cancellationToken) { AdjustBeforeWrite(ref size); return Task.FromResult(size); }
		protected virtual Task<bool> AdjustAfterReadAsync(int size, int rqsize, CancellationToken cancellationToken) { AdjustAfterRead(size, rqsize); return FalseResult; }
		protected virtual Task AdjustAfterWriteAsync(int size, CancellationToken cancellationToken) { AdjustAfterWrite(size); return Task.CompletedTask; }

		protected virtual void AdjustAfterReadCanceled(OperationCanceledException ex) {}
		protected virtual void AdjustAfterWriteCanceled(OperationCanceledException ex) {}

		public override int Read(byte[] buffer, int offset, int size)
		{
			int accum = 0;
			int sofar;
			try {
				do {
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

		public async override Task<int> ReadAsync(byte[] buffer, int offset, int size, CancellationToken cancellationToken = default)
		{
			int accum = 0;
			int sofar;
			try {
				do {
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

#if NET30
		public async override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
		{
			int accum = 0;
			int size;
			int sofar;
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

		public async override Task WriteAsync(byte[] buffer, int offset, int size, CancellationToken cancellationToken = default)
		{
			try {
				while (size > 0) {
					int size2 = size;
					size2 = await AdjustBeforeWriteAsync(size2, cancellationToken);
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

#if NET20
		public override void Close() => BackingStream?.Close();
#endif

		protected override void Dispose(bool disposing) { if (disposing) BackingStream?.Dispose(); }

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
