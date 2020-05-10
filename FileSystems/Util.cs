/* vi: ts=2
 */

using System;
using System.IO;
using System.Reflection;
using Encoding = System.Text.Encoding;
using StringBuilder = System.Text.StringBuilder;

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
#endif

		internal static bool IsPassError(int errno, int keep, bool writing)
			=> (errno == IOErrors.FileNotFound || errno == IOErrors.PathNotFound
				|| errno == IOErrors.IsADirectory || errno == IOErrors.IsNotADirectory
				|| errno == IOErrors.BadPathName || errno == IOErrors.TooManySymbolicLinks
				|| errno == keep || (!writing && errno == IOErrors.PermissionDenied));

#if OS_WIN
		internal static IOException GetExceptionFromNtStatus(int status, string path, bool canpass, int keep, bool writing)
		{
			NativeMethods.SetLastError(NativeMethods.RtlNtStatusToDosError(status));
			return GetExceptionFromLastError(path, canpass, keep, writing);
		}

		internal static IOException GetExceptionFromNtStatus(int status, Func<string> path, bool canpass, int keep, bool writing)
		{
			NativeMethods.SetLastError(NativeMethods.RtlNtStatusToDosError(status));
			return GetExceptionFromLastError(path, canpass, keep, writing);
		}
#endif

		internal static IOException GetExceptionFromLastError(string path, bool canpass, int keep, bool writing)
		{
			int errno = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
			if (errno == 0) throw new IOException((path is null) ? "error code lost" : (path + ": error code lost"));
#if OS_WIN
			errno |= unchecked((int)0x80070000);
#endif
			if (canpass && IsPassError(errno, keep, writing)) return null;
			var ci = new System.ComponentModel.Win32Exception();
			return GetExceptionFromErrno(errno, path, ci.Message);
		}

		internal static IOException GetExceptionFromLastError(Func<string> path, bool canpass, int keep, bool writing)
		{
			int errno = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
			if (errno == 0) throw new IOException((path is null) ? "error code lost" : (path + ": error code lost"));
#if OS_WIN
			errno |= unchecked((int)0x80070000);
#endif
			if (canpass && IsPassError(errno, keep, writing)) return null;
			var ci = new System.ComponentModel.Win32Exception();
			return GetExceptionFromErrno(errno, path?.Invoke(), ci.Message);
		}

		private static IOException GetExceptionFromErrno(int errno, string path, string cimsg)
		{
			var msg = (path is null) ? cimsg : (path + ": " + cimsg);
			if (errno == IOErrors.FileNotFound) {
#if OSTYPE_UNIX
				// Extra check to translate exception type
				if (path is string) {
					var idx = path.LastIndexOf('/');
					if (idx >= 0 && !FileSystem.DirectoryExists(path.Substring(0, idx)))
						return new DirectoryNotFoundException(msg); // DriveNotFound can't happen here
				}
#endif
				return new FileNotFoundException(msg);
			} else if (errno == IOErrors.PathNotFound) {
#if OS_WIN
				// Extra check to translate exception type
				if (path is string && path.Length > 0 && path[1] == ':' && path[2] == '\\' &&
					(path[0] >= 'A' && path[0] <= 'Z' || path[0] >= 'a' && path[0] <= 'z'))
				{
					if (!FileSystem.DirectoryExists(path.Substring(0, 3)))
						throw new DriveNotFoundException(msg);
				}
#endif
				return new DirectoryNotFoundException(msg);
			} else
				return new IOException(msg, errno);
		}
	}
}
