/* vi: ts=2
 */

using System;
using System.Collections.Generic;
using System.IO;
using Encoding = System.Text.Encoding;
using Marshal = System.Runtime.InteropServices.Marshal;
using StringBuilder = System.Text.StringBuilder;
using Win32Exception = System.ComponentModel.Win32Exception;

namespace Emet.FileSystems {

	internal static class Util {
#if OS_WIN
		internal static DateTime FileTimeToDateTime(NativeMethods.FILETIME filetime)
			=> UlongToDateTime(((ulong)filetime.dwHighDateTime) << 32 | filetime.dwLowDateTime);

		internal static DateTime UlongToDateTime(ulong datetime)
			// Documentation literally says 100ns in both places
			=> NativeMethods.Epoch.AddTicks((long)datetime);

		internal static string CharArrayToString(char[] namestring)
		{
			int slen = namestring.Length;
			int len;
			for (len = 0; len < slen; len++)
				if (namestring[len] == 0)
					break;
			return new string(namestring, 0, len);
		}

		internal sealed class ErrorPathBuilder {
			private string parent;
			private List<KeyValuePair<IntPtr, int>> values;

			internal ErrorPathBuilder(string parent) {
				this.parent = parent;
				this.values = new List<KeyValuePair<IntPtr, int>>();
			}

			internal void Push(IntPtr str, int len, ref bool added)
			{
				try {} finally {
					values.Add(new KeyValuePair<IntPtr, int>(str, len));
					added = true;
				}
			}

			internal void Pop(bool added) { if (added) values.RemoveAt(values.Count - 1); }

			public unsafe override string ToString()
			{
				int wlen = parent.Length;
				foreach (var kv in values)
					wlen += 1 + kv.Value;
				var data = new char[wlen];
				int offset = parent.Length;
				parent.CopyTo(0, data, 0,  offset);
				foreach (var kv in values) {
					data[offset++] = '\\';
					char *s = (char *)kv.Key.ToPointer();
					int ln = kv.Value;
					while ((ln--) > 0)
						data[offset++] = *s++;
				}
				return new string(data);
			}
		}

		internal static unsafe bool IsAnyChar(IntPtr str, int len, char c)
		{
			char *s = (char*)str.ToPointer();
			for (; len-- > 0; s++)
				if (*s == c)
					return true;
			return false;
		}

		internal static unsafe bool AreAllChar(IntPtr str, int len, char c)
		{
			char *s = (char*)str.ToPointer();
			for (; len-- > 0; s++)
				if (*s != c)
					return false;
			return true;
		}
#endif

#if OSTYPE_UNIX
    internal static DateTime UnixTimeToDateTime(long seconds, ulong nanoseconds)
			=> NativeMethods.Epoch.AddTicks(
					unchecked(seconds * TimeSpan.TicksPerSecond + (long)(nanoseconds / 100)));

		internal static string ByteArrayToName(byte[] namebytes)
		{
			int i;
			for (i = 0; i < namebytes.Length; i++)
				if (namebytes[i] == 0) break;
			return Encoding.UTF8.GetString(namebytes, 0, i);
		}

		internal static string ByteArrayToName(byte[] namebytes, int offset, int length)
			=> Encoding.UTF8.GetString(namebytes, offset, length);

		internal static byte[] NameToByteArray(string name)
		{
			var count = Encoding.UTF8.GetByteCount(name);
			var bytes = new byte[count + 1];
			//bytes[count] = 0; // Already done for us.
			Encoding.UTF8.GetBytes(name, 0, name.Length, bytes, 0);
			return bytes;
		}

		internal static void NameToByteArray(byte[] namebytes, int byteoffset, string name, int offset, int length)
		{
			namebytes[Encoding.UTF8.GetBytes(name, offset, length, namebytes, byteoffset)] = 0;
		}

		internal static byte[] NameToByteArrayExact(string path)
		{
			var count = Encoding.UTF8.GetByteCount(path);
			if (count > 255) throw GetExceptionFromErrno(IOErrors.BadPathName, path, path, new Win32Exception(IOErrors.BadPathName).Message);
			var bytes = new byte[count];
			Encoding.UTF8.GetBytes(path, 0, path.Length, bytes, 0);
			for (int i = 0; i < count; i++)
				if (bytes[i] == 0)
					throw new ArgumentException("An embedded null byte was found in the path");
			return bytes;
		}

		internal sealed class ErrorPathBuilder {
			private struct Triplet {
				internal byte[] Bytes { get; }
				internal int Offset { get; }
				internal int Length { get; }
				internal Triplet(byte[] bytes, int offset, int length)
				{
					Bytes = bytes;
					Offset = offset;
					Length = length;
				}
			}
			private List<Triplet> values = new List<Triplet>();

			internal void Push(byte[] str, int offset, int len, ref bool added)
			{
				try {} finally {
					values.Add(new Triplet(str, offset, len));
					added = true;
				}
			}

			internal void Pop(bool added) { if (added) values.RemoveAt(values.Count - 1); }

			public override string ToString()
			{
				if (values.Count == 0) return string.Empty;
				int wlen = -1;
				foreach (var t in values)
					wlen += 1 + Encoding.UTF8.GetCharCount(t.Bytes, t.Offset, t.Length);
				var data = new char[wlen];
				int offset = 0;
				foreach (var t in values) {
					if (offset != 0) data[offset++] = '/';
					offset += Encoding.UTF8.GetChars(t.Bytes, t.Offset, t.Length, data, offset);
				}
				return new string(data);
			}
		}

