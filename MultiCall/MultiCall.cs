#nullable enable
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

#if OS_WIN
using System.Runtime.InteropServices;
#endif

namespace Emet.MultiCall {
	///<summary>The delegate type of Main</summary>
	public delegate void ComponentMain(string[] args);

	///<summary>Entry.Dispatch is used to select which component to use</summary>
	public static class Entry {
		///<summary>Invokes the correct entry point for the binary name</summary>
		///<param name="args">the arguments, usually from main()</param>
		///<param name="components">the list of components to run</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)] // Don't waste stack
		public static void Dispatch(string[] args, params (string Name, string HelpText, ComponentMain EntryPoint)[] components)
#if OSTYPE_UNIX
			=> Dispatch(StringComparison.Ordinal, Argv0, args, components);
#elif OS_WIN
			=> Dispatch(StringComparison.OrdinalIgnoreCase, Argv0, args, components);
#else
			=> throw null;
#endif

		///<summary>Invokes the correct entry point for the binary name</summary>
		///<param name="invocation">the name the program was started as, usually from Argv0</param>
		///<param name="args">the arguments, usually from main()</param>
		///<param name="components">the list of components to run</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Dispatch(string invocation, string[] args, params (string Name, string HelpText, ComponentMain EntryPoint)[] components)
#if OSTYPE_UNIX
			=> Dispatch(StringComparison.Ordinal, invocation, args, components);
#elif OS_WIN
			=> Dispatch(StringComparison.OrdinalIgnoreCase, invocation, args, components);
#else
			=> throw null;
#endif

		///<summary>Invokes the correct entry point for the binary name</summary>
		///<param name="comparison">the string comparison to apply</param>
		///<param name="args">the arguments, usually from main()</param>
		///<param name="components">the list of components to run</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Dispatch(StringComparison comparison, string[] args, params (string Name, string HelpText, ComponentMain EntryPoint)[] components)
			=> Dispatch(comparison, Argv0, args, components);

		///<summary>Invokes the correct entry point for the binary name</summary>
		///<param name="comparison">the string comparison to apply</param>
		///<param name="invocation">the name the program was started as, usually from Argv0</param>
		///<param name="args">the arguments, usually from main()</param>
		///<param name="components">the list of components to run</param>
		public static void Dispatch(StringComparison comparison, string invocation, string[] args, params (string Name, string HelpText, ComponentMain EntryPoint)[] components)
		{
			invocation = Path.GetFileName(invocation);
			foreach (var component in components)
				if (String.Equals(invocation, component.Name, comparison)) {
					component.EntryPoint(args);
					return;
				}
			Console.Error.WriteLine("This is a multi-call binary and should be linked to with one of its names and started.");
			foreach (var component in components)
				Console.Error.WriteLine($" {component.Name} - {component.HelpText}");
			Environment.Exit(255);
		}

#if OSTYPE_UNIX
		private static string? argv0 = null;
		public static string Argv0 { get {
			if (argv0 is null) {
				byte[] cbytes;
				int i = 0;
				try {
					using (var reader = new FileStream("/proc/self/cmdline", FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Write | FileShare.Delete))
					{
						// Guess what. Length property doesn't work.
						cbytes = new byte[1024]; // Only need to read in the name not the whole command line
						int offset = 0;
						do {
							int n = reader.Read(cbytes, offset, cbytes.Length - offset);
							if (n == 0) break;
							offset += n;
							for (; i < offset; i++)
								if (cbytes[i] == 0)
									break;
							Array.Resize(ref cbytes, cbytes.Length << 1);
						} while (i == offset);
					}
				} catch (FileNotFoundException) {
					throw new PlatformNotSupportedException("/proc must be mounted for this program to work.");
				} catch (DirectoryNotFoundException) {
					throw new PlatformNotSupportedException("/proc must be mounted for this program to work.");
				}
				argv0 = Encoding.UTF8.GetString(cbytes, 0, i);
			}
			return argv0;
		} }
#elif OS_WIN
		[DllImport("kernel32")]
		private static extern unsafe char* GetCommandLineW();
		private static string? argv0 = null;
		public static string Argv0 { get {
			if (argv0 is null) {
				var sb = new StringBuilder();
				unsafe {
					char c;
					char *cmd = GetCommandLineW();
					bool quoted = false;
					while ((c = *cmd++) != 0 && (quoted || c != ' ' && c != '\t'))
						if (c == '"') 
							quoted = !quoted;
						else
							sb.Append(c);
				}
				if (sb.Length > 4
						&& (sb[sb.Length - 1] == 'e' || sb[sb.Length - 1] == 'E')
						&& (sb[sb.Length - 2] == 'x' || sb[sb.Length - 2] == 'X')
						&& (sb[sb.Length - 3] == 'e' || sb[sb.Length - 3] == 'E'))
					sb.Length -= 4;
				argv0 = sb.ToString();
			}
			return argv0;
		} }
#else
		///<summary>Gets the name the program was invoked with</summary>
		public static string Argv0 => throw null;
#endif
	}
}
