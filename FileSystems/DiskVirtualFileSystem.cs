/* vi:ts=2
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using static Emet.FileSystems.Util;

namespace Emet.FileSystems {
	///<summary>Provides the factory for the base implementations of IVirtualFileSystem and IDiskVirtualFileSystem</summary>
	public static class DiskVirtualFileSystem {

#if OS_WIN || OSTYPE_UNIX

		public static IDiskVirtualFileSystem VirtualizeWhole() => WholeVirtualFileSystem.Instance;

		public static IDiskVirtualFileSystem VirtualizeWholePreserveCurrentDirectory() => CurrentDirectoryVirtualFileSystem.Instance;

		private sealed class WholeVirtualFileSystem : IDiskVirtualFileSystem {
			private WholeVirtualFileSystem() {}
			internal readonly static IDiskVirtualFileSystem Instance = new WholeVirtualFileSystem();

			public bool SupportsCreationTime => FileSystem.OSSupportsCreationTime;
			public bool SupportsAccessTime => true;
			public bool SupportsHardLinks => FileSystem.OSSupportsHardLinks;
			public bool SupportsSymbolicLinks => FileSystem.OSSupportsSymbolicLinks;
			public bool SupportsDirectoryHandles => true;

			public char DirectorySeparatorCharacter => Path.DirectorySeparatorChar;
			public char AlternateDirectorySeparatorCharacter => Path.AltDirectorySeparatorChar;
			public string RootDirectoryName { get; } = Path.DirectorySeparatorChar.ToString();
			public string CurrentDirectoryName => ".";
			public string ParentDirectoryName => "..";

			public IDiskVirtualFileSystem CreateChild(string path) => CreateChild(path, FileSystem.FollowSymbolicLinks.Never);

			public IDiskVirtualFileSystem CreateChild(string path, FileSystem.FollowSymbolicLinks followSymbolicLinks)
			{
				SafeFileHandle handle = null;
				try {
#if OSTYPE_UNIX
					handle = OpenDirectory(MakeAbsolute(path), followSymbolicLinks, false);
					var r = new VirtualChroot(handle);
#elif OS_WIN
					path = MakeAbsolute(path);
					handle = GetDirectoryHandle(path, path, false, followSymbolicLinks == FileSystem.FollowSymbolicLinks.Always, false);
					var r = new VirtualChroot(path, handle);
#else
				SYNTAX_ERROR
#endif
					handle = null;
					return r;
				} finally {
					handle?.Dispose();
				}
			}

			IVirtualFileSystem IVirtualFileSystem.CreateChild(string path) => CreateChild(path, FileSystem.FollowSymbolicLinks.Never);

			public bool CreateDirectory(string path)
			{
				if (path is null) throw new ArgumentNullException("path");
				if (path.Length == 0) throw new ArgumentOutOfRangeException("path", "path cannot be the empty string");
#if OSTYPE_UNIX
				return DiskVirtualFileSystem.CreateDirectory(NameToByteArray(MakeAbsolute(path)), path);
#elif OS_WIN
				string patharg = MakeAbsolute(path);
				if (NativeMethods.CreateDirectory(patharg, IntPtr.Zero) != 0) return true;
				if (Marshal.GetLastWin32Error() == (IOErrors.AlreadyExists & 0xFFFF)) return false;
				throw GetExceptionFromLastError(path, path, false, 0, true);
#endif
			}

			public Stream OpenVirtualFile(string path, FileMode fileMode, FileAccess fileAccess, bool forAsyncAccess, int bufferSize)
				=> OpenFile(path, fileMode, fileAccess, FileSystem.FollowSymbolicLinks.Always, forAsyncAccess, bufferSize);

			public FileStream OpenFile(string path, FileMode fileMode, FileAccess fileAccess, FileSystem.FollowSymbolicLinks followSymbolicLinks, bool forAsyncAccess, int bufferSize)
			{
				if (path is null) throw new ArgumentNullException("path");
				if (path.Length == 0) throw new ArgumentOutOfRangeException("path", "path cannot be the empty string");
#if OSTYPE_UNIX
				var apath = MakeAbsolute(path);
				return OpenFileStream(NameToByteArray(apath), path, path, fileMode, fileAccess,
						followSymbolicLinks != FileSystem.FollowSymbolicLinks.Never, forAsyncAccess, bufferSize);
#elif OS_WIN
				return OpenFileStream(MakeAbsolute(path), path, fileMode, fileAccess,
						followSymbolicLinks != FileSystem.FollowSymbolicLinks.Never, forAsyncAccess, bufferSize);
#endif
			}

			public SafeFileHandle OpenDirectory(string path, FileSystem.FollowSymbolicLinks followSymbolicLinks, bool requestEnumeration)
			{
				if (path is null) throw new ArgumentNullException("path");
				if (path.Length == 0) throw new ArgumentOutOfRangeException("path", "path cannot be the empty string");
				return GetDirectoryHandle(MakeAbsolute(path), path, followSymbolicLinks == FileSystem.FollowSymbolicLinks.Always, requestEnumeration);
			}

			public IEnumerable<DirectoryEntry>? GetDirectoryContents(string path, FileSystem.NonExtantDirectoryBehavior nonExtantDirectoryBehavior)
			{
				if (path is null) throw new ArgumentNullException("path");
				if (path.Length == 0) throw new ArgumentOutOfRangeException("path", "path cannot be the empty string");
				return FileSystem.GetDirectoryContents(MakeAbsolute(path), nonExtantDirectoryBehavior);
			}

			public DirectoryEntry GetDirectoryEntry(string path)
			{
				if (path is null) throw new ArgumentNullException("path");
				if (path.Length == 0) throw new ArgumentOutOfRangeException("path", "path cannot be the empty string");
				return new AbsoluteDirectoryEntry(path);
			}

			private sealed class AbsoluteDirectoryEntry : DirectoryEntry
			{
				private string patharg;

				internal AbsoluteDirectoryEntry(string path) : base(MakeAbsolute(path), FileSystem.FollowSymbolicLinks.Never)
				{
					patharg = path;
				}

				private AbsoluteDirectoryEntry(string path, string errorpath) : base(path, FileSystem.FollowSymbolicLinks.Always)
				{
					patharg = errorpath;
				}

				protected override string ErrorPath => patharg;

				protected override DirectoryEntry _ResolveSymbolicLink() => new AbsoluteDirectoryEntry(Path, patharg);
			}

			public void RenameReplace(string oldname, string newname)
			{
				if (oldname is null) throw new ArgumentNullException("oldname");
				if (oldname.Length == 0) throw new ArgumentOutOfRangeException("oldname", "oldname cannot be the empty string");
				if (newname is null) throw new ArgumentNullException("newname");
				if (newname.Length == 0) throw new ArgumentOutOfRangeException("newname", "newname cannot be the empty string");
				FileSystem.RenameReplace(MakeAbsolute(oldname), MakeAbsolute(newname));
			}

			public void CreateHardLink(string targetpath, string linkpath)
			{
				if (targetpath is null) throw new ArgumentNullException("targetpath");
				if (targetpath.Length == 0) throw new ArgumentOutOfRangeException("targetpath", "targetpath cannot be the empty string");
				if (linkpath is null) throw new ArgumentNullException("linkpath");
				if (linkpath.Length == 0) throw new ArgumentOutOfRangeException("linkpath", "linkpath cannot be the empty string");
				FileSystem.CreateHardLink(MakeAbsolute(targetpath), MakeAbsolute(linkpath));
			}
			
			public void CreateSymbolicLink(string targetpath, string linkpath, FileType linkTargetType)
			{
				if (targetpath is null) throw new ArgumentNullException("targetpath");
				if (targetpath.Length == 0) throw new ArgumentOutOfRangeException("targetpath", "targetpath cannot be the empty string");
				if (linkpath is null) throw new ArgumentNullException("linkpath");
				if (linkpath.Length == 0) throw new ArgumentOutOfRangeException("linkpath", "linkpath cannot be the empty string");
				FileSystem.CreateSymbolicLink(targetpath, MakeAbsolute(linkpath));
			}

			public string ReadLink(string path)
			{
				if (path is null) throw new ArgumentNullException("path");
				if (path.Length == 0) throw new ArgumentOutOfRangeException("path", "path cannot be the empty string");
				return FileSystem.ReadLink(MakeAbsolute(path));
			}

			public bool RemoveFile(string path)
			{
				if (path is null) throw new ArgumentNullException("path");
				if (path.Length == 0) throw new ArgumentOutOfRangeException("path", "path cannot be the empty string");
				return FileSystem.RemoveFile(MakeAbsolute(path));
			}
			
			public bool RemoveDirectory(string path, bool recurse)
			{
				if (path is null) throw new ArgumentNullException("path");
				if (path.Length == 0) throw new ArgumentOutOfRangeException("path", "path cannot be the empty string");
				return FileSystem.RemoveDirectory(MakeAbsolute(path), recurse);
			}
			
			public void Dispose() {}
		}

		private sealed class CurrentDirectoryVirtualFileSystem : IDiskVirtualFileSystem {
			private CurrentDirectoryVirtualFileSystem() {}
			internal readonly static IDiskVirtualFileSystem Instance = new CurrentDirectoryVirtualFileSystem();

			public bool SupportsCreationTime => FileSystem.OSSupportsCreationTime;
			public bool SupportsAccessTime => true;
			public bool SupportsHardLinks => FileSystem.OSSupportsHardLinks;
			public bool SupportsSymbolicLinks => FileSystem.OSSupportsSymbolicLinks;
			public bool SupportsFileExclusiveRead => true;
			public bool SupportsFileExclusiveWrite => true;
#if OS_WIN
			public bool SupportsFileExclusiveDelete => true;
#else
			public bool SupportsFileExclusiveDelete => false;
#endif
			public bool SupportsDirectoryHandles => true;

			public char DirectorySeparatorCharacter => Path.DirectorySeparatorChar;
			public char AlternateDirectorySeparatorCharacter => Path.AltDirectorySeparatorChar;
			public string RootDirectoryName { get; } = Path.DirectorySeparatorChar.ToString();
			public string CurrentDirectoryName => ".";
			public string ParentDirectoryName => "..";

			public IDiskVirtualFileSystem CreateChild(string path) => CreateChild(path, FileSystem.FollowSymbolicLinks.Never);

			public IDiskVirtualFileSystem CreateChild(string path, FileSystem.FollowSymbolicLinks followSymbolicLinks)
			{
				SafeFileHandle handle = null;
				try {
#if OSTYPE_UNIX
					handle = OpenDirectory(path, followSymbolicLinks, false);
					var r = new VirtualChroot(handle);
#elif OS_WIN
					handle = GetDirectoryHandle(path, path, false, followSymbolicLinks == FileSystem.FollowSymbolicLinks.Always, false);
					var r = new VirtualChroot(path, handle);
#else
					SYNTAX ERROR
#endif
					handle = null;
					return r;
				} finally {
					handle?.Dispose();
				}
			}

			IVirtualFileSystem IVirtualFileSystem.CreateChild(string path) => CreateChild(path, FileSystem.FollowSymbolicLinks.Never);

			public bool CreateDirectory(string path)
			{
				if (path is null) throw new ArgumentNullException("path");
				if (path.Length == 0) throw new ArgumentOutOfRangeException("path", "path cannot be the empty string");
#if OSTYPE_UNIX
				return DiskVirtualFileSystem.CreateDirectory(NameToByteArray(path), path);
#elif OS_WIN
				if (NativeMethods.CreateDirectory(path, IntPtr.Zero) != 0) return true;
				if (Marshal.GetLastWin32Error() == (IOErrors.AlreadyExists & 0xFFFF)) return false;
				if (Marshal.GetLastWin32Error() == (IOErrors.PathNotFound & 0xFFFF)) {
					var dname = Path.GetDirectoryName(path);
					if (dname.Length > 0 && dname != "." && dname != path) {
						CreateDirectory(dname);
						if (NativeMethods.CreateDirectory(path, IntPtr.Zero) != 0) return true;
						if (Marshal.GetLastWin32Error() == (IOErrors.AlreadyExists & 0xFFFF)) return false;
					}
				}
				throw GetExceptionFromLastError(path, path, false, 0, true);
#endif
			}

			public Stream OpenVirtualFile(string path, FileMode fileMode, FileAccess fileAccess, bool forAsyncAccess, int bufferSize)
				=> OpenFile(path, fileMode, fileAccess, FileSystem.FollowSymbolicLinks.Always, forAsyncAccess, bufferSize);

			public FileStream OpenFile(string path, FileMode fileMode, FileAccess fileAccess, FileSystem.FollowSymbolicLinks followSymbolicLinks, bool forAsyncAccess, int bufferSize)
			{
#if OSTYPE_UNIX
				if (path is null) throw new ArgumentNullException("path");
				if (path.Length == 0) throw new ArgumentOutOfRangeException("path", "path cannot be the empty string");
				return OpenFileStream(NameToByteArray(path), path, path, fileMode, fileAccess,
						followSymbolicLinks != FileSystem.FollowSymbolicLinks.Never, forAsyncAccess, bufferSize);
#elif OS_WIN
				if (path is null) throw new ArgumentNullException("path");
				if (path.Length == 0) throw new ArgumentOutOfRangeException("path", "path cannot be the empty string");
				return OpenFileStream(path, path, fileMode, fileAccess,
						followSymbolicLinks != FileSystem.FollowSymbolicLinks.Never, forAsyncAccess, bufferSize);
#endif
			}

			public SafeFileHandle OpenDirectory(string path, FileSystem.FollowSymbolicLinks followSymbolicLinks, bool requestEnumeration)
			{
				if (path is null) throw new ArgumentNullException("path");
				if (path.Length == 0) throw new ArgumentOutOfRangeException("path", "path cannot be the empty string");
				return GetDirectoryHandle(path, path, followSymbolicLinks == FileSystem.FollowSymbolicLinks.Always, requestEnumeration);
			}

			public IEnumerable<DirectoryEntry>? GetDirectoryContents(string path, FileSystem.NonExtantDirectoryBehavior nonExtantDirectoryBehavior)
				=> FileSystem.GetDirectoryContents(path, nonExtantDirectoryBehavior);

			public DirectoryEntry GetDirectoryEntry(string path) => new DirectoryEntry(path, FileSystem.FollowSymbolicLinks.Never);

			public void RenameReplace(string oldname, string newname) => FileSystem.RenameReplace(oldname, newname);

			public void CreateHardLink(string linkpath, string targetpath) => FileSystem.CreateHardLink(linkpath, targetpath);
			
			public void CreateSymbolicLink(string linkpath, string targetpath, FileType linkTargetType) => FileSystem.CreateSymbolicLink(linkpath, targetpath);

			public string ReadLink(string path) => FileSystem.ReadLink(path);

			public bool RemoveFile(string path) => FileSystem.RemoveFile(path);
			
			public bool RemoveDirectory(string path, bool recurse) => FileSystem.RemoveDirectory(path, recurse);
			
			public void Dispose() {}
		}

		public static IDiskVirtualFileSystem VirtualizeChrootDirectory(string path)
			=> VirtualizeWholePreserveCurrentDirectory().CreateChild(path, FileSystem.FollowSymbolicLinks.Never);

#else
		///<summary>Creates a virtual file system that is the entire system as observed</summary>
		///<returns>a virtulaization of the whole; this method may be implemented as a singleton</returns>
		///<remarks>The only difference between this and just calling the methods is CurrentDirectory is no longer a thing.
		///To use in-application descent logic, use .CreateChild(".").</remarks>
		public static IDiskVirtualFileSystem VirtualizeWhole() => throw null;

		///<summary>Creates a virtual file system that is the entire system as observed</summary>
		///<returns>a virtulaization of the whole; this method may be implemented as a singleton</returns>
		///<remarks>This call preserves the behavior of Environment.CurrentDirectory</remarks>
		public static IDiskVirtualFileSystem VirtualizeWholePreserveCurrentDirectory() => throw null;
#endif

#if OSTYPE_UNIX
		private static string MakeAbsolute(string path) => path[0] == '/' ? path : "/" + path;

		private static FileStream OpenFileStream(byte[] patharg, string chkpath, string path, FileMode fileMode, FileAccess fileAccess, bool follow, bool forAsyncAccess, int bufferSize)
		{
			if ((int)fileAccess < 1 || (int)fileAccess > 3) throw new ArgumentOutOfRangeException("fileAccess");
			uint options = (follow ? NativeMethods.O_NOFOLLOW : 0) | ((uint)fileAccess & 3) - 1 | NativeMethods.O_CLOEXEC;
			switch (fileMode) {
				case (FileMode)6 /* append in newer reference files than we compile against */: options |= NativeMethods.O_APPEND | NativeMethods.O_CREAT; break;
				case FileMode.Create: options |= NativeMethods.O_CREAT | NativeMethods.O_TRUNC; break;
				case FileMode.CreateNew: options |= NativeMethods.O_CREAT | NativeMethods.O_EXCL; break;
				case FileMode.Open: break;
				case FileMode.OpenOrCreate: options |= NativeMethods.O_CREAT; break;
				case FileMode.Truncate: options |= NativeMethods.O_TRUNC; break;
				default: throw new ArgumentOutOfRangeException("fileAccess");
			}
			SafeFileHandle safe = null;
			FileStream stream = null;
			int handle = -1;
			IOException exception;
			try {
				do {
					handle = NativeMethods.open(patharg, options, NativeMethods.DefaultFileMode);
				} while (IsEIntrSyscallReturnOrException(handle, chkpath, path, false, 0, true, out exception));
				if (exception is not null) throw exception;
				safe = new SafeFileHandle((IntPtr)handle, true);
				if (new FileSystemNode(safe).FileType == FileType.Directory)
					throw GetExceptionFromErrno(IOErrors.IsADirectory, chkpath, path, new System.ComponentModel.Win32Exception(IOErrors.IsADirectory).Message);
				stream = new FileStream(safe, fileAccess, bufferSize, forAsyncAccess);
			} finally {
				if (safe is null && handle >= 0) NativeMethods.close(handle);
				if (stream is null && safe is not null) safe.Dispose();
			}
			return stream;
		}

		private static SafeFileHandle GetDirectoryHandle(string chkpath, string path, bool forEnumeration, bool follow)
			=> GetDirectoryHandle(NameToByteArray(path), chkpath, path, forEnumeration, follow, false);

		private static SafeFileHandle GetDirectoryHandle(byte[] patharg, string chkpath, string path, bool forEnumeration, bool follow, bool notFoundNull)
		{
#if OS_LINUXX64
			uint flags = NativeMethods.O_RDONLY | NativeMethods.O_DIRECTORY | (forEnumeration ? 0 : NativeMethods.O_PATH);
#else
			uint flags = NativeMethods.O_RDONLY | NativeMethods.O_DIRECTORY; /* OSX imposes a no-traverse-only constraint upon us */
#endif
			if (!follow) flags |= NativeMethods.O_NOFOLLOW;
			SafeFileHandle safe = null;
			int handle = -1;
			try {
				IOException exception;
				do {
					handle = NativeMethods.open(patharg, flags, 0);
				} while (IsEIntrSyscallReturnOrException(handle, chkpath, path, false, 0, false, out exception));
				if (notFoundNull && exception?.HResult == IOErrors.FileNotFound) return null;
				if (exception is not null) throw exception;
				safe = new SafeFileHandle((IntPtr)handle, true);
				// If porting to a target that doesn't support O_DIRECTORY, check here
			} finally {
				if (safe is null && handle >= 0) NativeMethods.close(handle);
			}
			return safe;
		}

		private static bool CreateDirectory(byte[] patharg, string path)
		{
				System.Collections.Generic.List<int> patches = null;
				int offset = patharg.Length - 1;
				while (offset > 1 && patharg[offset - 1] == (byte)'/')
					patharg[--offset] = 0;
				bool decreasing = true;
				int error;
				for(;;) {
					do {
						int rtn = NativeMethods.mkdir(patharg, NativeMethods.DefaultDirectoryMode);
						error = rtn == 0 ? 0 : Marshal.GetLastWin32Error();
					} while (error == IOErrors.Interrupted);
					if (error == 0 || error == IOErrors.FileExists) decreasing = false;
					if (decreasing) {
						if (error != IOErrors.FileNotFound) throw GetExceptionFromLastError("", path, false, 0, true);
						for (;;)
							if (patharg[--offset] == (byte)'/') {
								if (offset == 0) throw new InvalidOperationException("Trying to create a directory off the root failed with ENOENT");
								patches ??= new System.Collections.Generic.List<int>();
								patches.Add(offset);
								patharg[offset] = 0;
								break;
							}
					} else {
						if (error != 0 && error != IOErrors.FileExists) throw GetExceptionFromLastError("", path, false, 0, true);
						if (patches is null || patches.Count == 0) return error == 0;
						offset = patches[patches.Count - 1];
						patches.RemoveAt(patches.Count - 1);
						patharg[offset] = (byte)'/';
					}
				}
		}

		internal sealed class VirtualChroot : IDiskVirtualFileSystem
		{
			private readonly SafeFileHandle handle;

			public VirtualChroot(SafeFileHandle handle) { this.handle = handle; }

			public bool SupportsCreationTime => false;
			public bool SupportsAccessTime => true;
			public bool SupportsHardLinks => true;
			public bool SupportsSymbolicLinks => true;
			public bool SupportsDirectoryHandles => true;
			public char DirectorySeparatorCharacter => '/';
			public char AlternateDirectorySeparatorCharacter => '/';
			public string CurrentDirectoryName => ".";
			public string ParentDirectoryName => "..";
			public string RootDirectoryName => "/";

			IVirtualFileSystem IVirtualFileSystem.CreateChild(string path) => CreateChild(path, FileSystem.FollowSymbolicLinks.Never);

			public IDiskVirtualFileSystem CreateChild(string path) => CreateChild(path, FileSystem.FollowSymbolicLinks.Never);

			public IDiskVirtualFileSystem CreateChild(string path, FileSystem.FollowSymbolicLinks followSymbolicLinks)
			{
				SafeFileHandle handle = null;
				try {
					handle = OpenDirectory(path, followSymbolicLinks, false);
					var r = new VirtualChroot(handle);
					handle = null;
					return r;
				} finally {
					handle?.Dispose();
				}
			}

			internal SafeFileHandle Traverse(string path, bool mkdir, bool traverseFinal, bool notFoundNull, out byte[] finalPath)
			{
				if (handle is null) throw new ObjectDisposedException("Emet.FileSystems.IDiskVirtualFileSystem");
				finalPath = new byte[13 + 10 + 256 + 1];
				var buffer = new byte[1024];
				var activepath = NameToByteArrayExact(path);
				const int prefixlen = 14;
				NameToByteArray(finalPath, 0, "/proc/self/fd/", 0, prefixlen);
				var traverse = new List<SafeFileHandle>();
				SafeFileHandle rtn = null;
				int counter = 0;
				try {
					int index = Advance2(activepath, 0);
					int nextIndex = Advance(activepath, index);
					for (;;) {
						int h = (traverse.Count == 0 ? handle : traverse[traverse.Count - 1]).DangerousGetHandle().ToInt32();
						int offset = 0;
						do {
							finalPath[prefixlen + offset++] = (byte)(h % 10 + '0');
							h /= 10;
						} while (h > 0);
						for (int i = 0; i < offset / 2; i++)
							(finalPath[prefixlen + i], finalPath[prefixlen + offset - i - 1]) = (finalPath[prefixlen + offset - i - 1], finalPath[prefixlen + i]);
						finalPath[prefixlen + offset++] = (byte)'/';
						if (nextIndex - index > 255)
								throw GetExceptionFromErrno(IOErrors.BadPathName, path, path,
										new System.ComponentModel.Win32Exception(IOErrors.BadPathName).Message);
						Array.Copy(activepath, index, finalPath, prefixlen + offset, nextIndex - index);
						finalPath[prefixlen + offset + nextIndex - index] = 0;

						if (!traverseFinal && nextIndex == activepath.Length) break;
	
						if (finalPath[offset] == '.' && finalPath[offset + 1] == 0) {
							// current directory
						} else if (finalPath[offset] == '.' && finalPath[offset + 1] == '.' && finalPath[offset = 2] == 0) {
							// parent directory
							if (traverse.Count > 0) {
								traverse[traverse.Count - 1].Dispose();
								traverse.RemoveAt(traverse.Count - 1);
							}
						} else {
							// Open it
							int n;
							do {
								n = (int)NativeMethods.readlink(finalPath, buffer, buffer.Length);
							} while (IsEIntrSyscallReturn(n));
							if (n < 0 && Marshal.GetLastWin32Error() == IOErrors.IoError)
								throw GetExceptionFromLastError(path, path, false, 0, false);
							if (mkdir && n < 0 && Marshal.GetLastWin32Error() == IOErrors.FileNotFound) {
								int e;
								IOException ex;
								do {
									e = NativeMethods.mkdir(finalPath, NativeMethods.DefaultDirectoryMode);
								} while (IsEIntrSyscallReturnOrException(e, "", path, false, IOErrors.FileExists, false, out ex));
								if (ex is not null) throw ex;
							}
							if (n > 0) {
								if (++counter == 30)
									throw GetExceptionFromErrno(IOErrors.TooManySymbolicLinks, path, path,
											new System.ComponentModel.Win32Exception(IOErrors.TooManySymbolicLinks).Message);
								if (n < buffer.Length) buffer[n] = 0;	// Buffer is reused; omitting this is an extremely subtle bug
								int boffset = 0;
								if (buffer[0] == '/') {
									for (int i = traverse.Count; i --> 0;) {
										traverse[i].Dispose();
										traverse.RemoveAt(i);
									}
									boffset = Advance2(buffer, 1);
								}
								byte[] newbuf;
								if (activepath.Length == nextIndex) {
									newbuf = new byte[n - boffset];
									Array.Copy(buffer, boffset, newbuf, 0, n - boffset);
								} else {
									newbuf = new byte[n - boffset + activepath.Length - nextIndex];
									Array.Copy(buffer, boffset, newbuf, 0, n - boffset);
									Array.Copy(activepath, nextIndex, newbuf, activepath.Length - nextIndex, n - boffset);
								}
								activepath = newbuf;
								index = 0;
								if (index == activepath.Length) {
									// Ended with a symlink to /
									finalPath[prefixlen + offset] = (byte)'.';
									finalPath[prefixlen + offset + 1] = 0;
									break;
								}
								nextIndex = Advance(activepath, index);
							} else if (n == 0) {
								throw new InvalidOperationException("Zero length symbolic link target encountered");
							} else {
								if (nextIndex == activepath.Length) break;
								traverse.Add(null); /* stable over OOM */
								traverse[traverse.Count - 1] = GetDirectoryHandle(finalPath, "" /* is not FileNotFoundException */, path, false, false, notFoundNull);
								if (traverse[traverse.Count - 1] is null) { finalPath[0] = 0; return null; }
								index = Advance2(activepath, nextIndex);
								if (index == activepath.Length) {
									// Trailing /
									finalPath[prefixlen + offset] = (byte)'.';
									finalPath[prefixlen + offset + 1] = 0;
									break;
								}
								nextIndex = Advance(activepath, index);
							}
						}
					}
					rtn = traverse.Count == 0 ? null : traverse[traverse.Count - 1];
				} finally {
					for (int i = traverse.Count - 1; i --> 0;)
						if (rtn != traverse[i]) traverse[i]?.Dispose();
				}
				return rtn;
			}

			private static int Advance(byte[] activepath, int index)
			{
				index = Advance2(activepath, index);
				while (index < activepath.Length && activepath[index] != '/')
					index++;
				return index;
			}

			private static int Advance2(byte[] activepath, int nextIndex)
			{
				while (nextIndex < activepath.Length && activepath[nextIndex] == '/')
					nextIndex++;
				return nextIndex;
			}
			
			public Stream OpenVirtualFile(string path, FileMode fileMode, FileAccess fileAccess, bool forAsyncAccess, int bufferSize)
				=> OpenFile(path, fileMode, fileAccess, FileSystem.FollowSymbolicLinks.Always, forAsyncAccess, bufferSize);

			public FileStream OpenFile(string path, FileMode fileMode, FileAccess fileAccess, FileSystem.FollowSymbolicLinks followSymbolicLinks, bool forAsyncAccess, int bufferSize)
			{
				if (path is null) throw new ArgumentNullException("path");
				if (path.Length == 0) throw new ArgumentOutOfRangeException("path", "path cannot be the empty string");
				using (var traverse = Traverse(path, false, followSymbolicLinks != FileSystem.FollowSymbolicLinks.Never, false, out var finalpath))
					return OpenFileStream(finalpath, null, path, fileMode, fileAccess, false, forAsyncAccess, bufferSize);
			}

			public SafeFileHandle OpenDirectory(string path, FileSystem.FollowSymbolicLinks followSymbolicLinks, bool requestEnumeration)
			{
				if (path is null) throw new ArgumentNullException("path");
				if (path.Length == 0) throw new ArgumentOutOfRangeException("path", "path cannot be the empty string");
				using (var traverse = Traverse(path, false, followSymbolicLinks == FileSystem.FollowSymbolicLinks.Always, false, out var finalpath))
					return GetDirectoryHandle(finalpath, "", path, requestEnumeration, false, false);
			}

			public bool CreateDirectory(string path)
			{
				if (path is null) throw new ArgumentNullException("path");
				if (path.Length == 0) throw new ArgumentOutOfRangeException("path", "path cannot be the empty string");
				int e;
				IOException ex;
				using (var traverse = Traverse(path, true, false, false, out var finalpath))
				{
					do {
						e = NativeMethods.mkdir(finalpath, NativeMethods.DefaultDirectoryMode);
					} while (IsEIntrSyscallReturnOrException(e, "", path, false, 0, true, out ex));
					if (ex?.HResult == IOErrors.FileExists && FileSystem.DirectoryExists(ByteArrayToName(finalpath)))
						ex = null;
				}
				if (ex is not null) throw ex;
				return e == 0;
			}

			public void CreateHardLink(string targetpath, string linkpath)
			{
				if (targetpath is null) throw new ArgumentNullException("targetpath");
				if (targetpath.Length == 0) throw new ArgumentOutOfRangeException("targetpath", "targetpath cannot be the empty string");
				if (linkpath is null) throw new ArgumentNullException("linkpath");
				if (linkpath.Length == 0) throw new ArgumentOutOfRangeException("linkpath", "linkpath cannot be the empty string");
				IOException ex;
				using (var traverseold = Traverse(targetpath, false, false, false, out var finalold))
				using (var traversenew = Traverse(linkpath, false, false, false, out var finalnew))
				while (IsEIntrSyscallReturnOrException(NativeMethods.link(finalold, finalnew), null, targetpath, false, 0, true, out ex));
				if (ex is not null) {
					// Chroot into FAT is broken; we cannot reliably call pathconf from here.
					if (ex.HResult == IOErrors.NoSuchSystemCall || ex.HResult == IOErrors.SocketOperationNotSupported)
						ex = GetExceptionFromErrno(IOErrors.NoSuchSystemCall, "", targetpath, "File system does not support hard links");
					throw ex;
				}
			}

			public void CreateSymbolicLink(string targetpath, string linkpath, FileType targethint)
			{
				if (targetpath is null) throw new ArgumentNullException("targetpath");
				if (targetpath.Length == 0) throw new ArgumentOutOfRangeException("targetpath", "targetpath cannot be the empty string");
				if (linkpath is null) throw new ArgumentNullException("linkpath");
				if (linkpath.Length == 0) throw new ArgumentOutOfRangeException("linkpath", "linkpath cannot be the empty string");
				IOException ex;
				using (var traversenew = Traverse(linkpath, false, false, false, out var finalnew))
				while (IsEIntrSyscallReturnOrException(NativeMethods.symlink(NameToByteArray(targetpath), finalnew), null, linkpath, false, 0, true, out ex));
				if (ex is not null) {
					// Chroot into FAT is broken; we cannot reliably call pathconf from here.
					if (ex.HResult == IOErrors.NoSuchSystemCall || ex.HResult == IOErrors.SocketOperationNotSupported)
						ex = GetExceptionFromErrno(IOErrors.NoSuchSystemCall, "", targetpath, "File system does not support symbolic links");
					throw ex;
				}
			}

			public DirectoryEntry GetDirectoryEntry(string path) => new VirtualChrootDirectoryEntry(path, FileType.NodeHintNotAvailable, false, this);

			public IEnumerable<DirectoryEntry>? GetDirectoryContents(string path, FileSystem.NonExtantDirectoryBehavior nonExtantDirectoryBehavior)
			{
				if (path is null) throw new ArgumentNullException("path");
				if (path.Length == 0) throw new ArgumentOutOfRangeException("path", "path cannot be the empty string");
				bool throwMissing = nonExtantDirectoryBehavior != FileSystem.NonExtantDirectoryBehavior.ReturnEmpty
						&& nonExtantDirectoryBehavior != FileSystem.NonExtantDirectoryBehavior.ReturnNull;
				IntPtr opendirhandle = IntPtr.Zero;
				int handle = -1;
				try {
					using (var traverse = Traverse(path, false, true, !throwMissing, out var finalpath))
					{
						if (traverse is null && finalpath[0] == 0)
							return (nonExtantDirectoryBehavior == FileSystem.NonExtantDirectoryBehavior.ReturnNull) ? null : Enumerable.Empty<DirectoryEntry>();
						IOException exception;
						do {
							handle = NativeMethods.open(finalpath, NativeMethods.O_NOFOLLOW | NativeMethods.O_RDONLY | NativeMethods.O_DIRECTORY, 0);
						} while (IsEIntrSyscallReturnOrException(handle, "", path, false, 0, false, out exception));
						if (!throwMissing && exception?.HResult == IOErrors.FileNotFound)
							return (nonExtantDirectoryBehavior == FileSystem.NonExtantDirectoryBehavior.ReturnNull) ? null : Enumerable.Empty<DirectoryEntry>();
						if (exception is not null) throw exception;
					}
					opendirhandle = NativeMethods.fdopendir(handle);
					if (opendirhandle == IntPtr.Zero) throw new OutOfMemoryException();
					handle = -1;
					var entries = new FileSystem.DirectoryEntryList();
#if OS_LINUXX64
					var readentry = new NativeMethods.dirent64();
					readentry.d_name = new byte[256];
#endif
					for (;;) {
#if OS_LINUXX64
						int result = NativeMethods.readdir64_r(opendirhandle, ref readentry, out IntPtr z);
						if (result > 0) throw GetExceptionFromLastError("", path, false, 0, false);
						if (z == IntPtr.Zero) break;
						int dhint = (int)readentry.d_type << 12;
						if (dhint == 0 || dhint > 0b1111_000_000_000_0000) dhint = (int)FileType.NodeHintNotAvailable;
						var dname = ByteArrayToName(readentry.d_name);
#else
						IntPtr result = NativeMethods.readdir(opendirhandle);
						if (result == IntPtr.Zero) {
							var errno = Marshal.GetLastWin32Error();
							if (errno == 0) break;
							throw GetExceptionFromLastError("", path, false, 0, false);
						}
						var readentry = Marshal.PtrToStructure<NativeMethods.dirent>(result);
#if OS_FEATURES_DTYPE
						int dhint = (int)readentry.d_type << 12;
						if (dhint == 0 || dhint > 0b1111_000_000_000_0000) dhint = (int)FileType.NodeHintNotAvailable;
#else
						int dhint = (int)FileType.NodeHintNotAvailable;
#endif
						var bytes = new byte[readentry.d_namelen];
						Marshal.Copy(result + NativeMethods.dirent_d_name_offset, bytes, 0, readentry.d_namelen);
						var dname = ByteArrayToName(bytes);
#endif
						if (dname == DirectoryEntry.CurrentDirectoryName || dname == DirectoryEntry.ParentDirectoryName) continue;
						entries.Add(new VirtualChrootDirectoryEntry(Path.Combine(path, dname), (FileType)dhint, false, this));
					}
					return entries;
				} finally {
					if (handle != -1) NativeMethods.close(handle);
					if (opendirhandle != IntPtr.Zero) NativeMethods.closedir(opendirhandle);
				}
			}

			public string ReadLink(string path)
			{
				if (path is null) throw new ArgumentNullException("path");
				if (path.Length == 0) throw new ArgumentOutOfRangeException("path", "path cannot be the empty string");
				int e;
				IOException ex;
				var buf = new byte[1025];
				using (var traverse = Traverse(path, false, false, false, out var finalpath)) {
					do {
						e = (int)NativeMethods.readlink(finalpath, buf, 1024);
					} while (IsEIntrSyscallReturnOrException(e, "", path, false, 0, false, out ex));
				}
				if (ex is not null) throw ex;
				buf[e] = 0;
				return ByteArrayToName(buf);
			}

			public bool RemoveFile(string path)
			{
				if (path is null) throw new ArgumentNullException("path");
				if (path.Length == 0) throw new ArgumentOutOfRangeException("path", "path cannot be the empty string");
				int e;
				IOException ex;
				using (var traverse = Traverse(path, false, false, true, out var finalpath)) {
					if (traverse is null) return false;
					do {
						e = NativeMethods.unlink(finalpath);
					} while (IsEIntrSyscallReturnOrException(e, "", path, false, IOErrors.FileNotFound, true, out ex));
				}
				if (ex is not null) throw ex;
				return e == 0;
			}

			public bool RemoveDirectory(string path, bool recurse)
			{
				if (path is null) throw new ArgumentNullException("path");
				if (path.Length == 0) throw new ArgumentOutOfRangeException("path", "path cannot be the empty string");
				using (var traverse = Traverse(path, false, false, true, out var finalpath)) {
					if (traverse is null && finalpath[0] == 0) return false;
					var name = NameToByteArray(path);
					var epb = new ErrorPathBuilder();
					bool pushed = false;
					epb.Push(name, 0, name.Length, ref pushed);
					return FileSystem.RemoveDirectory(finalpath, 0, recurse ? 1 : 0, epb);
				}
			}

			public void RenameReplace(string oldname, string newname)
			{
				if (oldname is null) throw new ArgumentNullException("oldname");
				if (oldname.Length == 0) throw new ArgumentOutOfRangeException("oldname", "oldname cannot be the empty string");
				if (newname is null) throw new ArgumentNullException("newname");
				if (newname.Length == 0) throw new ArgumentOutOfRangeException("newname", "newname cannot be the empty string");
				IOException ex;
				using (var traverseold = Traverse(oldname, false, false, false, out var finalold))
				using (var traversenew = Traverse(newname, false, false, false, out var finalnew))
				while (IsEIntrSyscallReturnOrException(NativeMethods.rename(finalold, finalnew), oldname, oldname, false, 0, true, out ex));
				if (ex is not null) throw ex;
			}

			public void Dispose() { handle.Dispose(); }

			private sealed class VirtualChrootDirectoryEntry : DirectoryEntry {
				public VirtualChrootDirectoryEntry(string path, FileType nodeHint, bool follow, VirtualChroot chroot)
					: base (path,
							follow ? FileSystem.FollowSymbolicLinks.Always : FileSystem.FollowSymbolicLinks.Never,
							nodeHint, FileType.LinkTargetHintNotAvailable) { this.chroot = chroot; }

				private readonly VirtualChroot chroot;

				protected override DirectoryEntry _ResolveSymbolicLink() => new VirtualChrootDirectoryEntry(Path, FileType.NodeHintNotAvailable, true, chroot);

				protected override void _Refresh()
				{
					using (var dir = chroot.Traverse(Path, false, FollowSymbolicLinks != FileSystem.FollowSymbolicLinks.Never, false, out var finalpath))
						_Refresh(finalpath, FileSystem.FollowSymbolicLinks.Never);
				}
			}
		}

