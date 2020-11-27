/* vi:ts=2
 */

using System.Runtime.CompilerServices;

namespace Emet.VB.Extensions {
	///<summary>provides NullSafeToString</summary>
	[Microsoft.VisualBasic.CompilerServices.StandardModule]
	public static class ObjectExtensions {
		///<summary>Calls .ToString() avoiding nulls</summary>
		public static string NullSafeToString(this object o) => o?.ToString() ?? "";
	}
}
