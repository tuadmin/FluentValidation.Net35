#region License
// Copyright (c) .NET Foundation and contributors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// The latest version of this file can be found at https://github.com/FluentValidation/FluentValidation
#endregion

namespace FluentValidation {
	using System;
	using System.Collections.Generic;
	using Internal;
	using Validators;

	/// <summary>
	/// Rule builder that starts the chain
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <typeparam name="TProperty"></typeparam>
#if NET35
  public interface IRuleBuilderInitial<T, TProperty> : IRuleBuilder<T, TProperty>, IConfigurable<PropertyRule, IRuleBuilderInitial<T, TProperty>> {
#else
  public interface IRuleBuilderInitial<T, out TProperty> : IRuleBuilder<T, TProperty>, IConfigurable<PropertyRule, IRuleBuilderInitial<T, TProperty>> {
#endif

		/// <summary>
		/// Transforms the property value before validation occurs.
		/// </summary>
		/// <typeparam name="TNew"></typeparam>
		/// <param name="transformationFunc"></param>
		/// <returns></returns>
		[Obsolete("Use Transform(x => x.Property, transformer) at the root level instead. This method will be removed in FluentValidation 10.")]
		IRuleBuilderInitial<T, TNew> Transform<TNew>(Func<TProperty, TNew> transformationFunc);
	}

  /// <summary>
  /// Rule builder
  /// </summary>
  /// <typeparam name="T"></typeparam>
  /// <typeparam name="TProperty"></typeparam>
#if NET35
  public interface IRuleBuilder<T, TProperty> {
#else
  public interface IRuleBuilder<T, out TProperty> {
#endif
    /// <summary>
    /// Associates a validator with this the property for this rule builder.
    /// </summary>
    /// <param name="validator">The validator to set</param>
    /// <returns></returns>
    IRuleBuilderOptions<T, TProperty> SetValidator(IPropertyValidator validator);

		/// <summary>
		/// Associates an instance of IValidator with the current property rule.
		/// </summary>
		/// <param name="validator">The validator to use</param>
		/// <param name="ruleSets"></param>
		IRuleBuilderOptions<T, TProperty> SetValidator(IValidator<TProperty> validator, params string[] ruleSets);

		/// <summary>
		/// Associates a validator provider with the current property rule.
		/// </summary>
		/// <param name="validatorProvider">The validator provider to use</param>
		/// <param name="ruleSets"></param>
		IRuleBuilderOptions<T, TProperty> SetValidator<TValidator>(Func<T, TValidator> validatorProvider, params string[] ruleSets)
			where TValidator : IValidator<TProperty>;

		/// <summary>
		/// Associates a validator provider with the current property rule.
		/// </summary>
		/// <param name="validatorProvider">The validator provider to use</param>
		/// <param name="ruleSets"></param>
		IRuleBuilderOptions<T, TProperty> SetValidator<TValidator>(Func<T, TProperty, TValidator> validatorProvider, params string[] ruleSets)
			where TValidator : IValidator<TProperty>;
	}


  /// <summary>
  /// Rule builder
  /// </summary>
  /// <typeparam name="T"></typeparam>
  /// <typeparam name="TProperty"></typeparam>
#if NET35
  public interface IRuleBuilderOptions<T, TProperty> :
#else
  public interface IRuleBuilderOptions<T, out TProperty> :
#endif
    IRuleBuilder<T, TProperty>, IConfigurable<PropertyRule, IRuleBuilderOptions<T, TProperty>> {
	}

	/// <summary>
	/// Rule builder that starts the chain for a child collection
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <typeparam name="TElement"></typeparam>
	public interface IRuleBuilderInitialCollection<T, TElement> : IRuleBuilder<T, TElement>, IConfigurable<CollectionPropertyRule<T, TElement>, IRuleBuilderInitialCollection<T, TElement>> {

		/// <summary>
		/// Transforms the collection element value before validation occurs.
		/// </summary>
		/// <param name="transformationFunc"></param>
		/// <returns></returns>
		[Obsolete("Use TransformForEach(x => x.Property, transformer) at the root level instead. This method will be removed in FluentValidation 10.")]
		IRuleBuilderInitial<T, TNew> Transform<TNew>(Func<TElement, TNew> transformationFunc);
	}

	/// <summary>
	/// Fluent interface for conditions (When/Unless/WhenAsync/UnlessAsync)
	/// </summary>
	public interface IConditionBuilder {
		/// <summary>
		/// Rules to be invoked if the condition fails.
		/// </summary>
		/// <param name="action"></param>
		void Otherwise(Action action);
	}

}
