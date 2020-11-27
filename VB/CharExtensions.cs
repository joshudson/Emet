/* vi:ts=2
 */

using System.Runtime.CompilerServices;

namespace Emet.VB.Extensions {
	///<summary>provides conversions between Char and Int</summary>
	[Microsoft.VisualBasic.CompilerServices.StandardModule]
	public static class CharExtensions {
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static int AsInt(this char c) => (int)c;
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static int? AsInt(this char? c) => (int?)c;
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static ushort AsUShort(this char c) => (ushort)c;
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static ushort? AsUShort(this char? c) => (ushort?)c;
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static short AsShort(this char c) => (short)c;
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static short? ToShort(this char? c) => (short?)c;

		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static char AsChar(this short c) => (char)c;
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static char? AsChar(this short? c) => (char?)c;
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static char AsChar(this ushort c) => (char)c;
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static char? AsChar(this ushort? c) => (char?)c;

		public static char AsChar(this int c)
		{
			if (c < -32767 || c > 65535) throw new System.ArgumentOutOfRangeException("Integer is too large to become a single character.");
			return (char)(ushort)c;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static char? AsChar(this int? c) => c.HasValue ? AsChar(c.GetValueOrDefault()) : (char?)null;
	}
}
