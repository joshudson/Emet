/* vi:ts=2
 */

#if VIRTUAL_FS

using System;
using System.Collections.Generic;
using System.IO;

namespace Emet.FileSystems {

	///<summary>Interface for read-only file system access; by default a disk but could wrap arbitrary network file access</summary>
	public interface IReadOnlyVirtualFileSystem : IDisposable {
		///<summary>returns true if CreationTime is available</summary>
		bool SupportsCreationTime { get; }
		///<summary>returns true if AccessTime is available</summary>
		bool SupportsAccessTime { get; }
		///<summary>returns true if ReadLink is available</summary>
		bool SupportsSymbolicLinks { get; }

		///<summary>The directory separator character</summary>
		char DirectorySeparatorCharacter { get; }
		///<summary>The alternate directory separator character</summary>
		///<remarks>if this does not exist, the return value is DirectorySeparaterCharacter</remarks>
		char AlternateDirectorySeparatorCharacter { get; }
		///<summary>The name of the current directory</summary>
		///<remarks>almost always "."</remarks>
		string CurrentDirectoryName { get; }
		///<summary>The name of the parent directory</summary>
		///<remarks>almost always ".."</remarks>
		string ParentDirectoryName { get; }
		///<summary>The name of the root directory</summary>
		///<remarks>Almost always DirectorySeparatorCharacter</remarks>
		string RootDirectoryName { get; }

		///<summary>Gets information about a path in the virtual file system</summary>
		///<param name="path">The path to inquire of</param>
		///<exception cref="System.IO.DirectoryNotFoundException">the path was not found</exception>
		///<exception cref="System.IO.IOException">an error occurred opening the path</exception>
		///<exception cref="System.InvalidOperationException">A system constraint was found to be violated while descending the directory tree</exception>
		///<remarks>Does not throw file not found; a DirectoryEntry populated for non-extant file is returned instead.
		///Note that the returned DirectoryEntry retains a reference to the IVirtualFileSystem; if it is disposed, the DirectoryEntry is also disposed.
		///The properties can be pre-loaded by calling .Refresh() immediately.</remarks>
		DirectoryEntry GetDirectoryEntry(string path);

// THIS is the point of TO-DO for this interface; the main problem here is pre-statting every entry is too slow and so is re-traversing.
// Need to devise name filter first
		///<summary>Gets the contents of a directory</summary>
		///<param name="path">The path to the directory to enumerate</param>
		///<param name="nonExtantDirectoryBehavior">How to behave if the directory does not exist</param>
		///<exception cref="System.IO.DirectoryNotFoundException">The directory does not exist, and the directive is to throw</exception>
		///<exception cref="System.IO.IOException">an error occured opening the path</exception>
		///<exception cref="System.InvalidOperationException">A system constraint was found to be violated while descending the directory tree</exception>
		IEnumerable<DirectoryEntry>? GetDirectoryContents(string path, FileSystem.NonExtantDirectoryBehavior nonExtantDirectoryBehavior);

		///<summary>Opens an existing file, or creates a new one</summary>
		///<param name="path">The file to open</param>
		///<param name="forAsyncAccess">whether the file can be accessed asynchronously or not</param>
		///<param name="bufferSize">buffer size</param>
		///<returns>The System.IO.FileStream object for the file</returns>
		///<exception cref="System.IO.FileNotFoundException">the file was not found to open</exception>
		///<exception cref="System.IO.DirectoryNotFoundException">the path was not found</exception>
		///<exception cref="System.IO.IOException">an error occurred opening the path</exception>
		///<exception cref="System.InvalidOperationException">A system constraint was found to be violated while descending the directory tree</exception>
		Stream OpenVirtualFile(string path, bool forAsyncAccess = false, int bufferSize = 4096);

		///<summary>Reads a symbolic link</summary>
		///<param name="path">The path to the symbolic link to read</param>
		///<returns>The contents of the symbolic link</returns>
		///<exception cref="System.IO.FileNotFoundException">the file was not found to open</exception>
		///<exception cref="System.IO.DirectoryNotFoundException">the path was not found</exception>
		///<exception cref="System.IO.IOException">an error occurred</exception>
		///<exception cref="System.InvalidOperationException">A system constraint was found to be violated while descending the directory tree</exception>
		string ReadLink(string path);

		///<summary>Creates an IReadOnlyVirtualFileSystem with this subdirectory from the whole</summary>
		///<param name="path">The path to create the virtual filesystem within</param>
		///<exception cref="System.IO.DirectoryNotFoundException">path does not exist</exception>
		///<exception cref="System.IO.IOException">an error occurred opening the path</exception>
		///<exception cref="System.InvalidOperationException">A system constraint was found to be violated while descending the directory tree</exception>
		IReadOnlyVirtualFileSystem CreateChild(string path);
	}

