/* vi:ts=2
 */

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Emet.FileSystems {
	///<summary>Describes a DirectoryEntry</summary>
	///<remarks>If the DirectoryEntry was constructed with a relative path and the current directory was since changed, the object invokes unspecified (usually bad) behavior until Refresh() is called the first time, at which point it uses the current directory as of the most recent call to Refresh() reliably</remarks>
public class DirectoryEntry : FileSystemNode {
		///<summary>Constructs a new directory entry from a file path</summary>
		///<param name="path">The path to the directory entry to inspect</param>
		///<param name="symbolicLinkBehavior">What to do if path is a symbolic link</param>
		public DirectoryEntry(string path, FileSystem.FollowSymbolicLinks symbolicLinkBehavior)
			: this(path, symbolicLinkBehavior, FileType.NodeHintNotAvailable, FileType.LinkTargetHintNotAvailable) {}
		
		///<summary>Constructs a new directory entry from a directory path and file name</summary>
		///<param name="directoryname">The name of the directory containing the entry to inspect</param>
		///<param name="name">The name of the node to inspect</param>
		///<param name="symbolicLinkBehavior">What to do if path is a symbolic link</param>
		public DirectoryEntry(string directoryname, string name, FileSystem.FollowSymbolicLinks symbolicLinkBehavior)
			: this(directoryname, name, symbolicLinkBehavior, FileType.NodeHintNotAvailable, FileType.LinkTargetHintNotAvailable) {}

		internal DirectoryEntry(string path, FileSystem.FollowSymbolicLinks symbolicLinkBehavior, FileType type, FileType hint) : base(null)
		{
			if (path is null) throw new ArgumentNullException("path cannot be null");
			if (path.Length == 0) throw new ArgumentException("path cannot be the empty string");
			this.path = path;
			this.symbolic = symbolicLinkBehavior;
			this.type = type;
			this.hint = hint;
		}

		internal DirectoryEntry(string directoryname, string name, FileSystem.FollowSymbolicLinks symbolicLinkBehavior, FileType type, FileType hint) : base(null)
		{
			if (string.IsNullOrEmpty(directoryname)) directoryname = CurrentDirectoryName;
			if (name is null) throw new ArgumentNullException("name cannot be null");
			if (name.Length == 0) throw new ArgumentException("name cannot be the empty string");
			this.directory = directoryname;
			this.name = name;
			this.symbolic = symbolicLinkBehavior;
			this.type = type;
			this.hint = hint;
			if (type == FileType.SymbolicLink && symbolic != FileSystem.FollowSymbolicLinks.Never)
				this.type = FileType.NodeHintNotAvailable;
		}

#if OS_WIN
		internal DirectoryEntry(string directoryname, string filename, FileSystem.FollowSymbolicLinks symbolicLinkBehavior, ref NativeMethods.WIN32_FIND_DATA ff) : base(null)
		{
			this.directory = directoryname;
			this.name = filename;
			this.symbolic = symbolicLinkBehavior;
			this.type = FileAttributesToFileType(ff.dwFileAttributes, ff.dwReserved0);
			this.hint = (type == FileType.SymbolicLink || type == FileType.ReparsePoint)
				? ((ff.dwFileAttributes & NativeMethods.FILE_ATTRIBUTE_DIRECTORY) == 0 ? FileType.File : FileType.Directory)
				: type;
			if (type == FileType.SymbolicLink && (symbolic == FileSystem.FollowSymbolicLinks.Always
					|| (symbolic == FileSystem.FollowSymbolicLinks.IfNotDirectory && hint != FileType.Directory))) {
				_Refresh();
			} else {
				FillFindDataResult(ref ff);
			}
		}
#endif

		private FileType type;
		private FileType hint;
		private FileSystem.FollowSymbolicLinks symbolic;
		private string name;
		private string directory;
		private string path;
		private DirectoryEntry linkTarget;

		///<summary>Gets the name of the directory entry within the directory</summary>
		public string Name {
			get {
				if (name is null)
					name = System.IO.Path.GetFileName(path);
				return name;
			}
		}

		///<summary>Gets the name of the directory containing the node</summary>
		public string DirectoryName {
			get {
				if (directory is null) {
					directory = System.IO.Path.GetDirectoryName(path);
					if (string.IsNullOrEmpty(directory))
						directory = CurrentDirectoryName;
				}
				return directory;
			}
		}

		///<summary>Gets the path (absolute or relative) to the directory entry</summary>
		public string Path {
			get {
				if (path is null)
					path = System.IO.Path.Combine(directory, name);
				return path;
			}
		}

		///<summary>Gets the symbolic link behavior the DirectoryEntry was constructed with</summary>
		public FileSystem.FollowSymbolicLinks FollowSymbolicLinks => symbolic;

		///<summary>Gets the type of the FileSystemNode</summary>
		public override FileType FileType => (type == FileType.NodeHintNotAvailable) ? base.FileType : type;

		///<summary>Gets the link target hint, if available</summary>
		///<remarks>All callers must be prepared to handle LinkTargetHintNotAvailable; you probably want LinkTargetType instead</remarks>
		public FileType LinkTargetHint => hint;

		///<summary>Gets the type of the symbolic link target</summary>
		public FileType LinkTargetType => (hint == FileType.LinkTargetHintNotAvailable) ? LinkTarget.FileType : hint;

		public FileSystemNode LinkTarget {
			get {
				if (FileType != FileType.SymbolicLink) return this;
				if (linkTarget is null) linkTarget = ResolveSymbolicLink();
				return linkTarget;
			}
		}

		///<summary>Returns an instanteous view of the symbolic link resolution of this node</summary>
		///<remarks>If this is not a symbolic link, returns a new copy of the node itself</remarks>
		public DirectoryEntry ResolveSymbolicLink() =>
				new DirectoryEntry(Path, FileSystem.FollowSymbolicLinks.Always,
					// Passing the hint through happens to do the right thing on all platforms
					hint == FileType.LinkTargetHintNotAvailable ? FileType.NodeHintNotAvailable : hint, FileType.LinkTargetHintNotAvailable);

		protected override void _Refresh()
		{
#if OSTYPE_UNIX
			int cresult;
			var arg = FileSystem.NameToByteArray(Path);
#if OS_LINUXX64
			var statbuf = new NativeMethods.statbuf64();
			if (symbolic == FileSystem.FollowSymbolicLinks.Always)
				cresult = NativeMethods.__xstat64(NativeMethods.statbuf_version, arg, out statbuf);
			else
				cresult = NativeMethods.__lxstat64(NativeMethods.statbuf_version, arg, out statbuf);
#else
			var statbuf = new NativeMethods.statbuf();
			if (symbolic == FileSystem.FollowSymbolicLinks.Always)
				cresult = NativeMethods.stat(arg, out statbuf);
			else
				cresult = NativeMethods.lstat(arg, out statbuf);
#endif
			bool followerror = false;
			if (symbolic == FileSystem.FollowSymbolicLinks.IfNotDirectory && cresult == 0 &&
					((statbuf.st_mode & FileTypeMask) == (int)FileType.SymbolicLink)) {
#if OS_LINUXX64
				var statbuf2 = new NativeMethods.statbuf64();
				cresult = NativeMethods.__xstat64(NativeMethods.statbuf_version, arg, out statbuf2);
#else
				var statbuf2 = new NativeMethods.statbuf();
				cresult = NativeMethods.stat(arg, out statbuf2);
#endif
				if (cresult == 0) {
					if (symbolic == FileSystem.FollowSymbolicLinks.Always ||
							((statbuf2.st_mode & FileTypeMask) != (int)FileType.Directory))
						statbuf = statbuf2;
				} else
					followerror = true;
			}
			if (cresult != 0) {
				var errno = (int)Marshal.GetLastWin32Error();
				if (!IsPassError(errno)) {
					var ci = new System.ComponentModel.Win32Exception();
					throw new IOException(ci.Message, errno);
				}
				if (!followerror) {
					Clear();
					type = FileType.DoesNotExist;
					hint = FileType.DoesNotExist;
					return ;
				}
			}
			FillStatResult(ref statbuf);
			type = (FileType)(statbuf.st_mode & FileTypeMask);
			hint = FileType.LinkTargetHintNotAvailable;
#elif OS_WIN
			IntPtr handle = NativeMethods.INVALID_HANDLE_VALUE;
			try {
				handle = NativeMethods.CreateFileW(Path, NativeMethods.FILE_READ_ATTRIBUTES,
						NativeMethods.FILE_SHARE_ALL, IntPtr.Zero, NativeMethods.OPEN_EXISTING,
						(symbolic == FileSystem.FollowSymbolicLinks.Always
							|| symbolic == FileSystem.FollowSymbolicLinks.IfNotDirectory && hint == FileType.File)
						? NativeMethods.FILE_FLAG_BACKUP_SEMANTICS
						: NativeMethods.FILE_FLAG_BACKUP_SEMANTICS | NativeMethods.FILE_FLAG_OPEN_REPARSE_POINT,
						IntPtr.Zero);
				if (handle == NativeMethods.INVALID_HANDLE_VALUE) {
					var errno = unchecked((int)0x80070000 | (int)Marshal.GetLastWin32Error());
					if (errno != IOErrors.DeletePending && !IsPassError(errno)) {
						var ci = new System.ComponentModel.Win32Exception();
						throw new IOException(ci.Message, errno);
					}
					IntPtr handle2 = NativeMethods.INVALID_HANDLE_VALUE;
					try {
						var ff = new NativeMethods.WIN32_FIND_DATA();
						handle2 = NativeMethods.FindFirstFileW(Path, out ff);
						if (handle2 != NativeMethods.INVALID_HANDLE_VALUE) {
							FillFindDataResult(ref ff);
							FillMakeStuffUpBecauseInaccessible();
						} else {
							var errno2 = (int)Marshal.GetLastWin32Error();
							if (!IsPassError(errno2)) {
								var ci = new System.ComponentModel.Win32Exception();
								throw new IOException(ci.Message, errno2);
							}
							Clear();
							type = FileType.DoesNotExist;
							hint = FileType.DoesNotExist;
						}
					} finally {
						if (handle2 != NativeMethods.INVALID_HANDLE_VALUE) NativeMethods.FindClose(handle2);
					}
					return;
				}
				var typeraw = LoadFromHandle(handle);
				type = FileAttributesToFileType(typeraw, 0);
				if (symbolic == FileSystem.FollowSymbolicLinks.IfNotDirectory &&
					(typeraw & (NativeMethods.FILE_ATTRIBUTE_REPARSE_POINT | NativeMethods.FILE_ATTRIBUTE_DIRECTORY))
						== NativeMethods.FILE_ATTRIBUTE_REPARSE_POINT) {
					hint = FileType.File;
					_Refresh(); // Easiest way to redo stats load
				}
			} finally {
				if (handle != NativeMethods.INVALID_HANDLE_VALUE) NativeMethods.CloseHandle(handle);
			}
#else
			throw null;
#endif
		}

		internal static bool IsPassError(int errno)
			=> (errno == IOErrors.FileNotFound || errno == IOErrors.PathNotFound
				|| errno == IOErrors.IsADirectory || errno == IOErrors.IsNotADirectory
				|| errno == IOErrors.BadPathName || errno == IOErrors.TooManySymbolicLinks
				|| errno == IOErrors.PermissionDenied);
	}
}
