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

#pragma warning disable 1591
namespace FluentValidation.TestHelper {
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using System.Linq.Expressions;
	using System.Reflection;
	using System.Text.RegularExpressions;
	using System.Threading;
	using System.Threading.Tasks;
	using Internal;
	using Results;
	using Validators;

	public static class ValidationTestExtension {
		internal const string MatchAnyFailure = "__FV__ANY";

		//TODO: Should ShouldHaveChildValidator be deprecated? It isn't recommended and leads to brittle tests.
		public static void ShouldHaveChildValidator<T, TProperty>(this IValidator<T> validator, Expression<Func<T, TProperty>> expression, Type childValidatorType) {
			var descriptor = validator.CreateDescriptor();
			var expressionMemberName = expression.GetMember()?.Name;

			if (expressionMemberName == null && !expression.IsParameterExpression()) {
				throw new NotSupportedException("ShouldHaveChildValidator can only be used for simple property expressions. It cannot be used for model-level rules or rules that contain anything other than a property reference.");
			}

			var matchingValidators =
				expression.IsParameterExpression() ? GetModelLevelValidators<T>(descriptor) :
				descriptor.GetValidatorsForMember(expressionMemberName)
					.Select(x => x.Validator)
					.ToArray();


			matchingValidators = matchingValidators.Concat(GetDependentRules(expressionMemberName, expression, descriptor)).ToArray();

			var childValidatorTypes = matchingValidators.OfType<IChildValidatorAdaptor>().Select(x => x.ValidatorType);

			if (childValidatorTypes.All(x => !childValidatorType.IsAssignableFrom(x))) {
				var childValidatorNames = childValidatorTypes.Any() ? string.Join(", ", childValidatorTypes.Select(x => x.Name).ToArray()) : "none";
				throw new ValidationTestException(string.Format("Expected property '{0}' to have a child validator of type '{1}.'. Instead found '{2}'", expressionMemberName, childValidatorType.Name, childValidatorNames));
			}
		}

		private static IEnumerable<IPropertyValidator> GetDependentRules<T, TProperty>(string expressionMemberName, Expression<Func<T, TProperty>> expression, IValidatorDescriptor descriptor) {
			var member = expression.IsParameterExpression() ? null : expressionMemberName;

			var rules = descriptor.GetRulesForMember(member)
				.OfType<IValidationRuleInternal<T>>()
#if NET35
				.SelectMany(x =>  x.DependentRules is null ? Enumerable.Empty<IValidationRule>() : x.DependentRules.OfType<IValidationRule>())
#else
				.SelectMany(x => x.DependentRules ?? Enumerable.Empty<IValidationRuleInternal<T>>())
#endif
				.SelectMany(x => x.Components)
				.Select(x => x.Validator);

			return rules;
		}

		private static IPropertyValidator[] GetModelLevelValidators<T>(IValidatorDescriptor descriptor) {
			var rules = descriptor.GetRulesForMember(null).OfType<IValidationRule<T>>();
			return rules.Where(x => x.Expression == null || x.Expression.IsParameterExpression())
				.SelectMany(x => x.Components)
				.Select(x => x.Validator)
				.ToArray();
		}

		/// <summary>
		/// Performs validation, returning a TestValidationResult which allows assertions to be performed.
		/// </summary>
		public static TestValidationResult<T> TestValidate<T>(this IValidator<T> validator, T objectToTest, Action<ValidationStrategy<T>> options = null) {
			options ??= _ => { };
			ValidationResult validationResult;
			try {
				validationResult = validator.Validate(objectToTest, options);
			}
			catch (AsyncValidatorInvokedSynchronouslyException ex) {
				throw new AsyncValidatorInvokedSynchronouslyException(ex.ValidatorType.Name + " contains asynchronous rules - please use the asynchronous test methods instead.");
			}

			return new TestValidationResult<T>(validationResult);
		}

		/// <summary>
		/// Performs async validation, returning a TestValidationResult which allows assertions to be performed.
		/// </summary>
		public static async Task<TestValidationResult<T>> TestValidateAsync<T>(this IValidator<T> validator, T objectToTest, Action<ValidationStrategy<T>> options = null, CancellationToken cancellationToken = default) {
			options ??= _ => { };
			var validationResult = await validator.ValidateAsync(objectToTest, options, cancellationToken);
			return new TestValidationResult<T>(validationResult);
		}