	///<summary>Interface for file system access; by default a disk but could wrap arbitrary network file access</summary>
	public interface IVirtualFileSystem : IReadOnlyVirtualFileSystem {
		///<summary>returns true if CreateHardLink is available</summary>
		bool SupportsHardLinks { get; }
		///<summary>returns true if CreateSymbolicLink and ReadLink are available</summary>
		new bool SupportsSymbolicLinks { get; }

		///<summary>Creates an IVirtualFileSystem with this subdirectory from the whole</summary>
		///<param name="path">The path to create the virtual filesystem within</param>
		///<exception cref="System.IO.DirectoryNotFoundException">path does not exist</exception>
		///<exception cref="System.IO.IOException">an error occurred opening the path</exception>
		///<exception cref="System.InvalidOperationException">A system constraint was found to be violated while descending the directory tree</exception>
		new IVirtualFileSystem CreateChild(string path);

		///<summary>Creates a new subdirectory (and all parent directories)</summary>
		///<param name="path">The path to create</param>
 		///<returns>true if the directory was created; false if it already existed</returns>
		///<exception cref="System.IO.IOException">an error occurred creating the directory</exception>
		///<exception cref="System.InvalidOperationException">A system constraint was found to be violated while descending the directory tree</exception>
		bool CreateDirectory(string path);

		///<summary>Opens an existing file, or creates a new one</summary>
		///<param name="path">The file to open</param>
		///<param name="fileMode">disposition for how to behave on whether file exists or not</param>
		///<param name="fileAccess">the access desired for opening the file</param>
		///<param name="forAsyncAccess">whether the file can be accessed asynchronously or not</param>
		///<param name="bufferSize">buffer size</param>
		///<returns>The System.IO.FileStream object for the file</returns>
		///<exception cref="System.IO.FileNotFoundException">the file was not found to open</exception>
		///<exception cref="System.IO.DirectoryNotFoundException">the path was not found</exception>
		///<exception cref="System.IO.IOException">an error occurred opening the path</exception>
		///<exception cref="System.InvalidOperationException">A system constraint was found to be violated while descending the directory tree</exception>
		Stream OpenVirtualFile(string path, FileMode fileMode, FileAccess fileAccess, bool forAsyncAccess = false, int bufferSize = 4096);

		///<summary>Renames a file, clobbering any file in the way</summary>
		///<param name="oldname">The path to the node being renamed</param>
		///<param name="newname">The path the node shall be known as</param>
		///<exception cref="System.IO.FileNotFoundException">the file was not found to open</exception>
		///<exception cref="System.IO.DirectoryNotFoundException">the path was not found</exception>
		///<exception cref="System.IO.IOException">an error occurred</exception>
		///<exception cref="System.InvalidOperationException">A system constraint was found to be violated while descending the directory tree</exception>
		void RenameReplace(string oldname, string newname);

		///<summary>Creates a hard link</summary>
		///<param name="targetpath">The path to the node being renamed</param>
		///<param name="linkpath">The path the node shall be known as</param>
		///<exception cref="System.IO.FileNotFoundException">the file was not found to open</exception>
		///<exception cref="System.IO.DirectoryNotFoundException">the path was not found</exception>
		///<exception cref="System.IO.IOException">an error occurred</exception>
		///<exception cref="System.InvalidOperationException">A system constraint was found to be violated while descending the directory tree</exception>
		///<remarks>If you are using this in a security sensitive context; beware. Windows has a platform-level
		///unremovable race where the targetpath can be swapped out from under you to a symbolic link pointing outside the jail.</remarks>
		void CreateHardLink(string targetpath, string linkpath);

		///<summary>Creates a symbolic link</summary>
		///<param name="targetpath">The path to the node that shall be accessed, relative to the symbolic link</param>
		///<param name="linkpath">The path the node shall be known as</param>
		///<param name="linkTargetType">What kind of symbolic link to create</param>
		///<exception cref="System.IO.FileNotFoundException">the file was not found to open</exception>
		///<exception cref="System.IO.DirectoryNotFoundException">the path was not found</exception>
		///<exception cref="System.IO.IOException">an error occurred</exception>
		///<exception cref="System.InvalidOperationException">A system constraint was found to be violated while descending the directory tree</exception>
		///<remarks>On Windows, if linkTargetType is not specified and the target doesn't exist, the operation fails</remarks>
		void CreateSymbolicLink(string targetpath, string linkpath, FileType linkTargetType = FileType.LinkTargetHintNotAvailable);

