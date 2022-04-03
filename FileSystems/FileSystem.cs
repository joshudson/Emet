/* vi:ts=2
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using Encoding = System.Text.Encoding;
using static Emet.FileSystems.Util;

namespace Emet.FileSystems {
	///<summary>FileSystem contains static methods and properties for working with file systems</summary>
	public static class FileSystem {
		///<summary>Behavior for when enumerating the contents of a non-extant directory</summary>
		public enum NonExtantDirectoryBehavior : byte {
			///<summary>throw new DirectoryNotFoundException();</summary>
			Throw = 0,
			///<summary>return an enumerable with no items</summary>
			ReturnEmpty = 1,
			///<summary>return null</summary>
			///<remarks>this mode offers maximum performance if caller is expecting to handle it not being present</remarks>
			ReturnNull = 2,
		}

		///<summary>Behavior for what to do when encountering symbolic links</summary>
		public enum FollowSymbolicLinks : byte {
			///<summary>Never follow symbolic links</summary>
			Never = 0,
			///<summary>Always follow symbolic links</summary>
			///<remarks>If the link is broken the DirectoryEntry object might not be fully populated,
			///but FileType will be SymbolicLink</remarks>
			Always = 1,
			///<summary>Follow symbolic links if target is not a directory</summary>
			///<remarks>If the link is broken the DirectoryEntry object might not be fully populated,
			///but FileType will be SymbolicLink and LinkTargetHint will not be Directory.</remarks>
			IfNotDirectory = 2,
		}

		///<summary>Gets the contents of a directory</summary>
		///<param name="path">path to enumerate</param>
		///<param name="nonExtantDirectoryBehavior">What to do if the directory doesn't exist</param>
		///<param name="followSymbolicLinks">Whether or not to follow symbolic links</param>
		///<exception cref="System.IO.IOException">An IO error occurred accessing path</exception>
		///<remarks>Exceptions are not thrown if enumerating the directory encounters non-extant nodes</remarks>
		public static IEnumerable<DirectoryEntry> GetDirectoryContents(string path,
				NonExtantDirectoryBehavior nonExtantDirectoryBehavior = NonExtantDirectoryBehavior.Throw,
				FollowSymbolicLinks followSymbolicLinks = FollowSymbolicLinks.Never)
		{
#if OS_WIN
			IntPtr ffhandle = IOErrors.InvalidFileHandle;
			try {
				var ff = new NativeMethods.WIN32_FIND_DATA();
				ffhandle = NativeMethods.FindFirstFileW(path + "\\*", out ff);
				if (ffhandle == IOErrors.InvalidFileHandle) {
					var exception = GetExceptionFromLastError(path,
						nonExtantDirectoryBehavior == NonExtantDirectoryBehavior.ReturnEmpty || nonExtantDirectoryBehavior == NonExtantDirectoryBehavior.ReturnNull,
						0, false);
					if (exception is null) {
						return (nonExtantDirectoryBehavior == NonExtantDirectoryBehavior.ReturnEmpty) ? Enumerable.Empty<DirectoryEntry>() : null;
					}
					throw exception;
				}
				// Windows refuses to document that deleting the node from FindNextFile is safe.
				// Update: it isn't. Observed lived that deleting the node will occasionally cause a future entry to be skipped.
				var l = new DirectoryEntryList();
				do {
					var dname = ff.cFileName;
					if (dname == DirectoryEntry.CurrentDirectoryName || dname == DirectoryEntry.ParentDirectoryName) continue;
					l.Add(new DirectoryEntry(path, dname, followSymbolicLinks, ref ff));
				} while (0 != NativeMethods.FindNextFileW(ffhandle, out ff));
				var nhresult = unchecked((int)0x80070000 | (int)Marshal.GetLastWin32Error());
				if (nhresult != IOErrors.NoMoreFiles) {
					var ci = new System.ComponentModel.Win32Exception();
					throw new IOException(ci.Message, nhresult);
				}
				return l;
			} finally {
				if (ffhandle != IOErrors.InvalidFileHandle)
					NativeMethods.FindClose(ffhandle);
			}
#else
#if OSTYPE_UNIX
			IntPtr opendirhandle = IOErrors.InvalidFileHandle;
			try {
				opendirhandle = NativeMethods.opendir(NameToByteArray(path));
				if (opendirhandle == IOErrors.InvalidFileHandle) {
					var exception = GetExceptionFromLastError(path,
						nonExtantDirectoryBehavior == NonExtantDirectoryBehavior.ReturnEmpty || nonExtantDirectoryBehavior == NonExtantDirectoryBehavior.ReturnNull,
						0, false);
					if (exception is null) {
						return (nonExtantDirectoryBehavior == NonExtantDirectoryBehavior.ReturnEmpty) ? Enumerable.Empty<DirectoryEntry>() : null;
					}
					throw exception;
				}
				var entries = new List<DirectoryEntry>();
#if OS_LINUXX64
				var readentry = new NativeMethods.dirent64();
				readentry.d_name = new byte[256];
#endif
				for (;;) {
#if OS_LINUXX64
					int result = NativeMethods.readdir64_r(opendirhandle, ref readentry, out IntPtr z);
					if (result > 0)
						throw GetExceptionFromLastError(path, false, 0, false);
					if (z == IntPtr.Zero)
						break;
					int dhint = (int)readentry.d_type << 12;
					if (dhint == 0 || dhint > 0170000) dhint = (int)FileType.NodeHintNotAvailable;
					var dname = ByteArrayToName(readentry.d_name);
#else
					IntPtr result = NativeMethods.readdir(opendirhandle);
					if (result == IntPtr.Zero) {
						var errno = Marshal.GetLastWin32Error();
						if (errno == 0) break;
						throw GetExceptionFromLastError(path, false, 0, false);
					}
					var readentry = Marshal.PtrToStructure<NativeMethods.dirent>(result);
#if OS_FEATURES_DTYPE
					int dhint = (int)readentry.d_type << 12;
					if (dhint == 0 || dhint > 0170000) dhint = (int)FileType.NodeHintNotAvailable;
#else
					int dhint = (int)FileType.NodeHintNotAvailable;
#endif
					var bytes = new byte[readentry.d_namelen];
					Marshal.Copy(result + NativeMethods.dirent_d_name_offset, bytes, 0, readentry.d_namelen);
					var dname = ByteArrayToName(bytes);
#endif
					if (dname == DirectoryEntry.CurrentDirectoryName || dname == DirectoryEntry.ParentDirectoryName) continue;
					entries.Add(new DirectoryEntry(path, dname,
							followSymbolicLinks, (FileType)dhint, FileType.LinkTargetHintNotAvailable));
				}
				return entries;
			} finally {
				if (opendirhandle != IOErrors.InvalidFileHandle)
					NativeMethods.closedir(opendirhandle);
			}
#else
			throw null;
#endif
#endif
		}

		///<summary>Gets the contents of a directory or throw DirectoryNotFoundException if not found</summary>
		///<param name="path">path to enumerate</param>
		///<param name="followSymbolicLinks">Whether or not to follow symbolic links</param>
		///<exception cref="System.IO.IOException">An IO error occurred accessing path</exception>
		///<remarks>Exceptions are not thrown if enumerating the directory encounters non-extant nodes</remarks>
		public static IEnumerable<DirectoryEntry> GetDirectoryContentsOrThrow(string path,
				FollowSymbolicLinks followSymbolicLinks = FollowSymbolicLinks.Never)
			=> GetDirectoryContents(path, NonExtantDirectoryBehavior.Throw, followSymbolicLinks);

		///<summary>Gets the contents of a directory or an empty enumerable for not found</summary>
		///<param name="path">path to enumerate</param>
		///<param name="followSymbolicLinks">Whether or not to follow symbolic links</param>
		///<exception cref="System.IO.IOException">An IO error occurred accessing path</exception>
		///<remarks>Exceptions are not thrown if enumerating the directory encounters non-extant nodes</remarks>
		public static IEnumerable<DirectoryEntry> GetDirectoryContentsOrEmpty(string path,
				FollowSymbolicLinks followSymbolicLinks = FollowSymbolicLinks.Never)
			=> GetDirectoryContents(path, NonExtantDirectoryBehavior.ReturnEmpty, followSymbolicLinks);

		///<summary>Gets the contents of a directory or null for directory not found</summary>
		///<param name="path">path to enumerate</param>
		///<param name="followSymbolicLinks">Whether or not to follow symbolic links</param>
		///<exception cref="System.IO.IOException">An IO error occurred accessing path</exception>
		///<remarks>Exceptions are not thrown if enumerating the directory encounters non-extant nodes</remarks>
		public static IEnumerable<DirectoryEntry>? GetDirectoryContentsOrNull(string path,
				FollowSymbolicLinks followSymbolicLinks = FollowSymbolicLinks.Never)
			=> GetDirectoryContents(path, NonExtantDirectoryBehavior.ReturnNull, followSymbolicLinks);

		///<summary>Creates a hard link (new name) for a file</summary>
		///<param name="targetpath">The path to the existing file</param>
		///<param name="linkpath">The path to create the new link at</param>
		///<exception cref="System.IO.IOException">An IO error occurred</exception>
		public static void CreateHardLink(string targetpath, string linkpath)
		{
#if OSTYPE_UNIX
			var btargetpath = NameToByteArray(targetpath);
			var blinkpath = NameToByteArray(linkpath);
			IOException ex;
			while (IsEIntrSyscallReturnOrException(
				NativeMethods.link(btargetpath, blinkpath),
				linkpath, false, 0, true, out ex));
			if (ex is not null)
			{
				if (ex.HResult == IOErrors.NoSuchSystemCall || ex.HResult == IOErrors.SocketOperationNotSupported)
					ex = GetExceptionFromErrno(IOErrors.NoSuchSystemCall, targetpath, "File system does not support hard links.");
#if OS_LINUXX64
				else if (ex.HResult == IOErrors.PermissionDenied) {
					int n = blinkpath.Length;
					while (n-- > 0 && blinkpath[n] != '/')
						;
					// pathconf is busted for this call. glibc is refusing me permission to file bug
					if (n == -1) { blinkpath[0] = (byte)'.'; n = 1; }
					blinkpath[n] = 0;
					int fsresult;
					NativeMethods.statfsbuf64 statfs;
					do {
						fsresult = NativeMethods.statfs64(blinkpath, out statfs);
					} while (IsEIntrSyscallReturn(fsresult));
					if (fsresult >= 0 && statfs.f_type == NativeMethods.MSDOS_SUPER_MAGIC)
						ex = GetExceptionFromErrno(IOErrors.NoSuchSystemCall, targetpath, "File system does not support hard links.");
				}
#else
				else if (blinkpath.Length > 1) {
					// Call pathconf, hope it works
					int n = blinkpath.Length;
					while (n-- > 0 && blinkpath[n] != '/')
						;
					if (n == -1) { blinkpath[0] = (byte)'.'; n = 1; }
					blinkpath[n] = 0;
					long presult;
					do {
						presult = NativeMethods.pathconf(blinkpath, NativeMethods._PC_LINK_MAX);
					} while (presult == -1 && IsEIntrSyscallReturn(-1));
					if (presult == 1) {
						ex = GetExceptionFromErrno(IOErrors.NoSuchSystemCall, targetpath, "File system does not support hard links.");
					}
				}
#endif
				throw ex;
			}
#elif OS_WIN
			if (0 == NativeMethods.CreateHardLinkW(linkpath, targetpath, IntPtr.Zero)) {
				if (Marshal.GetLastWin32Error() == 1) throw GetExceptionFromErrno(1, targetpath, "File system does not support hard links.");
				throw GetExceptionFromLastError(linkpath, false, 0, true);
			}
#else
			throw null;
#endif
		}

		///<summary>Creates a symbolic link to a file</summary>
		///<param name="targetpath">The path to write to the symbolic link</param>
		///<param name="linkpath">The path to create the new link at</param>
		///<param name="targethint">The type of node the link is referring to</param>
		///<exception cref="System.IO.IOException">An IO error occurred</exception>
		///<exception cref="System.PlatformNotSupportedException">linkpath doesn't exist and targethint was neither File nor Directory and this platform uses explicit link types</exception>
		///<remarks>If targetpath doesn't exist and targethint is not provided, this call will fail on Windows.</remarks>
		public static void CreateSymbolicLink(string targetpath, string linkpath, FileType targethint = FileType.LinkTargetHintNotAvailable)
		{
#if OSTYPE_UNIX
			var btargetpath = NameToByteArray(targetpath);
			var blinkpath = NameToByteArray(linkpath);
			IOException ex;
			while (IsEIntrSyscallReturnOrException(
				NativeMethods.symlink(btargetpath, blinkpath),
				linkpath, false, 0, true, out ex));
			if (ex is not null) {
				if (ex.HResult == IOErrors.NoSuchSystemCall || ex.HResult == IOErrors.SocketOperationNotSupported)
					ex = GetExceptionFromErrno(IOErrors.NoSuchSystemCall, targetpath, "File system does not support symbolic links.");
#if OS_LINUXX64
				else if (ex.HResult == IOErrors.PermissionDenied)
#else
				else if (blinkpath.Length > 1)
#endif
				{
					int n = blinkpath.Length;
					while (n-- > 0 && blinkpath[n] != '/')
						;
					if (n == -1) { blinkpath[0] = (byte)'.'; n = 1; }
					blinkpath[n] = 0;
					long presult;
					do {
						presult = NativeMethods.pathconf(blinkpath, NativeMethods._PC_2_SYMLINKS);
					} while (presult == -1 && IsEIntrSyscallReturn(-1));
					if (presult == 0) {
						ex = GetExceptionFromErrno(IOErrors.NoSuchSystemCall, targetpath, "File system does not support symbolic links.");
					}
				}
				throw ex;
			}
#elif OS_WIN
			uint flags = 0;
			if (targethint != FileType.File && targethint != FileType.Directory) {
				var xreftargetpath = targetpath;
				if (!Path.IsPathRooted(xreftargetpath)) { // Resolve a copy of target path using symlink location as root
					var ldname = Path.GetDirectoryName(linkpath);
					if (string.IsNullOrEmpty(ldname)) ldname = DirectoryEntry.CurrentDirectoryName;
					xreftargetpath = Path.Combine(ldname, targetpath);
				}
				var node = new DirectoryEntry(xreftargetpath, FollowSymbolicLinks.Never);
				var hint = (node.FileType == FileType.SymbolicLink) ? node.LinkTargetHint : node.FileType;
				if (hint == FileType.Directory)
					flags = NativeMethods.SYMBOLIC_LINK_FLAG_DIRECTORY;
				else if (hint != FileType.File)
					throw new PlatformNotSupportedException("Windows can't handle symbolic links to file system nodes that don't exist.");
			}
			else if (targethint == FileType.Directory)
				flags = NativeMethods.SYMBOLIC_LINK_FLAG_DIRECTORY;
			if (0 == NativeMethods.CreateSymbolicLinkW(linkpath, targetpath, flags)) {
				var errno = (int)Marshal.GetLastWin32Error();
				if (errno == 1) throw GetExceptionFromErrno(errno, linkpath, "File system does not support symbolic links.");
				if (errno == 1314) {
					flags |= NativeMethods.SYMBOLIC_LINK_FLAG_ALLOW_UNPRIVILEGED_CREATE;
					if (0 != NativeMethods.CreateSymbolicLinkW(linkpath, targetpath, flags))
						return ;
					var errno2 = (int)Marshal.GetLastWin32Error();
					if (errno2 == 1314 || errno2 == 1 || errno2 == 0xA0)
						NativeMethods.SetLastError(errno); // Couldn't get a better error
				}
				throw GetExceptionFromLastError(linkpath, false, 0, true);
			}
#else
			throw null;
#endif
		}

		///<summary>Gets the link target text of a path</summary>
		///<param name="path">The path to the symbolic link to read</param>
		///<exception cref="System.IO.IOException">An IO error occurred</exception>
		public static string ReadLink(string path)
		{
#if OSTYPE_UNIX
			var blinkpath = NameToByteArray(path);
			var buflen = 512;
			long result;
			byte[] results;
			do {
				buflen <<= 1;
				results = new byte[buflen];
				IOException exception;
				do {
					result = NativeMethods.readlink(blinkpath, results, results.Length);
				} while (IsEIntrSyscallReturnOrException(result, path, false, 0, false, out exception));
				if (exception is object) throw exception;
				// Documentation suggests we should never go around this loop but ...
			} while (result == buflen);
			results[result] = 0;
			return ByteArrayToName(results);
#elif OS_WIN
			IntPtr handle = IOErrors.InvalidFileHandle;
			try {
				handle = NativeMethods.CreateFileW(path, NativeMethods.FILE_READ_ATTRIBUTES,
						NativeMethods.FILE_SHARE_ALL, IntPtr.Zero, NativeMethods.OPEN_EXISTING,
						NativeMethods.FILE_FLAG_BACKUP_SEMANTICS | NativeMethods.FILE_FLAG_OPEN_REPARSE_POINT, IntPtr.Zero);
				if (handle == IOErrors.InvalidFileHandle) {
					throw GetExceptionFromLastError(path, false, 0, false);
				}
				uint buflen = 1024;
				uint hdrsize = (uint)Marshal.SizeOf<NativeMethods.REPARSE_DATA_BUFFER_SYMLINK>();
				for(;;) {
					var results = new byte[buflen];
					if (0 == NativeMethods.DeviceIoControl(handle, NativeMethods.FSCTL_GET_REPARSE_POINT,
							IntPtr.Zero, 0, results, buflen, out uint returned, IntPtr.Zero)) {
						var errno = (int)Marshal.GetLastWin32Error();
						if (errno == IOErrors.ERROR_MORE_DATA) {
							buflen <<= 1;
							continue;
						}
						throw GetExceptionFromLastError(path, false, 0, false);
					}
					GCHandle gch;
					try {
						gch = GCHandle.Alloc(results, GCHandleType.Pinned);
						var symdata = Marshal.PtrToStructure<NativeMethods.REPARSE_DATA_BUFFER_SYMLINK>(gch.AddrOfPinnedObject());
						if (symdata.ReparseTag != 0xA000000C)
							throw new NotImplementedException("This is a reparse point, not a symbolic link.");
						uint bufwanted = hdrsize + (uint)((symdata.PrintNameOffset + symdata.PrintNameLength) << 1);
						if (bufwanted <= buflen)
							return Marshal.PtrToStringUni(gch.AddrOfPinnedObject() + (int)hdrsize + (int)(symdata.PrintNameOffset << 1),
								symdata.PrintNameLength >> 1);
						buflen = bufwanted;
					} finally {
						gch.Free();
					}
				}
			} finally {
				if (handle != IOErrors.InvalidFileHandle) NativeMethods.CloseHandle(handle);
			}
#else
			throw null;
#endif
		}

		///<summary>Removes a file from the disk</summary>
		///<param name="path">The path to the file to be removed</param>
		///<returns>true if the file was removed, false if path already didn't exist</returns>
		///<exception cref="System.IO.IOException">An IO error occurred</exception>
		public static bool RemoveFile(string path)
		{
#if OSTYPE_UNIX
			var bpath = NameToByteArray(path);
			int cresult;
			do {
				cresult = NativeMethods.unlink(bpath);
			} while (IsEIntrSyscallReturn(cresult));
			if (cresult < 0) {
				var exception = GetExceptionFromLastError(path, true, IOErrors.IsNotADirectory, true);
				if (exception is null) return false;
				throw exception;
			}
			return true;
#elif OS_WIN
			IntPtr handle = IOErrors.InvalidFileHandle;
			try {
				handle = NativeMethods.CreateFileW(path, NativeMethods.FILE_READ_ATTRIBUTES | NativeMethods.DELETE,
						NativeMethods.FILE_SHARE_READ | NativeMethods.FILE_SHARE_WRITE | NativeMethods.FILE_SHARE_DELETE,
						IntPtr.Zero, NativeMethods.OPEN_EXISTING, NativeMethods.FILE_FLAG_BACKUP_SEMANTICS, IOErrors.InvalidFileHandle);
				if (handle == IOErrors.InvalidFileHandle)
				{
					var exception = GetExceptionFromLastError(path, true, IOErrors.IsADirectory, true);
					if (exception is null) return false;
					throw exception;
				}
        int status = NativeMethods.NtQueryInformationFile(handle, out NativeMethods.IO_STATUS_BLOCK sb,
						out NativeMethods.FILE_BASIC_INFORMATION bi,
            Marshal.SizeOf<NativeMethods.FILE_BASIC_INFORMATION>(), NativeMethods.FileBasicInformation);
        if (status != 0) throw GetExceptionFromNtStatus(status, path, false, 0, false);
				if ((bi.FileAttributes & (NativeMethods.FILE_ATTRIBUTE_DIRECTORY | NativeMethods.FILE_ATTRIBUTE_REPARSE_POINT))
						== NativeMethods.FILE_ATTRIBUTE_DIRECTORY) {
					NativeMethods.SetLastError(IOErrors.IsADirectory & 0xFFFF);
					throw GetExceptionFromLastError(path, false, 0, true);
				}
				uint deletepending = 1;
				if (0 == NativeMethods.SetFileInformationByHandle(handle, NativeMethods.FileDispositionInfo, ref deletepending, 4))
					throw GetExceptionFromLastError(path, false, 0, true);
				return true;
			} finally {
				if (handle != IOErrors.InvalidFileHandle) NativeMethods.CloseHandle(handle);
			}
#else
			throw null;
#endif
		}

		///<summary>Removes a file from the disk</summary>
		///<param name="path">The path to the file to be removed</param>
		///<param name="recurse">Whether or not to remove the directory and all its contents</param>
		///<returns>true if the file was removed, false if path already didn't exist</returns>
		///<exception cref="System.ArgumentException">Removial of a trivial path of a root directory was requested</exception>
		///<exception cref="System.InvalidOperationException">A system constraint was found to be violated while descending the directory tree</exception>
		///<exception cref="System.NotImplementedException">Tried to remove a mountpoint on unix systems</exception>
		///<exception cref="System.IO.IOException">An IO error occurred</exception>
		///<remarks>This routine will not descend symbolc links, and uses an atomic check to prevent this from happening</remarks>
		public static bool RemoveDirectory(string path, bool recurse)
#if OSTYPE_UNIX
		{
				var name = NameToByteArray(path);
				var epb = new ErrorPathBuilder();
				bool pushed = false;
				epb.Push(name, 0, name.Length, ref pushed);
				return RemoveDirectory(name, 0, recurse ? 1 : 0, epb);
		}

		private static bool RemoveDirectory(byte[] path, long parentdevice, int flags, ErrorPathBuilder errorpath)
		{
			if (path.Length == 1 && path[0] == '/') throw new ArgumentException("Not removing root directory");
			while (0 > NativeMethods.rmdir(path))
			{
				int errno = Marshal.GetLastWin32Error();
				if (errno == IOErrors.Interrupted) continue;
				if ((flags & 2) != 0 && errno == IOErrors.IsNotADirectory)
				{
					if (0 < NativeMethods.unlink(path)) {
						if (Marshal.GetLastWin32Error() == IOErrors.Interrupted) continue;
						var exception = GetExceptionFromLastError(errorpath.ToString, true, IOErrors.FileNotFound, true);
						if (exception is null) return false;
						throw exception;
					}
				} else if ((flags & 1) == 0 || errno != IOErrors.DirectoryNotEmpty) {
					var exception = GetExceptionFromLastError(errorpath.ToString, errno != IOErrors.IsNotADirectory, IOErrors.FileNotFound, true);
					if (exception is null) return false;
					throw exception;
				}
				bool owns_handle = false;
				IntPtr dir = IntPtr.Zero;
				IntPtr handle = IntPtr.Zero;
				try {
					try {} finally {
						do {
							handle = NativeMethods.open(path, 0);
						} while (handle == IOErrors.InvalidFileHandle && Marshal.GetLastWin32Error() == IOErrors.Interrupted);
						owns_handle = true; // unsplittable
					}
					if (handle != IOErrors.InvalidFileHandle) {
						var hinfo = new FileSystemNode(new SafeFileHandle(handle, false));
						var dinfo = new ByteArrayDirectoryEntry(path, errorpath);
						// Assert it's the same node.
						if (dinfo.FileType == FileType.DoesNotExist) return true; // It's already gone.
						if (dinfo.DeviceNumber != hinfo.DeviceNumber || dinfo.InodeNumber != hinfo.InodeNumber)
							return false; // It's already been deleted and recreated. Pretend the open() was a little faster and we never opened it.
							              // Note that if we're inside a recursive call, the next pass will get it.
						long devicenumber = hinfo.DeviceNumber;
						if ((flags & 2) != 0 && parentdevice != devicenumber)
							throw new NotImplementedException(errorpath.ToString() + ": Tried to remove a mountpoint");
						string pathbase = "/proc/self/fd/" + handle.ToInt32().ToString(System.Globalization.CultureInfo.InvariantCulture) + "/";
						var newpathbase = pathbase.Length;
						var newpath = new byte[newpathbase + 256];
						Encoding.ASCII.GetBytes(pathbase, 0, newpathbase, newpath, 0);
						try {} finally {
							dir = NativeMethods.fdopendir(handle);
							if (dir != IntPtr.Zero) owns_handle = false;
						}
						bool didsomething;
						do {
							NativeMethods.rewinddir(dir);
							didsomething = false;
							for (;;) {
#if OS_LINUXX64
								var readentry = new NativeMethods.dirent64();
								readentry.d_name = new byte[256];
								int result = NativeMethods.readdir64_r(dir, ref readentry, out IntPtr z);
								if (result < 0)
									throw GetExceptionFromLastError(errorpath.ToString, false, 0, true);
								if (z == IntPtr.Zero) break;
								var namelen = readentry.d_reclen;
#else
								IntPtr result = NativeMethods.readdir(dir);
								if (result == IntPtr.Zero) {
									var errno2 = Marshal.GetLastWin32Error();
									if (errno2 == 0) break;
									var ci2 = new System.ComponentModel.Win32Exception();
									throw new IOException(errorpath.ToString() + ": " + ci2.Message, errno2);
								}
								var readentry = Marshal.PtrToStructure<NativeMethods.dirent>(result);
								var namelen = readentry.d_namelen;
#endif
#if OS_LINUXX64
								Array.Copy(readentry.d_name, 0, newpath, newpathbase, Math.Min((int)namelen, 256));
#else
								Marshal.Copy(result + NativeMethods.dirent_d_name_offset, newpath, newpathbase, Math.Min((int)namelen, 256));
#endif
								if (namelen < 256) newpath[newpathbase + namelen] = 0; // We might not have copied the null
								// Find trailing null
								for (namelen = 0; newpath[newpathbase + namelen] != 0; namelen++)
									if (namelen == 255)
										throw new InvalidOperationException("File name exceeds 255 bytes"); // Not supposed to be possible!
									else if (newpath[newpathbase + namelen] == '/')
										throw new InvalidOperationException("A file with / in the name was encountered; check NFS drivers");
								if (namelen == 0) throw new InvalidOperationException("File with the null name encountered");
								if (namelen == 1 && newpath[newpathbase] == '.') continue;
								if (namelen == 2 && newpath[newpathbase] == '.' && newpath[newpathbase + 1] == '.') continue;
								newpath[newpathbase + namelen] = 0;
								bool pushed = false;
								try {
									errorpath.Push(newpath, newpathbase, namelen, ref pushed);
									didsomething |= RemoveDirectory(newpath, devicenumber, 3, errorpath);
								} finally {
									errorpath.Pop(pushed);
								}
							}
						} while (didsomething);
					}
				} finally {
					if (dir != IntPtr.Zero) NativeMethods.closedir(dir);
#if OS_MACOSX64
					// This is in very bad taste of Apple
					if (owns_handle)
						while (NativeMethods.close(handle) < 0 && Marshal.GetLastWin32Error() == IOErrors.Interrupted)
							;
#else
					if (owns_handle) NativeMethods.close(handle);
#endif
				}
				// Go back around and try to remove it again. If it's still not empty, somebody's trying to fill it. Be stubborn.
			}
			return true;
		}

		private class ByteArrayDirectoryEntry : FileSystemNode
		{
			internal ByteArrayDirectoryEntry(byte[] bytepath, ErrorPathBuilder builder) : base(null)
			{
#if OS_LINUXX64
				var statbuf = new NativeMethods.statbuf64();
        var cresult = NativeMethods.__lxstat64(NativeMethods.statbuf_version, bytepath, out statbuf);
#else
				var statbuf = new NativeMethods.statbuf();
				var cresult = NativeMethods.lstat(bytepath, out statbuf);
#endif
				if (cresult < 0) {
					var exception = GetExceptionFromLastError(builder.ToString, true, 0, true);
					if (exception is not null) throw exception;
					Clear();
				} else {
					FillStatResult(ref statbuf);
				}
			}
		}
#elif OS_WIN
		{
			if (path == "") path = "."; // It's gonna fail on trying to delete .
			if (path == "\\" || path.Length < 4 && path.Length > 1 && path[1] == ':' && (path.Length == 2 || path[2] == '\\' || path[2] == '/')
					&& (path[0] >= 'A' && path[0] <= 'Z' || path[0] >= 'a' || path[0] <= 'z'))
				throw new ArgumentException("Not removing root directory"); // Root of a drive
			int bptr = path.Length - 1;
			while (--bptr >= 0)
				if (path[bptr] == '\\' || path[bptr] == '/')
					break;
			if (path.Length > 1 && (path[0] == '\\' || path[0] == '/') && (path[1] == '\\' || path[1] == '/')) {
				if (bptr <= 2) throw new ArgumentException("Not removing root directory"); // Server root
				for (int i = 3; path[i] == '\\' || path[i] == '/'; i++)
					if (i + 1 == bptr)
						throw new ArgumentException("Not removing root directory"); // Network share root
			}
			// The root checks are intended to prevent accident, not malice. \.\ is another root that gets past the check. :(
			IntPtr parent = IOErrors.InvalidFileHandle;
			IntPtr nameptr = IntPtr.Zero;
			IntPtr uptr = IntPtr.Zero;
			IntPtr dotdot = IntPtr.Zero;
			IntPtr backslash = IntPtr.Zero;
			int namelen;
			try {
				string parentname;
				string name;
				if (bptr < 0) {
					parentname = ".";
					namelen = path.Length;
					name = path;
				} else {
					if (bptr == 0)
						parentname = "\\";
					else if (bptr == 2 && path[1] == ':' && (path[0] >= 'A' && path[0] <= 'Z' || path[0] >= 'a' && path[0] <= 'z'))
						parentname = path.Substring(0, 3);
					else
						parentname = path.Substring(0, bptr);
					namelen = path.Length - (bptr + 1);
					name = path.Substring(bptr + 1);
				}
				nameptr = Marshal.StringToHGlobalUni(name);
				if (path[path.Length - 1] == '\\' || path[path.Length - 1] == '/') --namelen; // For consistency with *n?x
				uptr = Marshal.AllocHGlobal(Marshal.SizeOf<NativeMethods.UNICODE_STRING>());
				parent = NativeMethods.CreateFileW(parentname, NativeMethods.FILE_TRAVERSE,
						NativeMethods.FILE_SHARE_READ | NativeMethods.FILE_SHARE_WRITE | NativeMethods.FILE_SHARE_DELETE, IntPtr.Zero,
						NativeMethods.OPEN_EXISTING, NativeMethods.FILE_FLAG_BACKUP_SEMANTICS, IOErrors.InvalidFileHandle);
				if (parent == IOErrors.InvalidFileHandle) {
					var exception = GetExceptionFromLastError(parentname, true, IOErrors.IsNotADirectory, true);
					if (exception is null) return false;
					throw exception;
				}
				return RemoveDirectory(parent, uptr, nameptr, namelen, recurse ? 1 : 0, new ErrorPathBuilder(parentname));
			} finally {
				if (nameptr != IntPtr.Zero) Marshal.FreeHGlobal(nameptr);
				if (uptr != IntPtr.Zero) Marshal.FreeHGlobal(uptr);
				if (parent != IOErrors.InvalidFileHandle) NativeMethods.CloseHandle(parent);
			}
		}

		private static bool RemoveDirectory(IntPtr parent, IntPtr uptr, IntPtr nameptr, int namelen,
				int flags, ErrorPathBuilder errorpath)
		{
			if (namelen > 32767) return false; // It doesn't exist.
			IntPtr directory = IOErrors.InvalidFileHandle;
			IntPtr buffer = IntPtr.Zero;
			int step = Marshal.SizeOf<NativeMethods.FILE_DIRECTORY_INFORMATION>();
			bool pushed = false;
			try {
				errorpath.Push(nameptr, namelen, ref pushed);
				NativeMethods.IO_STATUS_BLOCK io;
				NativeMethods.UNICODE_STRING ustr;
				NativeMethods.OBJECT_ATTRIBUTES attributes;
				ustr.Length = unchecked((ushort)(namelen << 1));
				ustr.MaximumLength = ustr.Length;
				ustr.Buffer = nameptr;
				Marshal.StructureToPtr(ustr, uptr, false);
				attributes.Length = (uint)Marshal.SizeOf<NativeMethods.OBJECT_ATTRIBUTES>();
				attributes.RootDirectory = parent;
				attributes.ObjectName = uptr;
				attributes.Attributes = ((flags & 2) == 0) ? NativeMethods.OBJ_CASE_INSENSITIVE : 0;
				attributes.SecurityDescriptor = IntPtr.Zero;
				attributes.SecurityQualityOfService = IntPtr.Zero;
				int status = NativeMethods.NtOpenFile(ref directory,
						(((flags & 5) == 1) ? NativeMethods.FILE_LIST_DIRECTORY | NativeMethods.FILE_TRAVERSE : 0) |
						(((flags & 2) == 0) ? NativeMethods.FILE_READ_ATTRIBUTES : 0) | NativeMethods.SYNCHRONIZE | NativeMethods.DELETE,
						ref attributes, out io,
						NativeMethods.FILE_SHARE_READ | NativeMethods.FILE_SHARE_WRITE | NativeMethods.FILE_SHARE_DELETE,
						(((flags & 4) == 0) ? NativeMethods.FILE_DIRECTORY_FILE : 0) | NativeMethods.FILE_SYNCHRONOUS_IO_NONALERT
						| NativeMethods.FILE_OPEN_FOR_BACKUP_INTENT | NativeMethods.FILE_OPEN_REPARSE_POINT);
				if (unchecked(status & (int)0x80000000) != 0) {
					var exception = GetExceptionFromNtStatus(status, errorpath.ToString, true, IOErrors.IsADirectory, true);
					if (exception is null) return false;
					throw exception;
				}
				if (status == NativeMethods.STATUS_PENDING)
					throw new InvalidOperationException("STATUS_PENDING was returned from synchronous open");
				if (status == NativeMethods.STATUS_REPARSE)
					throw new InvalidOperationException("STATUS_REPARSE was returned on requesting to open a reparse point");
				if ((flags & 2) == 0) {
					status = NativeMethods.NtQueryInformationFile(directory, out NativeMethods.IO_STATUS_BLOCK sb,
							out NativeMethods.FILE_BASIC_INFORMATION bi,
	            Marshal.SizeOf<NativeMethods.FILE_BASIC_INFORMATION>(), NativeMethods.FileBasicInformation);
					if (status != 0)
						throw GetExceptionFromNtStatus(status, errorpath.ToString, false, 0, false);
					if ((bi.FileAttributes & (NativeMethods.FILE_ATTRIBUTE_DIRECTORY | NativeMethods.FILE_ATTRIBUTE_REPARSE_POINT))
							!= NativeMethods.FILE_ATTRIBUTE_DIRECTORY) {
						NativeMethods.SetLastError(IOErrors.IsNotADirectory & 0xFFFF);
						throw GetExceptionFromLastError(errorpath.ToString, false, 0, true);
					}
				}
				for (;;) {
					uint deletepending = 1;
					if (0 != NativeMethods.SetFileInformationByHandle(directory, NativeMethods.FileDispositionInfo, ref deletepending, 4))
						return true;
					int errno = unchecked(((int)0x80070000) | Marshal.GetLastWin32Error());
					if ((flags & 1) == 0 || errno != IOErrors.DirectoryNotEmpty) {
						throw GetExceptionFromLastError(errorpath.ToString, false, 0, true);
					}
					try {} finally { if (buffer == IntPtr.Zero) buffer = Marshal.AllocHGlobal(65536); }
					bool didsomething;
					do {
						didsomething = false;
						bool rewind = true;
						for(;;) {
							status = NativeMethods.NtQueryDirectoryFile(directory, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, out io,
									buffer, 65536, NativeMethods.FileDirectoryInformation, false, IntPtr.Zero, rewind);
							if (status == NativeMethods.STATUS_NO_MORE_FILES) break;
							if (status == NativeMethods.STATUS_PENDING)
								throw new InvalidOperationException("STATUS_PENDING was returned from synchronous directory read");
							if (status == NativeMethods.STATUS_INFO_LENGTH_MISMATCH)
								throw new InvalidOperationException("STATUS_INFO_LENGTH_MISMATCH mismatch was returned from directory read");
							if (status == NativeMethods.STATUS_BUFFER_OVERFLOW)
								throw new InvalidOperationException("STATUS_BUFFER_OVERFLOW was returned from synchronous directory read but our buffer is 64k");
							if (unchecked(status & (int)0x80000000) != 0)
								throw GetExceptionFromNtStatus(status, errorpath.ToString, false, 0, true);
							if (io.Information == UIntPtr.Zero)
								throw new InvalidOperationException("No bytes were returned from synchronous directory read but our buffer is 64k and status doesn't indicate an error and is not STATUS_NO_MORE_FILES");
							rewind = false;
							IntPtr nextentry;
							IntPtr nextentryadjust = buffer;
							do {
								nextentry = nextentryadjust;
								var thisentry = Marshal.PtrToStructure<NativeMethods.FILE_DIRECTORY_INFORMATION>(nextentry);
								if (thisentry.FileNameLength >= 65536) throw new InvalidOperationException("Maximum OS file name length exceeded");
								int thisnamelen = (int)(thisentry.FileNameLength >> 1);
								if (thisnamelen == 0) throw new InvalidOperationException("File with the null name encountered");
								if (IsAnyChar(nextentry + step, thisnamelen, '\\')) throw new InvalidOperationException("File with a \\ in the name encountered");
								if (!((thisnamelen == 1 || thisnamelen == 2) && AreAllChar(nextentry + step, thisnamelen, '.'))) {
									didsomething |= RemoveDirectory(directory, uptr, nextentry + step, thisnamelen,
										((thisentry.FileAttributes & (NativeMethods.FILE_ATTRIBUTE_DIRECTORY | NativeMethods.FILE_ATTRIBUTE_REPARSE_POINT))
												== NativeMethods.FILE_ATTRIBUTE_DIRECTORY)
										? 3 : 6, errorpath);
								}
								nextentryadjust = nextentry + (int)thisentry.NextEntryOffset;
							} while (nextentry != nextentryadjust);
						}
					} while (didsomething);
				}
			} finally {
				if (directory != IOErrors.InvalidFileHandle) NativeMethods.CloseHandle(directory);
				if (buffer != IntPtr.Zero) Marshal.FreeHGlobal(buffer);
				errorpath.Pop(pushed);
			}
		}
#else
			=> throw null;
#endif

		///<summary>Renames a file nearly atomically, but will not cross filesystems</summary>
		///<param name="oldname">The original file name</param>
		///<param name="newname">The target file name</param>
		///<exception cref="System.IO.IOException">An IO error occurred</exception>
		///<remarks>Even with a journaling filesystem, you could find both oldname and newname referring to the same file, or if the kernel were to fail, newname could be gone while oldname refers to the old name. Any other transactional failure is a bug in the kernel or a bug in the disk.</remarks>
		public static void RenameReplace(string oldname, string newname)
		{
#if OSTYPE_UNIX
			var boldname = NameToByteArray(oldname);
			var bnewname = NameToByteArray(newname);
			IOException ex;
			while (IsEIntrSyscallReturnOrException(NativeMethods.rename(boldname, bnewname), oldname, false, 0, true, out ex));
			if (ex is not null) throw ex;
#elif OS_WIN
			if (0 == NativeMethods.MoveFileEx(oldname, newname, NativeMethods.MOVEFILE_REPLACE_EXISTING))
				throw GetExceptionFromLastError(oldname, false, 0, true);
#else
			throw null;
#endif
		}

		// Platform properties
#if OSTYPE_UNIX
#if OS_LINUXX64
		// Waiting for xstat system call to appear. Glibc exports __xstat which does something else
		public static bool OSSupportsCreationTime => false;
#elif OS_MACOSX64
		public static bool OSSupportsCreationTime => true;
#else
		SYNTAX ERROR
#endif
		public static bool OSSupportsHardLinks => true;
		public static bool OSSupportsSymbolicLinks => true;
#elif OS_WIN
		public static bool OSSupportsCreationTime => true;
		public static bool OSSupportsHardLinks => true;
		public static bool OSSupportsSymbolicLinks => true;
#else
		///<summary>Returns whether or not the OS supports the CreationTime property</summary>
		public static bool OSSupportsCreationTime => throw null;

		///<summary>Returns whether or not the OS supports creating hard links</summary>
		public static bool OSSupportsHardLinks => throw null;

		///<summary>Returns whether or not the OS supports creating symbolic links</summary>
		public static bool OSSupportsSymbolicLinks => throw null;
#endif

		///<summary>Returns whether or not a file exists</summary>
		///<param name="path">the path to the file to check</param>
		///<remarks>can return false if the user does not have enough permissions on the path containing the file</remarks>
		///<exception cref="System.IO.IOException">An IO error occurred</exception>
		public static bool FileExists(string path)
			=> new DirectoryEntry(path, FollowSymbolicLinks.Always).FileType == FileType.File;

		///<summary>Returns whether or not a directory exists</summary>
		///<param name="path">the path to the directory to check</param>
		///<remarks>can return false if the user does not have enough permissions on the path containing the directory</remarks>
		///<exception cref="System.IO.IOException">An IO error occurred</exception>
		public static bool DirectoryExists(string path)
			=> new DirectoryEntry(path, FollowSymbolicLinks.Always).FileType == FileType.Directory;

		///<summary>Returns whether or not a file name is in use</summary>
		///<param name="path">the path to check the existence of</param>
		///<remarks>can return false if the user does not have enough permissions on the path containing the file system node</remarks>
		///<exception cref="System.IO.IOException">An IO error occurred</exception>
		public static bool PathExists(string path)
			=> new DirectoryEntry(path, FollowSymbolicLinks.Never).FileType != FileType.DoesNotExist;

		// List of DirectoryEntiry; this class actually exists so that callers don't take a dependency on downcasting
		// IEnumerable<DirectoryEntry> to List<DirectoryEntry>.  I've switched back and forth between yield return and
		// list more than once.
		private class DirectoryEntryList : IEnumerable<DirectoryEntry>, IEnumerator<DirectoryEntry> {
			private int clist;
			private int alist;
			private DirectoryEntry[] list;
			private int offset;

			internal DirectoryEntryList()
			{
				//clist = 0;
				alist = 16;
				list = new DirectoryEntry[16];
				offset = -2;
			}

			internal void Add(DirectoryEntry e)
			{
				if (clist == alist) {
					if (alist == int.MaxValue)
						throw new NotSupportedException("Directory contains more than 0x7FFFFFFF files.");
					alist <<= 1;
					if (alist <= 0)
						alist = int.MaxValue;
					Array.Resize(ref list, alist);
				}
				list[clist++] = e;
			}

			public IEnumerator<DirectoryEntry> GetEnumerator()
				=> System.Threading.Interlocked.CompareExchange(ref offset, -1, -2) == -2 ? this : new Enumerator(this);

			bool System.Collections.IEnumerator.MoveNext()
			{
				if (offset + 1 >= clist) return false;
				++offset;
				return true;
			}

			DirectoryEntry IEnumerator<DirectoryEntry>.Current => list[offset];

			object System.Collections.IEnumerator.Current => list[offset];

			void System.Collections.IEnumerator.Reset() => offset = -1;

			void IDisposable.Dispose() { offset = -2; }

			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

			// Optimized implementation knows DirectoryEntryList does not change after GetEnumerator() is first called.
			private class Enumerator : IEnumerator<DirectoryEntry>
			{
				private DirectoryEntry[] list;
				private int clist;
				private int offset;

				internal Enumerator(DirectoryEntryList l)
				{
					list = l.list;
					clist = l.clist;
					offset = -1;
				}

				bool System.Collections.IEnumerator.MoveNext()
				{
					if (offset + 1 >= clist) return false;
					++offset;
					return true;
				}

				DirectoryEntry IEnumerator<DirectoryEntry>.Current => list[offset];

				object System.Collections.IEnumerator.Current => list[offset];

				void System.Collections.IEnumerator.Reset() => offset = -1;

				void IDisposable.Dispose() {}
			}
		}
	}
}
