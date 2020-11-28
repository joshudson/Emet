using System.Runtime.CompilerServices;
namespace Emet.VB {
	///<summary>Provides value type helpers in global scope</summary>
	[Microsoft.VisualBasic.CompilerServices.StandardModule]
	public static class ValueTypeUtil {
		///<summary>Returns a T? of the given type if object could be downcast to T; otherwise returns a T? that equals Nothing</summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T? TryCastNullable<T>(object @object) where T: struct => @object as T?;
	}
}