		internal static bool IsEIntrSyscallReturn(int ereturn)
			=> ereturn < 0 && Marshal.GetLastWin32Error() == IOErrors.Interrupted;

		internal static bool IsEIntrSyscallReturnOrException(int ereturn, string chkpath, string path, bool canpass, int keep, bool writing, out IOException exception)
		{
			if (ereturn >= 0) { exception = null; return false; }
			int errno = Marshal.GetLastWin32Error();
			if (errno == IOErrors.Interrupted) { exception = null; return true; }
			if (canpass && IsPassError(errno, keep, writing)) { exception = null; return false; }
			var ci = new Win32Exception();
			exception = GetExceptionFromErrno(errno, chkpath, path, ci.Message);
			return false;
		}

		internal static bool IsEIntrSyscallReturnOrException(long ereturn, string chkpath, string path, bool canpass, int keep, bool writing, out IOException exception)
		{
			if (ereturn >= 0) { exception = null; return false; }
			return IsEIntrSyscallReturnOrException(-1, chkpath, path, canpass, keep, writing, out exception);
		}

#endif

		internal static bool IsPassError(int errno, int keep, bool writing)
			=> (errno == IOErrors.FileNotFound || errno == IOErrors.PathNotFound
				|| errno == IOErrors.IsNotADirectory || (!writing && errno == IOErrors.IsNotADirectory)
				|| errno == IOErrors.BadPathName || errno == IOErrors.TooManySymbolicLinks
				|| errno == keep || (!writing && errno == IOErrors.PermissionDenied));

#if OS_WIN
		internal static IOException GetExceptionFromNtStatus(int status, string path, bool canpass, int keep, bool writing)
		{
			NativeMethods.SetLastError(NativeMethods.RtlNtStatusToDosError(status));
			return GetExceptionFromLastError(path, path, canpass, keep, writing);
		}

		internal static IOException GetExceptionFromNtStatus(int status, Func<string> path, bool canpass, int keep, bool writing)
		{
			NativeMethods.SetLastError(NativeMethods.RtlNtStatusToDosError(status));
			return GetExceptionFromLastError(path, canpass, keep, writing);
		}
#endif

		internal static IOException GetExceptionFromLastError(string chkpath, string path, bool canpass, int keep, bool writing)
		{
			int errno = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
			if (errno == 0) return new IOException((path is null) ? "error code lost" : (path + ": error code lost"));
#if OS_WIN
			errno |= unchecked((int)0x80070000);
#endif
			if (canpass && IsPassError(errno, keep, writing)) return null;
			var ci = new Win32Exception();
			return GetExceptionFromErrno(errno, chkpath, path, ci.Message);
		}

		internal static IOException GetExceptionFromLastError(Func<string> path, bool canpass, int keep, bool writing)
		{
			int errno = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
			if (errno == 0) { string spath = path?.Invoke(); return new IOException((spath is null) ? "error code lost" : (spath + ": error code lost")); }
#if OS_WIN
			errno |= unchecked((int)0x80070000);
#endif
			if (canpass && IsPassError(errno, keep, writing)) return null;
			var ci = new Win32Exception();
			var path2 = path?.Invoke();
			return GetExceptionFromErrno(errno, path2, path2, ci.Message);
		}

		internal static IOException GetExceptionFromErrno(int errno, string chkpath, string path, string cimsg)
		{
			var msg = (path is null) ? cimsg : (path + ": " + cimsg);
			if (errno == IOErrors.FileNotFound) {
#if OSTYPE_UNIX
				// Extra check to translate exception type
				if (chkpath is not null) {
					if (chkpath.Length == 0) return SetHResult(new DirectoryNotFoundException(msg), IOErrors.FileNotFound); // It's preordained
					var idx = chkpath.LastIndexOf('/');
					if (idx >= 0 && !FileSystem.DirectoryExists(chkpath.Substring(0, idx)))
						return SetHResult(new DirectoryNotFoundException(msg), IOErrors.FileNotFound); // DriveNotFound can't happen here
				}
#endif
				return SetHResult(new FileNotFoundException(msg), IOErrors.FileNotFound);
			} else if (errno == IOErrors.PathNotFound) {
#if OS_WIN
				// Extra check to translate exception type
				if (path is string && path.Length > 0 && path[1] == ':' && path[2] == '\\' &&
					(path[0] >= 'A' && path[0] <= 'Z' || path[0] >= 'a' && path[0] <= 'z'))
				{
					if (!FileSystem.DirectoryExists(path.Substring(0, 3)))
						return SetHResult(new DriveNotFoundException(msg), IOErrors.PathNotFound);
				}
#endif
				return SetHResult(new DirectoryNotFoundException(msg), IOErrors.PathNotFound);
			} else
				return new IOException(msg, errno);
		}

		// Our target is too old to directly write HResult = value, so do the long way around.
		// I really do still need .NET Framework 4.8 builds so this just isn't changing.
		private static System.Reflection.MethodInfo sethresult;
		internal static IOException SetHResult(IOException exception, int value)
		{
			sethresult ??= typeof(Exception).GetProperty("HResult", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetMethod;
			sethresult.Invoke(exception, new object[]{value});
			return exception;
		}
	}
}

namespace System {
	internal sealed class NotNullWhenAttribute : Attribute {
		private bool value;
		public bool ReturnValue => value;
		public NotNullWhenAttribute(bool returnValue) { value = returnValue; }
	}
}
