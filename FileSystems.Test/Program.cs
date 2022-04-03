/* vi:ts=2
 */

using System;
using System.IO;
using Emet.FileSystems;

static class Program {
	static void Main(string[] args)
	{
		string testpath = ".scratch";
		string fatpath = null;
		if (args.Length > 0) {
			testpath = Path.Combine(args[0], testpath);
			if (args.Length > 1)
				fatpath = Path.Combine(args[1], "test.fat");
		} else
			Console.WriteLine("Running with default test location; to test on normal and FAT filesystems, give two arguments to Emet.FileSystems.Test.");
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
			RunSomeTest("Testing RemoveFile", () => {
					CreateFile(Path.Combine(testpath, "file"));
					Assert("file can be removed", FileSystem.RemoveFile(Path.Combine(testpath, "file")));
					try {
						FileSystem.RemoveFile(Path.Combine(testpath, "dir1"));
						throw new AssertionFailed("RemoveFile should have thrown when passed a directory");
					} catch (IOException) {}
					Assert("File actually removed", !FileSystem.FileExists(Path.Combine(testpath, "file")));
			});
			// These asserts just check that the test harness isn't broken somewhere
			Assert("file1 exists", FileSystem.FileExists(Path.Combine(testpath, "file1")));
			Assert("dir2 exists", FileSystem.DirectoryExists(Path.Combine(testpath, "dir2")));
			RunSomeTest("Testing RemoveDirectory", () => {
					try {
						FileSystem.RemoveDirectory(Path.Combine(testpath, "file1"), false);
						throw new AssertionFailed("RemoveDirectory should have thrown when passed a non-directory");
					} catch (IOException) {}
					try {
						FileSystem.RemoveDirectory(Path.Combine(testpath, "file1"), true);
						throw new AssertionFailed("RemoveDirectory should have thrown when passed a non-directory (recursive mode)");
					} catch (IOException) {}
					Directory.CreateDirectory(Path.Combine(testpath, "r1"));
					FileSystem.CreateSymbolicLink(Path.Combine("..", "dir2"), Path.Combine(testpath, "r1", "sm"));
					Directory.CreateDirectory(Path.Combine(testpath, "r1", "r2"));
					CreateFile(Path.Combine(testpath, "r1", "r2", "file"));
					Directory.CreateDirectory(Path.Combine(testpath, "r1", "xyz"));
					CreateFile(Path.Combine(testpath, "r1", "r2", "abc"));
					var deltarget = Path.Combine(testpath, "r1");
					try {
						FileSystem.RemoveDirectory(deltarget, false);
						throw new AssertionFailed("RemoveDirectory was recursive when asked not to be");
					} catch (IOException e) { Assert("exception contains faulting path", e.Message.Contains(deltarget)); }
					Assert("directory removed", FileSystem.RemoveDirectory(Path.Combine(testpath, "r1"), true));
					Assert("directory actually removed", !FileSystem.DirectoryExists(Path.Combine(testpath, "r1")));
					Assert("Symbolic link not traversed", FileSystem.DirectoryExists(Path.Combine(testpath, "dir2")));
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
		if (fatpath is not null) {
			try {
				try {
					Directory.Delete(fatpath, true);
				} catch (DirectoryNotFoundException) {}
				Directory.CreateDirectory(fatpath);
				RunSomeTest("Testing FAT error code on CreateHardLink", () => {
					CreateFile(Path.Combine(fatpath, "file1"));
					try {
						FileSystem.CreateHardLink(Path.Combine(fatpath, "file1"), Path.Combine(fatpath, "link1"));
					} catch (IOException ex) when (ex.HResult == IOErrors.NoSuchSystemCall) {}
				});
				RunSomeTest("Testing FAT error code on CreateSymbolicLink", () => {
					try {
						FileSystem.CreateSymbolicLink(Path.Combine(fatpath, "link2a"), Path.Combine(fatpath, "link2b"));
					} catch (IOException ex) when (ex.HResult == IOErrors.NoSuchSystemCall) {}
				});
			}
			catch (Exception ex) {
				Environment.ExitCode = 1;
				Console.WriteLine("Failed");
				Console.WriteLine(ex.ToString());
			}
			finally {
				try {
					Directory.Delete(fatpath, true);
				} catch (DirectoryNotFoundException) {}
			}
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
