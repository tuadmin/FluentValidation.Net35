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

using System.Linq;

namespace FluentValidation.Tests {
	using System;
	using System.Collections.Generic;
	using System.Threading;
#if NET35
	using Task = System.Threading.Tasks.TaskEx;
#else
	using System.Threading.Tasks;
#endif
	using Xunit;


	public class CollectionValidatorWithParentTests {
		Person person;

		public CollectionValidatorWithParentTests() {
			person = new Person() {
				AnotherInt = 99,
				Children = new List<Person>() {
					new Person() {Email = "person@email.com"}
				},
				Orders = new List<Order>() {
					new Order {ProductName = "email_that_does_not_belong_to_a_person", Amount = 99},
					new Order {ProductName = "person@email.com", Amount = 1},
					new Order {ProductName = "another_email_that_does_not_belong_to_a_person", Amount = 1},
				}
			};
		}

		[Fact]
		public void Validates_collection() {
			var validator = new TestValidator {
				v => v.RuleFor(x => x.Surname).NotNull(),
				v => v.RuleForEach(x => x.Orders).SetValidator(y => new OrderValidator(y))
			};

			var results = validator.Validate(person);
			results.Errors.Count.ShouldEqual(3);

			results.Errors[1].PropertyName.ShouldEqual("Orders[0].ProductName");
			results.Errors[2].PropertyName.ShouldEqual("Orders[2].ProductName");
		}

		[Fact]
		public async System.Threading.Tasks.Task Validates_collection_asynchronously() {
			var validator = new TestValidator {
				v => v.RuleFor(x => x.Surname).NotNull(),
				v => v.RuleForEach(x => x.Orders).SetValidator(y => new AsyncOrderValidator(y))
			};

			var results = await validator.ValidateAsync(person);
			results.Errors.Count.ShouldEqual(3);

			results.Errors[1].PropertyName.ShouldEqual("Orders[0].ProductName");
			results.Errors[2].PropertyName.ShouldEqual("Orders[2].ProductName");
		}


		[Fact]
		public void Validates_collection_several_levels_deep() {
			var validator = new TestValidator {
				v => v.RuleFor(x => x.Surname).NotNull(),
				v => v.RuleForEach(x => x.Orders).SetValidator(y => new OrderValidator(y))
			};

			var rootValidator = new InlineValidator<Tuple<Person, object>>();
			rootValidator.RuleFor(x => x.Item1).SetValidator(validator);

			var results = rootValidator.Validate(Tuple.Create(person, new object()));
			results.Errors.Count.ShouldEqual(3);

			results.Errors[1].PropertyName.ShouldEqual("Item1.Orders[0].ProductName");
			results.Errors[2].PropertyName.ShouldEqual("Item1.Orders[2].ProductName");
		}

		[Fact]
		public async System.Threading.Tasks.Task Validates_collection_several_levels_deep_async() {
			var validator = new TestValidator {
				v => v.RuleFor(x => x.Surname).NotNull(),
				v => v.RuleForEach(x => x.Orders).SetValidator(y => new OrderValidator(y))
			};

			var rootValidator = new InlineValidator<Tuple<Person, object>>();
			rootValidator.RuleFor(x => x.Item1).SetValidator(validator);

			var results  = await rootValidator.ValidateAsync(Tuple.Create(person, new object()));
			results.Errors.Count.ShouldEqual(3);

			results.Errors[1].PropertyName.ShouldEqual("Item1.Orders[0].ProductName");
			results.Errors[2].PropertyName.ShouldEqual("Item1.Orders[2].ProductName");
		}


		[Fact]
		public void Collection_should_be_explicitly_included_with_expression() {
			var validator = new TestValidator {
				v => v.RuleFor(x => x.Surname).NotNull(),
				v => v.RuleForEach(x => x.Orders).SetValidator(y => new OrderValidator(y))
			};

			var results = validator.Validate(person, v => v.IncludeProperties(x => x.Orders));
			results.Errors.Count.ShouldEqual(2);
		}

		[Fact]
		public void Collection_should_be_explicitly_included_with_string() {
			var validator = new TestValidator {
				v => v.RuleFor(x => x.Surname).NotNull(),
				v => v.RuleForEach(x => x.Orders).SetValidator(y => new OrderValidator(y))
			};

			var results = validator.Validate(person, v => v.IncludeProperties("Orders"));
			results.Errors.Count.ShouldEqual(2);
		}

		[Fact]
		public void Collection_should_be_excluded() {
			var validator = new TestValidator {
				v => v.RuleFor(x => x.Surname).NotNull(),
				v => v.RuleForEach(x => x.Orders).SetValidator(y => new OrderValidator(y))
			};

			var results = validator.Validate(person, v => v.IncludeProperties(x => x.Forename));
			results.Errors.Count.ShouldEqual(0);
		}

		[Fact]
		public void Condition_should_work_with_child_collection() {
			var validator = new TestValidator() {
				v => v.RuleForEach(x => x.Orders).SetValidator(y => new OrderValidator(y)).When(x => x.Orders.Count == 4 /*there are only 3*/)
			};

			var result = validator.Validate(person);
			result.IsValid.ShouldBeTrue();
		}