		public static ITestValidationContinuation ShouldHaveAnyValidationError<T>(this TestValidationResult<T> testValidationResult) {
			if (!testValidationResult.Errors.Any())
				throw new ValidationTestException($"Expected at least one validation error, but none were found.");

			return TestValidationContinuation.Create(testValidationResult.Errors);
		}

		public static void ShouldNotHaveAnyValidationErrors<T>(this TestValidationResult<T> testValidationResult) {
			ShouldNotHaveValidationError(testValidationResult.Errors, MatchAnyFailure, true);
		}

		private static string BuildErrorMessage(ValidationFailure failure, string exceptionMessage, string defaultMessage) {
      if (exceptionMessage != null && failure != null) {
        var formattedExceptionMessage = exceptionMessage.Replace("{Code}", failure.ErrorCode)
          .Replace("{Message}", failure.ErrorMessage)
          .Replace("{State}", failure.CustomState?.ToString() ?? "")
          .Replace("{Severity}", failure.Severity.ToString());

        var messageArgumentMatches = Regex.Matches(formattedExceptionMessage, "{MessageArgument:(.*)}");
        for (var i = 0; i < messageArgumentMatches.Count; i++) {
          if (failure.FormattedMessagePlaceholderValues.ContainsKey(messageArgumentMatches[i].Groups[1].Value)) {
            formattedExceptionMessage = formattedExceptionMessage.Replace(messageArgumentMatches[i].Value, failure.FormattedMessagePlaceholderValues[messageArgumentMatches[i].Groups[1].Value].ToString());
          }
        }
        return formattedExceptionMessage;
      }
			return defaultMessage;
		}

    internal static ITestValidationWith ShouldHaveValidationError(IList<ValidationFailure> errors, string propertyName, bool shouldNormalizePropertyName) {
	    var result = TestValidationContinuation.Create(errors);
	    result.ApplyPredicate(x => (shouldNormalizePropertyName ?  NormalizePropertyName(x.PropertyName) == propertyName : x.PropertyName == propertyName)
	                               || (string.IsNullOrEmpty(x.PropertyName) && string.IsNullOrEmpty(propertyName))
	                               || propertyName == MatchAnyFailure);

      if (result.Any()) {
        return result;
      }

      // We expected an error but failed to match it.
      var errorMessageBanner = $"Expected a validation error for property {propertyName}";

      string errorMessage = "";

      if (errors?.Any() == true) {
        string errorMessageDetails = "";
        for (int i = 0; i < errors.Count; i++) {
          errorMessageDetails += $"[{i}]: {errors[i].PropertyName}\n";
        }
        errorMessage = $"{errorMessageBanner}\n----\nProperties with Validation Errors:\n{errorMessageDetails}";
      }
      else {
        errorMessage = $"{errorMessageBanner}";
      }

      throw new ValidationTestException(errorMessage);
    }

		internal static void ShouldNotHaveValidationError(IEnumerable<ValidationFailure> errors, string propertyName, bool shouldNormalizePropertyName) {
			var failures = errors.Where(x => (shouldNormalizePropertyName ? NormalizePropertyName(x.PropertyName) == propertyName : x.PropertyName == propertyName)
			                                 || (string.IsNullOrEmpty(x.PropertyName) && string.IsNullOrEmpty(propertyName))
			                                 || propertyName == MatchAnyFailure
			                                 ).ToList();

			if (failures.Any()) {
				var errorMessageBanner = $"Expected no validation errors for property {propertyName}";
				if (propertyName == MatchAnyFailure) {
					errorMessageBanner = "Expected no validation errors";
				}
				string errorMessageDetails = "";
				for (int i = 0; i < failures.Count; i++) {
					errorMessageDetails += $"[{i}]: {failures[i].ErrorMessage}\n";
				}
				var errorMessage = $"{errorMessageBanner}\n----\nValidation Errors:\n{errorMessageDetails}";
				throw new ValidationTestException(errorMessage, failures);
			}
		}

		public static ITestValidationWith When(this ITestValidationContinuation failures, Func<ValidationFailure, bool> failurePredicate, string exceptionMessage = null) {
			var result = TestValidationContinuation.Create(((TestValidationContinuation)failures).MatchedFailures);
			result.ApplyPredicate(failurePredicate);

			var anyMatched = result.Any();
			if (!anyMatched) {
				var failure = result.UnmatchedFailures.FirstOrDefault();
				string message = BuildErrorMessage(failure, exceptionMessage, "Expected validation error was not found");
				throw new ValidationTestException(message);
			}

			return result;
		}