		///<summary>Removes a non-directory</summary>
		///<param name="path">The path to the file to remove</param>
		///<returns>true if the file used to exist, false if it already did not</returns>
		///<exception cref="System.IO.IOException">an error occurred</exception>
		///<exception cref="System.InvalidOperationException">A system constraint was found to be violated while descending the directory tree</exception>
		bool RemoveFile(string path);

		///<summary>Removes a directory</summary>
		///<param name="path">The path to the file to remove</param>
		///<param name="recurse">Whether to remove the contents of a non-empty directory nor not</param>
		///<returns>true if the directory used to exist, false if it already did not</returns>
		///<exception cref="System.IO.IOException">an error occurred</exception>
		///<exception cref="System.InvalidOperationException">A system constraint was found to be violated while descending the directory tree</exception>
		bool RemoveDirectory(string path, bool recurse = false);
	}

	///<summary>Interface for disk file system access</summary>
	///<remarks>This interface provides extra methods that non-disk access would have trouble matching</remarks>
	public interface IDiskVirtualFileSystem : IVirtualFileSystem {
		///<summary>Returns whether or not handles to directories are first class handles on this platform</summary>
		bool SupportsDirectoryHandles { get; }

		///<summary>Creates an IDiskVirtualFileSystem with this subdirectory from the whole</summary>
		///<param name="path">The path to create the virtual filesystem within</param>
		///<exception cref="System.IO.DirectoryNotFoundException">path does not exist</exception>
		///<exception cref="System.IO.IOException">an error occurred opening the path</exception>
		///<exception cref="System.InvalidOperationException">A system constraint was found to be violated while descending the directory tree</exception>
		new IDiskVirtualFileSystem CreateChild(string path);

		///<summary>Creates an IDiskVirtualFileSystem with this subdirectory from the whole</summary>
		///<param name="path">The path to create the virtual filesystem within</param>
		///<param name="followSymbolicLinks">Whether or not to follow symbolic links on the leaf node</param>
		///<exception cref="System.IO.DirectoryNotFoundException">path does not exist</exception>
		///<exception cref="System.IO.IOException">an error occurred opening the path</exception>
		///<exception cref="System.InvalidOperationException">A system constraint was found to be violated while descending the directory tree</exception>
		IDiskVirtualFileSystem CreateChild(string path, FileSystem.FollowSymbolicLinks followSymbolicLinks);

		///<summary>Opens an existing file, or creates a new one</summary>
		///<param name="path">The file to open</param>
		///<param name="fileMode">disposition for how to behave on whether file exists or not</param>
		///<param name="fileAccess">the access desired for opening the file</param>
		///<param name="followSymbolicLinks">whether or not to follow symbolic links at the leaf level when creating a file</param>
		///<param name="forAsyncAccess">whether the file can be accessed asynchronously or not</param>
		///<param name="bufferSize">buffer size</param>
		///<returns>The System.IO.FileStream object for the file</returns>
		///<remarks>followSymbolicLinks cannot and does not function when fileMode is FileMode.CreateNew</remarks>
		///<exception cref="System.IO.FileNotFoundException">the file was not found to open</exception>
		///<exception cref="System.IO.DirectoryNotFoundException">the path was not found</exception>
		///<exception cref="System.IO.IOException">an error occurred opening the path</exception>
		///<exception cref="System.InvalidOperationException">A system constraint was found to be violated while descending the directory tree</exception>
		FileStream OpenFile(string path, FileMode fileMode, FileAccess fileAccess, FileSystem.FollowSymbolicLinks followSymbolicLinks, bool forAsyncAccess = false, int bufferSize = 4096);

		///<summary>Opens a directory handle for file system operations</summary>
		///<param name="path">The path to the directory</param>
		///<param name="followSymbolicLinks">whether or not to follow symbolic links at the leaf level when creating a file</param>
		///<param name="requestEnumeration">Whether to open the directory for enumeration or just traversal</param>
		///<exception cref="System.IO.DirectoryNotFoundException">the path was not found</exception>
		///<exception cref="System.IO.IOException">an error occurred opening the path</exception>
		///<exception cref="System.InvalidOperationException">A system constraint was found to be violated while descending the directory tree</exception>
		Microsoft.Win32.SafeHandles.SafeFileHandle OpenDirectory(string path, FileSystem.FollowSymbolicLinks followSymbolicLinks, bool requestEnumeration = false);
	}

	///<summary>Provides additional methods on IVirtualFileSystem implemented in terms of existing methods</summary>
	public static class VirtualFileSystemExtensions {
		///<summary>Gets the contents of a directory</summary>
		///<param name="this">the IVirtualFileSystem entry to operate on</param>
		///<param name="path">The path to the directory to enumerate</param>
		///<exception cref="System.IO.IOException">an error occured opening the path</exception>
		///<exception cref="System.InvalidOperationException">A system constraint was found to be violated while descending the directory tree</exception>
		public static IEnumerable<DirectoryEntry>? GetDirectoryContentsOrNull(this IReadOnlyVirtualFileSystem @this, string path)
			=> @this.GetDirectoryContents(path, FileSystem.NonExtantDirectoryBehavior.ReturnNull);

