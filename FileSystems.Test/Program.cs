/* vi:ts=2
 */

using System;
using System.IO;
using Emet.FileSystems;

static class Program {
	static void Main(string[] args)
	{
		string testpath = ".scratch";
		try {
			try {
				Directory.Delete(testpath, true);
			} catch (DirectoryNotFoundException) {}
			var dt1 = DateTime.UtcNow;

			RunSomeTest("Setting up test paths", () => {
					CreateDirectory(Path.Combine(testpath, "dir1"));
					CreateDirectory(Path.Combine(testpath, "dir2"));
					CreateFile(Path.Combine(testpath, "file1"));
					CreateFile(Path.Combine(testpath, "file2"));
				});
			var dt2 = DateTime.UtcNow;
			if (dt2 < dt1) dt1 = dt2;
			dt1 = ShearOffMilliseconds(dt1);
			RunSomeTest("Creating symbolic link to file", () => {
					FileSystem.CreateSymbolicLink("file1", Path.Combine(testpath, "symlink1"));
				});
			RunSomeTest("Creating symbolic link to directory", () => {
					FileSystem.CreateSymbolicLink("dir2", Path.Combine(testpath, "symlink2"));
				});
			RunSomeTest("Testing ReadLink", () => {
					AssertAreEqual("file1", FileSystem.ReadLink(Path.Combine(testpath, "symlink1")));
				});
			RunSomeTest("Creating hard link to file", () => {
					FileSystem.CreateHardLink(Path.Combine(testpath, "file1"), Path.Combine(testpath, "link1"));
				});
			RunSomeTest("Testing that stat can be used on files", () => {
					Assert("new file is newer than start", (new DirectoryEntry(Path.Combine(testpath, "file1"), FileSystem.FollowSymbolicLinks.Never)).LastChangeTimeUTC >= dt1);
				});
			RunSomeTest("Testing that stat can be used on directories", () => {
					Assert("new directory is newer than start", (new DirectoryEntry(Path.Combine(testpath, "dir1"), FileSystem.FollowSymbolicLinks.Never)).LastChangeTimeUTC >= dt1);
				});
			RunSomeTest("Testing that links have same inode number", () => {
					AssertAreEqual(
						(new DirectoryEntry(Path.Combine(testpath, "file1"), FileSystem.FollowSymbolicLinks.Never)).InodeNumber,
						(new DirectoryEntry(Path.Combine(testpath, "link1"), FileSystem.FollowSymbolicLinks.Never)).InodeNumber);
				});
			RunSomeTest("Testing that symbolic link has the correct type for FollowSymboicLinks.Never", () => {
					AssertAreEqual(FileType.SymbolicLink, (new DirectoryEntry(Path.Combine(testpath, "symlink1"), FileSystem.FollowSymbolicLinks.Never)).FileType);
				});
			RunSomeTest("Testing that file has same inode number as following symbolic link", () => {
					AssertAreEqual(
						(new DirectoryEntry(Path.Combine(testpath, "file1"), FileSystem.FollowSymbolicLinks.Never)).InodeNumber,
						(new DirectoryEntry(Path.Combine(testpath, "symlink1"), FileSystem.FollowSymbolicLinks.Always)).InodeNumber);
				});
			RunSomeTest("Testing that follow always follows link to files", () => {
					AssertAreEqual(FileType.File, (new DirectoryEntry(Path.Combine(testpath, "symlink1"), FileSystem.FollowSymbolicLinks.Always)).FileType);
				});
			RunSomeTest("Testing that follow not directory follows link to files", () => {
					AssertAreEqual(FileType.File, (new DirectoryEntry(Path.Combine(testpath, "symlink1"), FileSystem.FollowSymbolicLinks.IfNotDirectory)).FileType);
				});
			RunSomeTest("Testing that follow not directory does not follow links to directories", () => {
					AssertAreEqual(FileType.SymbolicLink, (new DirectoryEntry(Path.Combine(testpath, "symlink2"), FileSystem.FollowSymbolicLinks.IfNotDirectory)).FileType);
				});
			RunSomeTest("Testing that follow always returns the correct type for directory", () => {
					AssertAreEqual(FileType.Directory, (new DirectoryEntry(Path.Combine(testpath, "symlink2"), FileSystem.FollowSymbolicLinks.Always)).FileType);
				});
			RunSomeTest("Testing that follow never directory does not follow link to directory", () => {
					AssertAreEqual(FileType.SymbolicLink, (new DirectoryEntry(Path.Combine(testpath, "symlink2"), FileSystem.FollowSymbolicLinks.Never)).FileType);
				});
			RunSomeTest("Testing FileExists", () => {
					AssertAreEqual(false, FileSystem.FileExists(Path.Combine(testpath, "dir1")));
					AssertAreEqual(true, FileSystem.FileExists(Path.Combine(testpath, "file1")));
					AssertAreEqual(false, FileSystem.FileExists(Path.Combine(testpath, "nope")));
			});
			RunSomeTest("Testing DirectoryExists", () => {
					AssertAreEqual(true, FileSystem.DirectoryExists(Path.Combine(testpath, "dir1")));
					AssertAreEqual(false, FileSystem.DirectoryExists(Path.Combine(testpath, "file1")));
					AssertAreEqual(false, FileSystem.DirectoryExists(Path.Combine(testpath, "nope")));
			});
			void Enum(FileSystem.FollowSymbolicLinks behavior)
			{
				bool founddir1 = false;
				bool founddir2 = false;
				bool foundfile1 = false;
				bool foundfile2 = false;
				bool foundlink1 = false;
				bool foundsymlink1 = false;
				bool foundsymlink2 = false;
				foreach (var node in FileSystem.GetDirectoryContents(testpath, FileSystem.NonExtantDirectoryBehavior.Throw, behavior))
				{
					if (node is null) throw new AssertionFailed("Null node");
					if (node.Name is null) throw new AssertionFailed("Null name");
					AssertAreNotEqual(DirectoryEntry.CurrentDirectoryName, node.Name);
					AssertAreNotEqual(DirectoryEntry.ParentDirectoryName, node.Name);
					switch(node.Name)
					{
						case "dir1":
							AssertAreEqual(FileType.Directory, node.FileType);
							founddir1 = true;
							break;
						case "dir2":
							AssertAreEqual(FileType.Directory, node.FileType);
							founddir2 = true;
							break;
						case "file1":
							AssertAreEqual(FileType.File, node.FileType);
							AssertAreEqual((long)2, node.LinkCount);
							foundfile1 = true;
							break;
						case "file2":
							AssertAreEqual(FileType.File, node.FileType);
							AssertAreEqual((long)1, node.LinkCount);
							foundfile2 = true;
							break;
						case "link1":
							AssertAreEqual(FileType.File, node.FileType);
							AssertAreEqual((long)2, node.LinkCount);
							foundlink1 = true;
							break;
						case "symlink1":
							AssertAreEqual(behavior == FileSystem.FollowSymbolicLinks.Never ? FileType.SymbolicLink : FileType.File, node.FileType);
							AssertAreEqual(behavior == FileSystem.FollowSymbolicLinks.Never ? (long)1 : (long)2, node.LinkCount);
							foundsymlink1 = true;
							break;
						case "symlink2":
							AssertAreEqual(behavior == FileSystem.FollowSymbolicLinks.Always ? FileType.Directory : FileType.SymbolicLink, node.FileType);
							if (behavior != FileSystem.FollowSymbolicLinks.Always)
								AssertAreEqual((long)1, node.LinkCount); // Don't try to assert the directory's link count
							foundsymlink2 = true;
							break;
					}
				}
				Assert("Found dir1", founddir1);
				Assert("Found dir2", founddir2);
				Assert("Found file1", foundfile1);
				Assert("Found file2", foundfile2);
				Assert("Found link1", foundlink1);
				Assert("Found symlink1", foundsymlink1);
				Assert("Found symlink2", foundsymlink2);
			}
			RunSomeTest("Testing enumeration of scratch directory with follow none", () => {
					Enum(FileSystem.FollowSymbolicLinks.Never); });
			RunSomeTest("Testing enumeration of scratch directory with follow always", () => {
					Enum(FileSystem.FollowSymbolicLinks.Always); });
			RunSomeTest("Testing enumeration of scratch directory with follow not directory", () => {
					Enum(FileSystem.FollowSymbolicLinks.IfNotDirectory); });
			RunSomeTest("Testing RenameReplace", () => {
					// Doesn't use an inode check in case Windows doesn't actually ensure inode numbers don't change here
					CreateFile(Path.Combine(testpath, "rnto"));
					File.WriteAllText(Path.Combine(testpath, "rnfr"), "Test");
					FileSystem.RenameReplace(Path.Combine(testpath, "rnfr"), Path.Combine(testpath, "rnto"));
					AssertAreEqual("Test", File.ReadAllText(Path.Combine(testpath, "rnto")));
				});
		}
		catch (Exception ex) {
			Environment.ExitCode = 1;
			Console.WriteLine("Failed");
			Console.WriteLine(ex.ToString());
		}
		finally {
			try {
				Directory.Delete(testpath, true);
			} catch (DirectoryNotFoundException) {}
		}
	}

