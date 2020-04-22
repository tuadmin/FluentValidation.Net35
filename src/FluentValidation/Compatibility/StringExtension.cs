

namespace System
{
	static class StringExtension
	{
		[Runtime.CompilerServices.MethodImpl((Runtime.CompilerServices.MethodImplOptions)256 /*AggressiveInlining*/)]
		public static bool IsNullOrWhiteSpace(this string source) =>
#if NET35
			string.IsNullOrEmpty(source) || source.Trim().Length == 0;
#else
			string.IsNullOrWhiteSpace(source);
#endif


	}
}