		///<summary>Gets the contents of a directory</summary>
		///<param name="this">the IVirtualFileSystem entry to operate on</param>
		///<param name="path">The path to the directory to enumerate</param>
		///<exception cref="System.IO.DirectoryNotFoundException">The directory does not exist</exception>
		///<exception cref="System.IO.IOException">an error occured opening the path</exception>
		///<exception cref="System.InvalidOperationException">A system constraint was found to be violated while descending the directory tree</exception>
		public static IEnumerable<DirectoryEntry> GetDirectoryContentsOrThrow(this IReadOnlyVirtualFileSystem @this, string path)
			=> @this.GetDirectoryContents(path, FileSystem.NonExtantDirectoryBehavior.Throw);

		///<summary>Gets the contents of a directory</summary>
		///<param name="this">the IVirtualFileSystem entry to operate on</param>
		///<param name="path">The path to the directory to enumerate</param>
		///<exception cref="System.IO.IOException">an error occured opening the path</exception>
		///<exception cref="System.InvalidOperationException">A system constraint was found to be violated while descending the directory tree</exception>
		public static IEnumerable<DirectoryEntry> GetDirectoryContentsOrEmpty(this IReadOnlyVirtualFileSystem @this, string path)
			=> @this.GetDirectoryContents(path, FileSystem.NonExtantDirectoryBehavior.ReturnEmpty);

		///<summary>Build a path from components, using the context of the IVirtualFileSystem</summary>
		///<param name="this">the IVirtualFileSystem entry to examine</param>
		///<param name="components">the components to join</param>
		public static string CombinePath(this IReadOnlyVirtualFileSystem @this, params string[] components)
			=> CombinePath(@this, (IEnumerable<string>)components);

		///<summary>Build a path from components, using the context of the IVirtualFileSystem</summary>
		///<param name="this">the IVirtualFileSystem entry to examine</param>
		///<param name="components">the components to join</param>
		public static string CombinePath(this IReadOnlyVirtualFileSystem @this, IEnumerable<string> components)
		{
			if (@this is null) throw new ArgumentNullException(nameof(@this));
			if (components is null) throw new ArgumentNullException(nameof(components));
			char c = @this.DirectorySeparatorCharacter;
			bool first = true;
			var builder = new System.Text.StringBuilder();
			foreach (var component in components)
			{
				if (first) first = false; else builder.Append(c);
				builder.Append(component);
			}
			if (first) throw new ArgumentOutOfRangeException(nameof(components), "components cannot be empty");
			return builder.ToString();
		}

		///<summary>Gets the split point between the directory name and the file name</summary>
		///<param name="this">the IVirtualFileSystem entry to examine</param>
		///<param name="path">The path to examine</param>
		///<returns>The index into the string of the last directory separator character, or -1 if none are present</returns>
		public static int GetDirectoryFileSeparationPoint(this IReadOnlyVirtualFileSystem @this, string path)
		{
			if (@this is null) throw new ArgumentNullException(nameof(@this));
			if (path is null) throw new ArgumentNullException(nameof(path));
			char sep = @this.DirectorySeparatorCharacter;
			char sep2 = @this.AlternateDirectorySeparatorCharacter;
			for (int i = path.Length; i --> 0;) {
				char c = path[i];
				if (c == sep || c == sep2) return i;
			}
			return -1;
		}

		///<summary>Given a path that could exist on a filesystem, get its directory name component</summary>
		///<param name="this">the IVirtualFileSystem entry to examine</param>
		///<param name="path">The path to extract the directory name from</param>
		public static string GetDirectoryName(this IReadOnlyVirtualFileSystem @this, string path)
		{
			int separation = GetDirectoryFileSeparationPoint(@this, path);
			if (separation < 0) return @this.CurrentDirectoryName;
			if (separation == 0) separation = 1;
			return path.Substring(0, separation);
		}

		///<summary>Given a path that could exist on a filesystem, get its file name component</summary>
		///<param name="this">the IVirtualFileSystem entry to examine</param>
		///<param name="path">The path to extract the file name from</param>
		public static string GetFileName(this IReadOnlyVirtualFileSystem @this, string path)
		{
			int separation = GetDirectoryFileSeparationPoint(@this, path);
			if (separation < 0 || separation == path.Length - 1) return path;
			return path.Substring(separation + 1);
		}
	}
}

#endif