		[Fact]
		public async System.Threading.Tasks.Task Async_condition_should_work_with_child_collection() {
			var validator = new TestValidator() {
				v => v.RuleForEach(x => x.Orders).SetValidator(y => new OrderValidator(y)).WhenAsync((x, c) => Task.FromResult(x.Orders.Count == 4) /*there are only 3*/)
			};

			var result = await validator.ValidateAsync(person);
			result.IsValid.ShouldBeTrue();
		}

		[Fact]
		public void Skips_null_items() {
			var validator = new TestValidator {
				v => v.RuleFor(x => x.Surname).NotNull(),
				v => v.RuleForEach(x => x.Orders).SetValidator(y => new OrderValidator(y))
			};

			person.Orders[0] = null;
			var results = validator.Validate(person);
			results.Errors.Count.ShouldEqual(2); //2 errors - 1 for person, 1 for 3rd Order.
		}

		[Fact]
		public void Can_validate_collection_using_validator_for_base_type() {
			var validator = new TestValidator() {
				v => v.RuleForEach(x => x.Orders).SetValidator(y => new OrderInterfaceValidator(y))
			};

			var result = validator.Validate(person);
			result.IsValid.ShouldBeFalse();
		}

		[Fact]
		public void Can_specify_condition_for_individual_collection_elements() {
			var validator = new TestValidator {
				v => v.RuleForEach(x => x.Orders)
					.Where(x => x.Amount != 1)
					.SetValidator(y => new OrderValidator(y))
			};

			var results = validator.Validate(person);
			results.Errors.Count.ShouldEqual(1);
		}

		[Fact]
		public void Should_override_property_name() {
			var validator = new TestValidator {
				v => v.RuleForEach(x => x.Orders).SetValidator(y => new OrderValidator(y))
					.OverridePropertyName("Orders2")
			};

			var results = validator.Validate(person);
			results.Errors[0].PropertyName.ShouldEqual("Orders2[0].ProductName");
		}


		[Fact]
		public void Should_work_with_top_level_collection_validator() {
			var personValidator = new InlineValidator<Person>();
			personValidator.RuleFor(x => x.Surname).NotNull();

			var validator = new InlineValidator<List<Person>>();
			validator.RuleForEach(x => x).SetValidator(personValidator);


			var results = validator.Validate(new List<Person> {new Person(), new Person(), new Person {Surname = "Bishop"}});
			results.Errors.Count.ShouldEqual(2);
			results.Errors[0].PropertyName.ShouldEqual("x[0].Surname");
		}

		[Fact]
		public void Should_work_with_top_level_collection_validator_and_overriden_name() {
			var personValidator = new InlineValidator<Person>();
			personValidator.RuleFor(x => x.Surname).NotNull();

			var validator = new InlineValidator<List<Person>>();
			validator.RuleForEach(x => x).SetValidator(personValidator).OverridePropertyName("test");


			var results = validator.Validate(new List<Person> {new Person(), new Person(), new Person {Surname = "Bishop"}});
			results.Errors.Count.ShouldEqual(2);
			results.Errors[0].PropertyName.ShouldEqual("test[0].Surname");
		}

		[Fact]
		public void Creates_validator_using_context_from_property_value() {
			var personValidator = new InlineValidator<Person>();

			var normalOrderValidator = new InlineValidator<Order>();
			normalOrderValidator.RuleFor(x => x.Amount).GreaterThan(0);

			var freeOrderValidator = new InlineValidator<Order>();
			freeOrderValidator.RuleFor(x => x.Amount).Equal(0);

			personValidator.RuleForEach(x => x.Orders)
				.SetValidator((p, order) => order.ProductName == "FreeProduct" ? freeOrderValidator : normalOrderValidator);

			var result1 = personValidator.Validate(new Person() {Orders = new List<Order> {new Order {ProductName = "FreeProduct"}}});
			result1.IsValid.ShouldBeTrue();

			var result2 = personValidator.Validate(new Person() {Orders = new List<Order> {new Order()}});
			result2.IsValid.ShouldBeFalse();
			result2.Errors[0].ErrorCode.ShouldEqual("GreaterThanValidator");
		}

		public class OrderValidator : AbstractValidator<Order> {
			public OrderValidator(Person person) {
				RuleFor(x => x.ProductName).Must(BeOneOfTheChildrensEmailAddress(person));
			}

			private Func<string, bool> BeOneOfTheChildrensEmailAddress(Person person) {
				return productName => person.Children.Any(child => child.Email == productName);
			}
		}

		public class OrderInterfaceValidator : AbstractValidator<IOrder> {
			public OrderInterfaceValidator(Person person) {
				RuleFor(x => x.Amount).NotEqual(person.AnotherInt);
			}
		}

		public class AsyncOrderValidator : AbstractValidator<Order> {
			public AsyncOrderValidator(Person person) {
				RuleFor(x => x.ProductName).MustAsync(BeOneOfTheChildrensEmailAddress(person));
			}

			private Func<string, CancellationToken, System.Threading.Tasks.Task<bool>> BeOneOfTheChildrensEmailAddress(Person person) {
				return (productName, cancel) => Task.FromResult(person.Children.Any(child => child.Email == productName));
			}
		}
	}
}