	static void RunSomeTest(string name, Action test)
	{
		Console.Write(name + "...");
		test();
		Console.WriteLine("OK");
	}

	static void AssertAreEqual(object expected, object actual)
	{
		if (!expected.Equals(actual))
			throw new AssertionFailed("Expected " + expected.ToString() + " but got " + actual.ToString());
	}

	static void AssertAreNotEqual(object expected, object actual)
	{
		if (expected.Equals(actual))
			throw new AssertionFailed("Expected not " + expected.ToString() + " but got " + actual.ToString() + " anyway");
	}

	static void Assert(string assertion, bool result)
	{
		if (!result) throw new AssertionFailed("Not " + assertion);
	}

	static string GetFullPath(string rel, string path)
	{
		if (Path.IsPathRooted(path)) return Path.GetFullPath(path);
		return Path.GetFullPath(Path.Combine(rel, path));
	}

	static void CreateDirectory(string path) => Directory.CreateDirectory(path);

	static void CreateFile(string path)
	{
		using (var fs = new FileStream(path, FileMode.CreateNew, FileAccess.Write)) {}
	}

	static DateTime ShearOffMilliseconds(DateTime input)
		=> new DateTime((input.Ticks / TimeSpan.TicksPerSecond) * TimeSpan.TicksPerSecond, input.Kind);
}

class AssertionFailed : Exception {
	public AssertionFailed(string message) : base(message) {}
}
