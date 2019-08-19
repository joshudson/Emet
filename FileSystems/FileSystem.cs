/* vi:ts=2
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Encoding = System.Text.Encoding;

namespace Emet.FileSystems {
	public static class FileSystem {
		public enum NonExtantDirectoryBehavior : byte {
			Throw = 0,
			ReturnEmpty = 1,
			ReturnNull = 2,
		}

		public enum FollowSymbolicLinks : byte {
			///<summary>Never follow symbolic links</summary>
			Never = 0,
			///<summary>Always follow symbolic links</summary>
			///<remarks>If the link is broken the DirectoryEntry object might not be fully populated,
			///but FileType will be SymbolicLink</remarks>
			Always = 1,
			///<summary>Follow symbolic links if target is directory</summary>
			///<remarks>If the link is broken the DirectoryEntry object might not be fully populated,
			///but FileType will be SymbolicLink and LinkTargetHint will not be Directory.</remarks>
			IfNotDirectory = 2,
		}

		///<summary>Gets the contents of a directory</summary>
		///<param name="path">path to enumerate</param>
		///<param name="nonExtantDirectoryBehavior">What to do if the directory doesn't exist</param>
		///<param name="followSymbolicLinks">Whether or not to follow symbolic links</exception>
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
					var hresult = unchecked((int)0x80070000 | (int)Marshal.GetLastWin32Error());
					if (hresult == IOErrors.FileNotFound)
						return Enumerable.Empty<DirectoryEntry>();
					if (DirectoryEntry.IsPassError(hresult)) {
						switch (nonExtantDirectoryBehavior)
						{
							case NonExtantDirectoryBehavior.ReturnEmpty:
								return Enumerable.Empty<DirectoryEntry>();
							case NonExtantDirectoryBehavior.ReturnNull:
								return null;
							//case NonExtantDirectoryBehavior.Throw:
							default:
								throw new DirectoryNotFoundException();
						}
					}
					var ci = new System.ComponentModel.Win32Exception();
					throw new IOException(ci.Message, hresult);
				}
				// Windows refuses to document that deleting the node from FindNextFile is safe.
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
					var errno = Marshal.GetLastWin32Error();
					if (DirectoryEntry.IsPassError(errno)) {
						switch (nonExtantDirectoryBehavior)
						{
							case NonExtantDirectoryBehavior.ReturnEmpty:
								return Enumerable.Empty<DirectoryEntry>();
							case NonExtantDirectoryBehavior.ReturnNull:
								return null;
							//case NonExtantDirectoryBehavior.Throw:
							default:
								throw new DirectoryNotFoundException();
						}
					}
					var ciz = new System.ComponentModel.Win32Exception();
					throw new IOException(ciz.Message, errno);
				}
				var entries = new List<DirectoryEntry>();
#if OS_LINUXX64
				var readentry = new NativeMethods.dirent64();
				readentry.d_name = new byte[256];
#endif
				for (;;) {
#if OS_LINUXX64
					int result = NativeMethods.readdir64_r(opendirhandle, ref readentry, out IntPtr z);
					if (result > 0) {
						var errno = Marshal.GetLastWin32Error();
						var ci = new System.ComponentModel.Win32Exception();
						throw new IOException(ci.Message, errno);
					}
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
						var ci = new System.ComponentModel.Win32Exception();
						throw new IOException(ci.Message, errno);
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

		///<summary>Creates a hard link (new name) for a file</summary>
		///<param name="targetpath">The path to the existing file</param>
		///<param name="linkpath">The path to create the new link at</param>
		///<exception cref="System.IO.IOException">An IO error occurred</exception>
		public static void CreateHardLink(string targetpath, string linkpath)
		{
#if OSTYPE_UNIX
			var btargetpath = NameToByteArray(targetpath);
			var blinkpath = NameToByteArray(linkpath);
			if (NativeMethods.link(btargetpath, blinkpath) != 0) {
				var errno = Marshal.GetLastWin32Error();
				var ci = new System.ComponentModel.Win32Exception();
				throw new IOException(ci.Message, errno);
			}
#elif OS_WIN
			if (0 == NativeMethods.CreateHardLinkW(linkpath, targetpath, IntPtr.Zero)) {
				var errno = (int)Marshal.GetLastWin32Error();
				var ci = new System.ComponentModel.Win32Exception();
				throw new IOException(ci.Message, unchecked((int)0x80070000 | errno));
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
			if (NativeMethods.symlink(btargetpath, blinkpath) != 0) {
				var errno = Marshal.GetLastWin32Error();
				var ci = new System.ComponentModel.Win32Exception();
				throw new IOException(ci.Message, errno);
			}
#elif OS_WIN
			uint flags = 0;
			if (targethint != FileType.File && targethint != FileType.Directory) {
				if (!Path.IsPathRooted(targetpath)) {
					var ldname = Path.GetDirectoryName(linkpath);
					if (string.IsNullOrEmpty(ldname)) ldname = DirectoryEntry.CurrentDirectoryName;
					targetpath = Path.Combine(ldname, targetpath);
				}
				var node = new DirectoryEntry(targetpath, FollowSymbolicLinks.Never);
				var hint = (node.FileType == FileType.SymbolicLink) ? node.LinkTargetHint : node.FileType;
				if (hint == FileType.Directory)
					flags = NativeMethods.SYMBOLIC_LINK_FLAG_DIRECTORY;
				else if (hint != FileType.File)
					throw new PlatformNotSupportedException("Windows can't handle symbolic links to file system nodes that don't exist.");
			}
			if (targethint == FileType.Directory)
				flags = NativeMethods.SYMBOLIC_LINK_FLAG_DIRECTORY;
			if (0 == NativeMethods.CreateSymbolicLinkW(linkpath, targetpath, flags)) {
				var errno = (int)Marshal.GetLastWin32Error();
				if (errno == 1314) {
					flags |= NativeMethods.SYMBOLIC_LINK_FLAG_ALLOW_UNPRIVILEGED_CREATE;
					if (0 != NativeMethods.CreateSymbolicLinkW(linkpath, targetpath, flags))
						return ;
					var errno2 = (int)Marshal.GetLastWin32Error();
					if (errno2 != 1314 && errno2 != 1 && errno2 != 0xA0)
						errno = errno2; // Try to get a better error
				}
				var ci = new System.ComponentModel.Win32Exception();
				throw new IOException(ci.Message, unchecked((int)0x80070000 | errno));
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
				result = NativeMethods.readlink(blinkpath, results, results.Length);
				if (result < 0) {
					var errno = (int)Marshal.GetLastWin32Error();
					var ci = new System.ComponentModel.Win32Exception();
					throw new IOException(ci.Message, errno);
				}
				// Documentation suggests we should never go around this loop but ...
			} while (result == buflen);
			results[result] = 0;
			return ByteArrayToName(results);
#elif OS_WIN
			IntPtr handle = NativeMethods.INVALID_HANDLE_VALUE;
			try {
				handle = NativeMethods.CreateFileW(path, NativeMethods.FILE_READ_ATTRIBUTES,
						NativeMethods.FILE_SHARE_ALL, IntPtr.Zero, NativeMethods.OPEN_EXISTING,
						NativeMethods.FILE_FLAG_BACKUP_SEMANTICS | NativeMethods.FILE_FLAG_OPEN_REPARSE_POINT, IntPtr.Zero);
				if (handle == NativeMethods.INVALID_HANDLE_VALUE) {
					var errno = Marshal.GetLastWin32Error();
					var ci = new System.ComponentModel.Win32Exception();
					throw new IOException(ci.Message, errno);
				}
				uint buflen = 1024;
				uint hdrsize = (uint)Marshal.SizeOf<NativeMethods.REPARSE_DATA_BUFFER_SYMLINK>();
				for(;;) {
					var results = new byte[buflen];
					if (0 == NativeMethods.DeviceIoControl(handle, NativeMethods.FSCTL_GET_REPARSE_POINT,
							IntPtr.Zero, 0, results, buflen, out uint returned, IntPtr.Zero)) {
						var errno = (int)Marshal.GetLastWin32Error();
						if (errno == IOErrors.ERROR_MORE_DATA) continue;
						var ci = new System.ComponentModel.Win32Exception();
						throw new IOException(ci.Message, unchecked((int)0x80070000 | errno));
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
				if (handle != NativeMethods.INVALID_HANDLE_VALUE) NativeMethods.CloseHandle(handle);
			}
#else
			throw null;
#endif
		}

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
			if (NativeMethods.rename(boldname, bnewname) != 0)
			{
				var errno = Marshal.GetLastWin32Error();
				var ci = new System.ComponentModel.Win32Exception();
				throw new IOException(ci.Message, errno);
			}
#elif OS_WIN
			if (0 == NativeMethods.MoveFileEx(oldname, newname, NativeMethods.MOVEFILE_REPLACE_EXISTING)) {
				var errno = unchecked((int)0x80070000 | Marshal.GetLastWin32Error());
				var ci = new System.ComponentModel.Win32Exception();
				throw new IOException(ci.Message, errno);
			}
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

#if OS_WIN
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
		internal static string ByteArrayToName(byte[] namebytes)
		{
			int i;
			for (i = 0; i < namebytes.Length; i++)
				if (namebytes[i] == 0) break;
			return Encoding.UTF8.GetString(namebytes, 0, i);
		}

		internal static byte[] NameToByteArray(string name)
		{
			var count = Encoding.UTF8.GetByteCount(name);
			var bytes = new byte[count + 1];
			//bytes[count] = 0; // Already done for us.
			Encoding.UTF8.GetBytes(name, 0, name.Length, bytes, 0);
			return bytes;
		}
#endif

		// List of DirectoryEntiry; this class actually exists so that callers don't take a dependency on downcasting
		// IEnumerable<DirectoryEntry> to List<DirectoryEntry>.  I've switched back and forth between yield return and
		// list more than once.
    private class DirectoryEntryList : IEnumerable<DirectoryEntry> {
			private int clist;
			private int alist;
			private DirectoryEntry[] list;

			internal DirectoryEntryList()
			{
				//clist = 0;
				alist = 16;
				list = new DirectoryEntry[16];
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

			public IEnumerator<DirectoryEntry> GetEnumerator() => new Enumerator(this);

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

				public bool MoveNext()
				{
					if (offset + 1 >= clist) return false;
					++offset;
					return true;
				}

				public DirectoryEntry Current => list[offset];

				object System.Collections.IEnumerator.Current => Current;

				public void Reset() => offset = -1;

				void IDisposable.Dispose() {}
			}
		}
	}
}
