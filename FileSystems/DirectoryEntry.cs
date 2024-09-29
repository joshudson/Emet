/* vi:ts=2
 */

using System;
using System.IO;
using System.Runtime.InteropServices;
using static Emet.FileSystems.Util;

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
			if (path is null) throw new ArgumentNullException("path");
			if (path.Length == 0) throw new ArgumentException("path cannot be the empty string");
			this.path = path;
			this.symbolic = symbolicLinkBehavior;
			this.type = type;
			this.hint = hint;
		}

		internal DirectoryEntry(string directoryname, string name, FileSystem.FollowSymbolicLinks symbolicLinkBehavior, FileType type, FileType hint) : base(null)
		{
			if (string.IsNullOrEmpty(directoryname)) directoryname = CurrentDirectoryName;
			if (name is null) throw new ArgumentNullException("name");
			if (name.Length == 0) throw new ArgumentException("name cannot be the empty string");
			this.directory = directoryname;
			this.name = name;
			this.symbolic = symbolicLinkBehavior;
			this.type = type;
			this.hint = hint;
			if (type == FileType.SymbolicLink && symbolic != FileSystem.FollowSymbolicLinks.Never)
				this.type = FileType.NodeHintNotAvailable; // Forces resolution on accessing FileType
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

		///<summary>provides the path for generating error messages</summary>
		///<remarks>may not return null, nor the empty string</remarks>
		protected override string ErrorPath => Path;

		///<summary>Gets the symbolic link behavior the DirectoryEntry was constructed with</summary>
		public FileSystem.FollowSymbolicLinks FollowSymbolicLinks => symbolic;

		///<summary>Gets the type of the FileSystemNode</summary>
		public override FileType FileType => (type == FileType.NodeHintNotAvailable) ? base.FileType : type;

		///<summary>Gets the link target hint, if available</summary>
		///<remarks>All callers must be prepared to handle LinkTargetHintNotAvailable; you probably want LinkTargetType instead</remarks>
		public FileType LinkTargetHint => hint;

		///<summary>Gets the type of the symbolic link target</summary>
		public FileType LinkTargetType => (hint == FileType.LinkTargetHintNotAvailable) ? LinkTarget.FileType : hint;

		///<summary>Gets a file system node traverses the link</summary>
		///<remarks>returns this if the node is not a symbolic link</remarks>
		public FileSystemNode LinkTarget {
			get {
				if (FileType != FileType.SymbolicLink) return this;
				if (linkTarget is null) linkTarget = ResolveSymbolicLink();
				return linkTarget;
			}
		}

		///<summary>Returns an instanteous view of the symbolic link resolution of this node</summary>
		///<remarks>If this is not a symbolic link, returns a new copy of the node itself</remarks>
		public DirectoryEntry ResolveSymbolicLink() => _ResolveSymbolicLink();

		///<summary>Returns an instanteous view of the symbolic link resolution of this node</summary>
		///<remarks>If this is not a symbolic link, returns a new copy of the node itself</remarks>
		protected virtual DirectoryEntry _ResolveSymbolicLink() =>
				new DirectoryEntry(Path, FileSystem.FollowSymbolicLinks.Always,
					// Passing the hint through happens to do the right thing on all platforms
					hint == FileType.LinkTargetHintNotAvailable ? FileType.NodeHintNotAvailable : hint, FileType.LinkTargetHintNotAvailable);

		///<summary>Reloads the file node information</summary>
		///<exception cref="System.IO.IOException">A disk IO exception occurred resolving the node</exception>
		protected override void _Refresh() => _Refresh(Path);

#if OSTYPE_UNIX
		internal void _Refresh(byte[] realpath, FileSystem.FollowSymbolicLinks symbolic)
		{
			int cresult;
#if OS_LINUXX64
			var statbuf = new NativeMethods.statbuf64();
			do {
				if (symbolic == FileSystem.FollowSymbolicLinks.Always)
					cresult = NativeMethods.__xstat64(NativeMethods.statbuf_version, realpath, out statbuf);
				else
					cresult = NativeMethods.__lxstat64(NativeMethods.statbuf_version, realpath, out statbuf);
			} while (IsEIntrSyscallReturn(cresult));
#else
			var statbuf = new NativeMethods.statbuf();
			do {
				if (symbolic == FileSystem.FollowSymbolicLinks.Always)
					cresult = NativeMethods.stat(realpath, out statbuf);
				else
					cresult = NativeMethods.lstat(realpath, out statbuf);
			} while (IsEIntrSyscallReturn(cresult));
#endif
			bool followerror = false;
			if (symbolic == FileSystem.FollowSymbolicLinks.IfNotDirectory && cresult == 0 &&
					((statbuf.st_mode & FileTypeMask) == (int)FileType.SymbolicLink)) {
#if OS_LINUXX64
				var statbuf2 = new NativeMethods.statbuf64();
				do {
					cresult = NativeMethods.__xstat64(NativeMethods.statbuf_version, realpath, out statbuf2);
				} while (IsEIntrSyscallReturn(cresult));
#else
				var statbuf2 = new NativeMethods.statbuf();
				do {
					cresult = NativeMethods.stat(realpath, out statbuf2);
				} while (IsEIntrSyscallReturn(cresult));
#endif
				if (cresult == 0) {
					if (symbolic == FileSystem.FollowSymbolicLinks.Always ||
							((statbuf2.st_mode & FileTypeMask) != (int)FileType.Directory))
						statbuf = statbuf2;
				} else
					followerror = true;
			}
			if (cresult != 0) {
				var exception = GetExceptionFromLastError(Path, ErrorPath, true, 0, false);
				if (exception != null) throw exception;
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
		}
#endif

		internal void _Refresh(string realpath)
		{
#if OSTYPE_UNIX
			_Refresh(NameToByteArray(realpath), symbolic);
#elif OS_WIN
			IntPtr handle = IOErrors.InvalidFileHandle;
			try {
				handle = NativeMethods.CreateFileW(realpath, NativeMethods.FILE_READ_ATTRIBUTES,
						NativeMethods.FILE_SHARE_ALL, IntPtr.Zero, NativeMethods.OPEN_EXISTING,
						(symbolic == FileSystem.FollowSymbolicLinks.Always
							|| symbolic == FileSystem.FollowSymbolicLinks.IfNotDirectory && hint == FileType.File)
						? NativeMethods.FILE_FLAG_BACKUP_SEMANTICS
						: NativeMethods.FILE_FLAG_BACKUP_SEMANTICS | NativeMethods.FILE_FLAG_OPEN_REPARSE_POINT,
						IntPtr.Zero);
				if (handle == IOErrors.InvalidFileHandle) {
					var exception = GetExceptionFromLastError(Path, ErrorPath, true, IOErrors.DeletePending, false);
					if (exception != null) throw exception;
					IntPtr handle2 = IOErrors.InvalidFileHandle;
					try {
						var ff = new NativeMethods.WIN32_FIND_DATA();
						handle2 = NativeMethods.FindFirstFileW(realpath, out ff);
						if (handle2 != IOErrors.InvalidFileHandle) {
							FillFindDataResult(ref ff);
							FillMakeStuffUpBecauseInaccessible();
						} else {
							var exception2 = GetExceptionFromLastError(Path, ErrorPath, true, 0, false);
							if (exception2 != null) throw exception2;
							Clear();
							type = FileType.DoesNotExist;
							hint = FileType.DoesNotExist;
						}
					} finally {
						if (handle2 != IOErrors.InvalidFileHandle) NativeMethods.FindClose(handle2);
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
				if (handle != IOErrors.InvalidFileHandle) NativeMethods.CloseHandle(handle);
			}
#else
			throw null;
#endif
		}
	}
}
