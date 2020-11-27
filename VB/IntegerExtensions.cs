/* vi:ts=2
 */

using System;
using System.Runtime.CompilerServices;

namespace Emet.VB.Extensions {
	///<summary>Provides signed/unsigned conversions on integers</summary>
	[Microsoft.VisualBasic.CompilerServices.StandardModule]
	public static class IntegerExtensions {
		///<summary>Converts to signed type without changing any bits</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static sbyte AsSigned(this byte n) => (sbyte)n;
		///<summary>Converts to signed type without changing any bits</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static short AsSigned(this ushort n) => (short)n;
		///<summary>Converts to signed type without changing any bits</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static int AsSigned(this uint n) => (int)n;
		///<summary>Converts to signed type without changing any bits</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static long AsSigned(this ulong n) => (long)n;

		///<summary>Converts to unsigned type without changing any bits</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static byte AsUnsigned(this sbyte n) => (byte)n;
		///<summary>Converts to unsigned type without changing any bits</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static ushort AsUnsigned(this short n) => (ushort)n;
		///<summary>Converts to unsigned type without changing any bits</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static uint AsUnsigned(this int n) => (uint)n;
		///<summary>Converts to unsigned type without changing any bits</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)] public static ulong AsUnsigned(this long n) => (ulong)n;

		///<summary>Converts to signed type without changing any bits</summary>
		public static IntPtr AsSigned(this UIntPtr ptr) =>
			IntPtr.Size == 4 ? new IntPtr((int)ptr.ToUInt32()) : new IntPtr((long)ptr.ToUInt64());

		///<summary>Converts to unsigned type without changing any bits</summary>
		public static UIntPtr AsUnsigned(this IntPtr ptr) =>
			UIntPtr.Size == 4 ? new UIntPtr((uint)ptr.ToInt32()) : new UIntPtr((ulong)ptr.ToInt64());
	}
}