#elif OS_WIN
		private static string MakeAbsolute(string path)
		{
				if (path[0] != '\\' && path[0] != '/' &&
						(path.Length == 1 || path[1] != ':' && (path[0] >= 'A' && path[0] <= 'Z' || path[0] >= 'a' && path[0] <= 'z')))
					path = "\\" + path;
				return path;
		}

		private static FileStream OpenFileStream(string patharg, string path, FileMode fileMode, FileAccess fileAccess, bool follow, bool forAsyncAccess, int bufferSize)
		{
			uint access;
			DecodeFileAccess(fileMode, ref fileAccess, out access);
			if (!follow) access |= NativeMethods.FILE_READ_ATTRIBUTES;
			SafeFileHandle safe = null;
			FileStream stream = null;
			IntPtr handle = IOErrors.InvalidFileHandle;
			try {
				try {} finally {
					handle = NativeMethods.CreateFileW(patharg, access, (uint)(FileShare.ReadWrite | FileShare.Delete), IntPtr.Zero, (uint)fileMode,
							(forAsyncAccess ? NativeMethods.FILE_FLAG_OVERLAPPED : 0) | (follow ? 0 : NativeMethods.FILE_FLAG_OPEN_REPARSE_POINT),
							IOErrors.InvalidFileHandle);
				}
				if (handle == IOErrors.InvalidFileHandle)
					throw GetExceptionFromLastError(patharg, path, false, 0, true);
				if (!follow) {
					int status = NativeMethods.NtQueryInformationFile(handle, out NativeMethods.IO_STATUS_BLOCK sb,
							out NativeMethods.FILE_BASIC_INFORMATION bi,
							Marshal.SizeOf<NativeMethods.FILE_BASIC_INFORMATION>(), NativeMethods.FileBasicInformation);
					if (status != 0) throw GetExceptionFromNtStatus(status, path, false, 0, false);
					if ((bi.FileAttributes & NativeMethods.FILE_ATTRIBUTE_REPARSE_POINT) != 0) {
						NativeMethods.SetLastError(IOErrors.OperationNotSupportedOnSymbolicLink & 0xFFFF);
						throw GetExceptionFromLastError(path, patharg, false, 0, true);
					}
				}
				safe = new SafeFileHandle((IntPtr)handle, true);
				stream = new FileStream(safe, fileAccess, bufferSize, forAsyncAccess);
			} finally {
				if (safe is null && handle != IOErrors.InvalidFileHandle) NativeMethods.CloseHandle(handle);
				if (stream is null && safe is not null) safe.Dispose();
			}
			return stream;
		}

		private static void DecodeFileAccess(FileMode fileMode, ref FileAccess fileAccess, out uint access)
		{
			switch (fileAccess) {
				case FileAccess.Read: access = NativeMethods.FILE_READ_DATA | NativeMethods.SYNCHRONIZE; break;
				case FileAccess.Write: access = NativeMethods.FILE_WRITE_DATA | NativeMethods.SYNCHRONIZE; break;
				case FileAccess.ReadWrite: access = NativeMethods.FILE_READ_DATA | NativeMethods.FILE_WRITE_DATA | NativeMethods.SYNCHRONIZE; break;
				default: throw new ArgumentOutOfRangeException("fileAccess");
			}
			if (fileMode == (FileMode)6 /* append */) {
				fileMode = FileMode.Create;
				if ((access & NativeMethods.FILE_WRITE_DATA) != 0) access = (access & ~NativeMethods.FILE_WRITE_DATA) | NativeMethods.FILE_APPEND_DATA;
			} else if ((uint)fileMode > 5)
				throw new ArgumentOutOfRangeException("fileMode");
		}

		private static SafeFileHandle GetDirectoryHandle(string chkpath, string path, bool forEnumeration, bool follow)
			=> GetDirectoryHandle(chkpath, path, true, forEnumeration, follow);

		private static SafeFileHandle GetDirectoryHandle(string chkpath, string path, bool allowRename, bool forEnumeration, bool follow)
		{
			uint access = NativeMethods.FILE_READ_ATTRIBUTES | NativeMethods.SYNCHRONIZE;
			if (forEnumeration) access |= NativeMethods.FILE_LIST_DIRECTORY;
			uint flags = NativeMethods.FILE_FLAG_BACKUP_SEMANTICS;
			uint share = allowRename ? (uint)(FileShare.ReadWrite | FileShare.Delete) : (uint)(FileShare.ReadWrite);
			if (!follow) flags |= NativeMethods.FILE_FLAG_OPEN_REPARSE_POINT;
			IntPtr handle = IOErrors.InvalidFileHandle;
			SafeFileHandle safeHandle = null;
			SafeFileHandle rtn = null;
			try {
				handle = NativeMethods.CreateFileW(chkpath, access, share, IntPtr.Zero, NativeMethods.OPEN_EXISTING, flags, IOErrors.InvalidFileHandle);
				if (handle == IOErrors.InvalidFileHandle) throw GetExceptionFromLastError(chkpath, path, false, 0, false);
				safeHandle = new SafeFileHandle(handle, true);
				handle = IOErrors.InvalidFileHandle;
				// So the problem is this might not be a directory
				if (new FileSystemNode(safeHandle).FileType != FileType.Directory)
					throw GetExceptionFromErrno(IOErrors.IsNotADirectory, chkpath, path,
							new System.ComponentModel.Win32Exception(IOErrors.IsNotADirectory).Message);
				rtn = safeHandle;
				safeHandle = null;
			} finally {
				if (safeHandle is null && handle != IOErrors.InvalidFileHandle) NativeMethods.CloseHandle(handle);
				if (safeHandle is not null) safeHandle.Dispose();
			}
			return rtn;
		}

		private class VirtualChroot : IDiskVirtualFileSystem {
			public bool SupportsCreationTime => true;
			public bool SupportsAccessTime => true;
			public bool SupportsHardLinks => true;
			public bool SupportsSymbolicLinks => true;
			public bool SupportsDirectoryHandles => true;
			public char DirectorySeparatorCharacter => '\\';
			public char AlternateDirectorySeparatorCharacter => '/';
			public string CurrentDirectoryName => ".";
			public string ParentDirectoryName => "..";
			public string RootDirectoryName => "\\";

			private readonly string root;
			private readonly SafeFileHandle handle;
			private readonly IDisposable chain;

			public VirtualChroot(string path, SafeFileHandle directory) { root = path; handle = directory; }

			private VirtualChroot(string path, ref IDisposable parents, ref SafeFileHandle directory) {
				root = path;
				chain = parents;
				handle = directory;
				parents = null;
				directory = null;
			}

			IVirtualFileSystem IVirtualFileSystem.CreateChild(string path) => CreateChild(path);
			public Stream OpenVirtualFile(string path, FileMode fileMode, FileAccess fileAccess, bool forAsyncAccess = false, int bufferSize = 4096)
				=> OpenFile(path, fileMode, fileAccess, FileSystem.FollowSymbolicLinks.Always, forAsyncAccess, bufferSize);

			public IDiskVirtualFileSystem CreateChild(string path) => CreateChild(path, FileSystem.FollowSymbolicLinks.Always);

			public IDiskVirtualFileSystem CreateChild(string path, FileSystem.FollowSymbolicLinks followSymbolicLinks)
			{
				SafeFileHandle dir = null;
				IDisposable chain = null;
				try {
					chain = Traverse(path, false, followSymbolicLinks == FileSystem.FollowSymbolicLinks.Always, out var finalPath);
					dir = GetDirectoryHandle(finalPath, path, false, false, false);
					return new VirtualChroot(finalPath, ref chain, ref dir);
				} finally {
					dir?.Dispose();
					chain?.Dispose();
				}
			}

			IDisposable Traverse(string path, bool mkdir, bool traverseFinal, out string finalPath)
			{
				HandleChain collective = null;
				HandleChain rtn = null;
				try {
					collective = new HandleChain(handle);
					string activepath = root;
					string localpath = path;
					int lastindex = 0;
					int linkcount = 0;
					if (path[0] == '/' || path[0] == '\\') {
						if (path.Length == 1) { finalPath = root; rtn = collective; collective = null; return rtn; }
						if (path[1] == '/' || path[1] == '\\')
							throw new ArgumentException("paths do not contain adjacient directory separator characters");
						lastindex = 1;
					}
					int index = Advance(localpath, lastindex);
					string component = null;
					for (;;) {
						component = localpath.Substring(lastindex, index - lastindex);
						if (index == localpath.Length && !traverseFinal)
							break;
						else if (component == "" || component == ".") {
							lastindex = index + 1;
							if (lastindex >= localpath.Length) break;
							index = Advance(localpath, lastindex);
						} else if (component == "..") {
							if (collective.Count > 0) {
								collective.Pop().Dispose();
								activepath = Path.GetDirectoryName(activepath);
								localpath = Path.GetDirectoryName(localpath);
							}
							lastindex = index + 1;
							if (lastindex >= localpath.Length) break;
							index = Advance(localpath, lastindex);
						} else {
							var local2 = Path.Combine(activepath, component);
							var attr = NativeMethods.GetFileAttributes(local2);
							if (index == localpath.Length && (attr == NativeMethods.INVALID_FILE_ATTRIBUTES || (attr & NativeMethods.FILE_ATTRIBUTE_REPARSE_POINT) == 0)) break;
							if (attr == NativeMethods.INVALID_FILE_ATTRIBUTES && mkdir && Marshal.GetLastWin32Error() == (IOErrors.FileNotFound & 0xFFFF)) {
								if (0 != NativeMethods.CreateDirectory(local2, IntPtr.Zero)) attr = NativeMethods.FILE_ATTRIBUTE_DIRECTORY;
							}
							if (attr == NativeMethods.INVALID_FILE_ATTRIBUTES) throw GetExceptionFromLastError(path, path, false, 0, false);
							if ((attr & NativeMethods.FILE_ATTRIBUTE_REPARSE_POINT) != 0) {
								if (++linkcount == 30)
									throw GetExceptionFromErrno(IOErrors.TooManySymbolicLinks, path, path,
											new System.ComponentModel.Win32Exception(IOErrors.TooManySymbolicLinks).Message);
								var path2 = FileSystem.ReadLink(local2);
								if (IsAbsoluteTextSkip(path2, out path2)) {
									while (collective.Count > 0) collective.Pop().Dispose();
									activepath = root;
								}
								if (index == localpath.Length)
									localpath = path2;
								else if (index == localpath.Length - 1)
									throw new ArgumentException("paths do not end in directory separator characters");
								else
									localpath = Path.Combine(path2, localpath.Substring(index + 1));
								lastindex = 0;
								index = Advance(localpath, 0);
							}
							else if (index == localpath.Length)
								break;
							else if ((attr & NativeMethods.FILE_ATTRIBUTE_DIRECTORY) != 0) {
								SafeFileHandle h2 = null;
								try {
									h2 = GetDirectoryHandle(local2, path, false, false, false);
									collective.Add(h2);
									h2 = null;
								} finally {
									h2?.Dispose();
								}
								lastindex = index + 1;
								index = Advance(localpath, lastindex);
								activepath = Path.Combine(activepath, component);
							} else
								throw GetExceptionFromErrno(IOErrors.IsNotADirectory, path, path, new System.ComponentModel.Win32Exception(IOErrors.IsNotADirectory & 0xFFFF).Message);
						}
					}
					rtn = collective;
					collective = null;
					finalPath = (component is null || component == "." || component == "..") ? activepath
								 : Path.Combine(activepath, component);
				} finally {
					collective?.Dispose();
				}
				return rtn;
			}

			private sealed class HandleChain : List<SafeFileHandle>, IDisposable {
				public HandleChain(SafeFileHandle root) { this.root = root; root.DangerousAddRef(ref gothandle); }
				public SafeFileHandle Pop() {
					SafeFileHandle rtn = this[Count - 1];
					this.RemoveAt(Count - 1);
					return rtn;
				}
				public void Dispose() {
					for (int i = Count; i --> 0;)
						this[i].Dispose();
					if (gothandle) { gothandle = false; root.DangerousRelease(); }
				}
				private SafeFileHandle root;
				private bool gothandle;
			}

			private static int Advance(string path, int index)
			{
				for (; index < path.Length; index++)
					if (path[index] == '/' || path[index] == '\\')
						break;
				return index;
			}

			private static bool IsAbsoluteTextSkip(string path, out string relpath) {
				if (path.Length >= 4 && (path[0] == '\\' || path[0] == '/') && path[1] == '?' && path[2] == '?' && (path[3] == '\\' || path[3] == '/')) {
					if (path.Length < 7 || path[5] != ':')
						if (path.StartsWith("\\??\\UNC"))
							throw new NotSupportedException("Tried to traverse a symbolic link to a network share inside a VirtualChroot jail");
						else
							throw new InvalidOperationException("Got a \\??\\ link that does not start with a drive letter");
					relpath = path.Substring(7);
				}
				if (path.Length == 0) { relpath = path; return false; }
				if (path[0] == '\\' || path[0] == '/') {
					if (path.Length > 1 && (path[1] == '/' || path[1] == '\\'))
						throw new NotSupportedException("Tried to traverse a symbolic link to a network share inside a VirtualChroot jail");
					relpath = path.Substring(1);
					return true;
				}
				if (path.Length > 1 && path[1] == ':') {
					if (path.Length == 2 || (path[2] != '\\' && path[2] != '/'))
						throw new NotSupportedException("Tried to traverse a drive letter relative symbolic link");
					relpath = path.Substring(3);
					return true;
				}
				relpath = path;
				return false;
			}

			public bool CreateDirectory(string path)
			{
				if (path is null) throw new ArgumentNullException("path");
				if (path.Length == 0) throw new ArgumentOutOfRangeException("path", "path cannot be the empty string.");
				using (var traversal = Traverse(path, true, false, out string final)) {
					if (0 == NativeMethods.CreateDirectory(final, IntPtr.Zero)) {
						if (Marshal.GetLastWin32Error() == (IOErrors.AlreadyExists & 0xFFFF)) return false;
						throw GetExceptionFromLastError(final, path, false, 0, true);
					}
				}
				return true;
			}

			public void CreateHardLink(string targetpath, string linkpath)
			{
				if (targetpath is null) throw new ArgumentNullException("targetpath");
				if (targetpath.Length == 0) throw new ArgumentOutOfRangeException("targetpath", "targetpath must not be the empty string");
				if (linkpath is null) throw new ArgumentNullException("linkpath");
				if (linkpath.Length == 0) throw new ArgumentOutOfRangeException("linkpath", "linkpath must not be the empty string");
				using (var ttarget = Traverse(targetpath, false, true, out var finaltarget)) // Windows resolves through symbolic links (!!)
				using (var tlink = Traverse(linkpath, false, false, out var finallink))
					FileSystem.CreateHardLink(finaltarget, finallink);
			}

			public void CreateSymbolicLink(string targetpath, string linkpath, FileType linkTargetType = FileType.LinkTargetHintNotAvailable)
			{
				if (targetpath is null) throw new ArgumentNullException("targetpath");
				if (targetpath.Length == 0) throw new ArgumentOutOfRangeException("targetpath", "targetpath must not be the empty string");
				if (linkpath is null) throw new ArgumentNullException("linkpath");
				if (linkpath.Length == 0) throw new ArgumentOutOfRangeException("linkpath", "linkpath must not be the empty string");
				if (linkTargetType != FileType.File && linkTargetType != FileType.Directory && linkTargetType != FileType.LinkTargetHintNotAvailable)
					throw new ArgumentOutOfRangeException("linkTargetType", "link target type must be File, Directory, or Not Available (autodetect)");
				if (linkTargetType == FileType.LinkTargetHintNotAvailable) {
					string targetpath2;
					if (targetpath[0] == '/' || targetpath[0] == '\\') {
						targetpath2 = targetpath;
					} else {
						int offset = linkpath.Length;
						while (offset > 0 && linkpath[offset - 1] != '/' && linkpath[offset - 1] != '\\') --offset;
						targetpath2 = linkpath.Substring(0, offset) + targetpath;
					}
					using (var traversal2 = Traverse(targetpath2, false, false, out var finaltarget)) {
						var attr = NativeMethods.GetFileAttributes(finaltarget);
						if (attr == NativeMethods.INVALID_FILE_ATTRIBUTES)
							throw GetExceptionFromLastError(finaltarget, targetpath, false, 0, false);
						linkTargetType = ((attr & NativeMethods.FILE_ATTRIBUTE_DIRECTORY) != 0) ? FileType.Directory : FileType.File;
					}
				}
				using (var traversal = Traverse(linkpath, false, false, out var finallink))
					FileSystem.CreateSymbolicLink(targetpath, finallink, linkTargetType);
			}

			public DirectoryEntry GetDirectoryEntry(string path)
			{
				if (path is null) throw new ArgumentNullException("path");
				if (path.Length == 0) throw new ArgumentOutOfRangeException("path", "path cannot be the empty string.");
				return new VirtualChrootDirectoryEntry(this, path, false);
			}

			public IEnumerable<DirectoryEntry>? GetDirectoryContents(string path, FileSystem.NonExtantDirectoryBehavior nonExtantDirectoryBehavior)
			{
				if (path is null) throw new ArgumentNullException("path");
				if (path.Length == 0) throw new ArgumentOutOfRangeException("path", "path cannot be the empty string.");
				using (var traversal = Traverse(path, false, true, out var final))
				{
					var slist = FileSystem.GetDirectoryContents(final, nonExtantDirectoryBehavior);
					if (slist is null) return null;
					var rtn = new FileSystem.DirectoryEntryList();
					foreach (var entry in slist)
						rtn.Add(new VirtualChrootDirectoryEntry(this, Path.Combine(path, entry.Name), entry));
					return rtn;
				}
			}

			public SafeFileHandle OpenDirectory(string path, FileSystem.FollowSymbolicLinks followSymbolicLinks, bool requestEnumeration = false)
			{
				if (path is null) throw new ArgumentNullException("path");
				if (path.Length == 0) throw new ArgumentOutOfRangeException("path", "path cannot be the empty string.");
				using (var traversal = Traverse(path, false, followSymbolicLinks == FileSystem.FollowSymbolicLinks.Always, out var final))
					return GetDirectoryHandle(final, path, requestEnumeration, false);
			}

			public FileStream OpenFile(string path, FileMode fileMode, FileAccess fileAccess, FileSystem.FollowSymbolicLinks followSymbolicLinks, bool forAsyncAccess = false, int bufferSize = 4096)
			{
				if (path is null) throw new ArgumentNullException("path");
				if (path.Length == 0) throw new ArgumentOutOfRangeException("path", "path cannot be the empty string.");
				using (var traversal = Traverse(path, false, followSymbolicLinks != FileSystem.FollowSymbolicLinks.Never, out string final))
					return OpenFileStream(final, path, fileMode, fileAccess, false, forAsyncAccess, bufferSize);
			}

			public void RenameReplace(string oldname, string newname)
			{
				if (oldname is null) throw new ArgumentNullException("oldname");
				if (oldname.Length == 0) throw new ArgumentOutOfRangeException("oldname cannot be the empty string");
				if (newname is null) throw new ArgumentNullException("newname");
				if (newname.Length == 0) throw new ArgumentOutOfRangeException("newname cannot be the empty string");
				using (var traverseold = Traverse(oldname, false, false, out string finalold))
				using (var traversenew = Traverse(newname, false, false, out string finalnew))
					FileSystem.RenameReplace(finalold, finalnew);
			}

			public string ReadLink(string path)
			{
				if (path is null) throw new ArgumentNullException("path");
				if (path.Length == 0) throw new ArgumentOutOfRangeException("path", "path cannot be the empty string.");
				using (var traversal = Traverse(path, false, false, out string final))
					return FileSystem.ReadLink(final);
			}

			public bool RemoveFile(string path)
			{
				if (path is null) throw new ArgumentNullException("path");
				if (path.Length == 0) throw new ArgumentOutOfRangeException("path", "path cannot be the empty string.");
				using (var traversal = Traverse(path, false, false, out string final))
					return FileSystem.RemoveFile(final);
			}

			public bool RemoveDirectory(string path, bool recurse = false)
			{
				if (path is null) throw new ArgumentNullException("path");
				if (path.Length == 0) throw new ArgumentOutOfRangeException("path", "path cannot be the empty string.");
				using (var traversal = Traverse(path, false, false, out string final))
					return FileSystem.RemoveDirectory(final, recurse); // Attempting to remove \ will fail with in use.
			}

			public void Dispose() { handle.Dispose(); chain?.Dispose(); }

			private sealed class VirtualChrootDirectoryEntry : DirectoryEntry {
				public VirtualChrootDirectoryEntry(VirtualChroot owner, string path, bool follow)
					: base(path, follow ? FileSystem.FollowSymbolicLinks.Always : FileSystem.FollowSymbolicLinks.Never)
				{
					this.owner = owner;
				}

				internal VirtualChrootDirectoryEntry(VirtualChroot owner, string path, DirectoryEntry source)
					: base(path, FileSystem.FollowSymbolicLinks.Never)
				{
					this.owner = owner;
					FillFindDataResult(source);
					SetLinkTargetHint(source.LinkTargetHint);
				}

				protected override DirectoryEntry _ResolveSymbolicLink() => new VirtualChrootDirectoryEntry(owner, Path, true);

				protected override void _Refresh()
				{
					try {
						using (var traversal = owner.Traverse(Path, false, FollowSymbolicLinks == FileSystem.FollowSymbolicLinks.Always, out string final))
						{
							var source = new DirectoryEntry(final, FileSystem.FollowSymbolicLinks.Never);
							if (source.ErrorCode == 0) {
								RefreshSucceeded(source.FileType, source.FileSize, source.BytesUsed, source.DeviceNumber, source.InodeNumber, source.LinkCount,
									source.CreationTimeUTC, source.LastModificationTimeUTC, source.LastChangeTimeUTC, source.LastAccessTimeUTC);
								SetLinkTargetHint(source.LinkTargetHint);
							} else {
								RefreshFailed(source.ErrorCode);
							}
						}
					} catch (IOException ex) when (!ShouldThrowExceptionForRefreshError(ex.HResult))
					{
						RefreshFailed(ex.HResult);
					}
				}

				private VirtualChroot owner;
			}
		}

#else
		// Must be reference assembly
		private static string MakeAbsolute(string path) => throw null;

		///<summary>Creates a virtual file system that is the system from here</summary>
		///<returns>a virtualization of the given directory</returns>
		///<exception cref="System.IO.DirectoryNotFoundException">The requested path does not exist</exception>
		///<exception cref="System.IO.IOException">an error occurred opening the path</exception>
		public static IDiskVirtualFileSystem VirtualizeChrootDirectory(string path) => throw null;
#endif
	}
}
