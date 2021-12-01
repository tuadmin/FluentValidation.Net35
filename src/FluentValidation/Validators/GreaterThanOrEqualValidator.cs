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

namespace FluentValidation.Validators {
	using System;
	using System.Reflection;

	public class GreaterThanOrEqualValidator<T, TProperty> : AbstractComparisonValidator<T, TProperty>, IGreaterThanOrEqualValidator where TProperty : IComparable<TProperty>, IComparable {

		public override string Name => "GreaterThanOrEqualValidator";

		public GreaterThanOrEqualValidator(TProperty value) :
			base(value) {
		}

		public GreaterThanOrEqualValidator(Func<T, TProperty> valueToCompareFunc, MemberInfo member, string memberDisplayName)
			: base(valueToCompareFunc, member, memberDisplayName) {
		}

#pragma warning disable CS3001 // Argument type is not CLS-compliant
		public GreaterThanOrEqualValidator(Func<T, (bool HasValue, TProperty Value)> valueToCompareFunc, MemberInfo member, string memberDisplayName)
#pragma warning restore CS3001 // Argument type is not CLS-compliant
			: base(valueToCompareFunc, member, memberDisplayName) {
		}

		public override bool IsValid(TProperty value, TProperty valueToCompare) {
			if (valueToCompare == null)
				return false;

			return value.CompareTo(valueToCompare) >= 0;
		}

		public override Comparison Comparison => Validators.Comparison.GreaterThanOrEqual;

		protected override string GetDefaultMessageTemplate(string errorCode) {
			return Localized(errorCode, Name);
		}
	}

	public interface IGreaterThanOrEqualValidator : IComparisonValidator { }
}
