#if NET35
using System.Reflection;

namespace System.Collections.Generic
{
	//
	// Summary:
	//     Represents a strongly-typed, read-only collection of elements.
	//
	// Type parameters:
	//   T:
	//     The type of the elements.This type parameter is covariant. That is, you can use
	//     either the type you specified or any type that is more derived. For more information
	//     about covariance and contravariance, see Covariance and Contravariance in Generics.
	//[TypeDependencyAttribute("System.SZArrayHelper")]
	public interface IReadOnlyCollection<T> : IEnumerable<T>, IEnumerable
	{
		//
		// Summary:
		//     Gets the number of elements in the collection.
		//
		// Returns:
		//     The number of elements in the collection.
		int Count { get; }
	}

	static class RealdOnlyExtension
	{
		class RL<T>: ObjectModel.ReadOnlyCollection<T>, IReadOnlyList<T>			
		{
			public RL(IList<T> source):base(source)
			{

			}
		}

		public static IReadOnlyList<T> AsReadOnlyEx<T>(this IList<T> source)
		{
			return new RL<T>(source);
		}
	}
}
#endif