		public static ITestValidationContinuation WhenAll(this ITestValidationContinuation failures, Func<ValidationFailure, bool> failurePredicate, string exceptionMessage = null) {
			var result = TestValidationContinuation.Create(((TestValidationContinuation)failures).MatchedFailures);
			result.ApplyPredicate(failurePredicate);

			bool allMatched = !result.UnmatchedFailures.Any();

			if (!allMatched) {
				var failure = result.UnmatchedFailures.First();
				string message = BuildErrorMessage(failure, exceptionMessage, "Found an unexpected validation error");
				throw new ValidationTestException(message);
			}

			return result;
		}

		public static ITestValidationWith WithSeverity(this ITestValidationContinuation failures, Severity expectedSeverity) {
			return failures.When(failure => failure.Severity == expectedSeverity, string.Format("Expected a severity of '{0}'. Actual severity was '{{Severity}}'", expectedSeverity));
		}

		public static ITestValidationWith WithCustomState(this ITestValidationContinuation failures, object expectedCustomState, IEqualityComparer comparer = null) {
			return failures.When(failure => comparer?.Equals(failure.CustomState, expectedCustomState) ?? Equals(failure.CustomState, expectedCustomState), string.Format("Expected custom state of '{0}'. Actual state was '{{State}}'", expectedCustomState));
		}

    public static ITestValidationWith WithMessageArgument<T>(this ITestValidationContinuation failures, string argumentKey, T argumentValue) {
      return failures.When(failure => failure.FormattedMessagePlaceholderValues.ContainsKey(argumentKey) && ((T)failure.FormattedMessagePlaceholderValues[argumentKey]).Equals(argumentValue),
        string.Format("Expected message argument '{0}' with value '{1}'. Actual value was '{{MessageArgument:{0}}}'", argumentKey, argumentValue.ToString()));
    }

		public static ITestValidationWith WithErrorMessage(this ITestValidationContinuation failures, string expectedErrorMessage) {
			return failures.When(failure => failure.ErrorMessage == expectedErrorMessage, string.Format("Expected an error message of '{0}'. Actual message was '{{Message}}'", expectedErrorMessage));
		}

		public static ITestValidationWith WithErrorCode(this ITestValidationContinuation failures, string expectedErrorCode) {
			return failures.When(failure => failure.ErrorCode == expectedErrorCode, string.Format("Expected an error code of '{0}'. Actual error code was '{{Code}}'", expectedErrorCode));
		}

		public static ITestValidationContinuation WithoutSeverity(this ITestValidationContinuation failures, Severity unexpectedSeverity) {
			return failures.WhenAll(failure => failure.Severity != unexpectedSeverity, string.Format("Found an unexpected severity of '{0}'", unexpectedSeverity));
		}

		public static ITestValidationContinuation WithoutCustomState(this ITestValidationContinuation failures, object unexpectedCustomState) {
			return failures.WhenAll(failure => failure.CustomState != unexpectedCustomState, string.Format("Found an unexpected custom state of '{0}'", unexpectedCustomState));
		}

		public static ITestValidationContinuation WithoutErrorMessage(this ITestValidationContinuation failures, string unexpectedErrorMessage) {
			return failures.WhenAll(failure => failure.ErrorMessage != unexpectedErrorMessage, string.Format("Found an unexpected error message of '{0}'", unexpectedErrorMessage));
		}

		public static ITestValidationContinuation WithoutErrorCode(this ITestValidationContinuation failures, string unexpectedErrorCode) {
			return failures.WhenAll(failure => failure.ErrorCode != unexpectedErrorCode, string.Format("Found an unexpected error code of '{0}'", unexpectedErrorCode));
		}

		public static ITestValidationWith Only(this ITestValidationWith failures) {
			if (failures.UnmatchedFailures.Any()) {

				var errorMessageBanner = "Expected to have errors only matching specified conditions";
				string errorMessageDetails = "";
				var unmatchedFailures = failures.UnmatchedFailures.ToList();
				for (int i = 0; i < unmatchedFailures.Count; i++) {
					errorMessageDetails += $"[{i}]: {unmatchedFailures[i].ErrorMessage}\n";
				}
				var errorMessage = $"{errorMessageBanner}\n----\nUnexpected Errors:\n{errorMessageDetails}";

				throw new ValidationTestException(errorMessage);
			}
			return failures;
		}

		private static string NormalizePropertyName(string propertyName) {
			return Regex.Replace(propertyName, @"\[.*\]", string.Empty);
		}
	}
}
