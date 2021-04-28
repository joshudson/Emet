using System;

namespace Emet.MultiCall {
	public static class Test {
		public static void Main(string[] args) {
			Console.WriteLine("My name is " + Entry.Argv0);
			Console.WriteLine("My name is " + Entry.argv0);
			Entry.Dispatch(args,
				("entry1", "Entry Point 1", EntryPoint1),
				("entry2", "Entry Point 2", EntryPoint2),
				("entry3", "Entry Point 3", EntryPoint3));
		}

		public static void EntryPoint1(string[] _) => Console.WriteLine("Reached EntryPoint1");
		public static void EntryPoint2(string[] _) => Console.WriteLine("Reached EntryPoint2");
		public static void EntryPoint3(string[] _) => Console.WriteLine("Reached EntryPoint3");
	}
}
