using FluentAssertions;
using FluentAssertions.Specialized;
using FluentValidation;
using RedisGUI.Application.Core.Behavior;
using RedisGUI.Shared.Mediator;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AppValidationException = RedisGUI.Application.Exceptions.ValidationException;

namespace RedisGUI.Application.Tests.Core.Behavior;

[TestFixture]
public class ValidationBehaviorTests
{
	[Test]
	public async Task Handle_NoValidators_ShouldPassThrough()
	{
		// Arrange
		List<IValidator<TestRequest>> validators = [];
		ValidationBehavior<TestRequest, TestResponse> behavior = new(validators);
		TestRequest request = new() { Name = "Test" };
		TestResponse expectedResponse = new() { Result = "Success" };

		// Act
		TestResponse result = await behavior.Handle(
			request,
			CancellationToken.None,
			async () => await Task.FromResult(expectedResponse));

		// Assert
		result.Should().Be(expectedResponse);
	}

	[Test]
	public async Task Handle_ValidRequest_ShouldPassThrough()
	{
		// Arrange
		TestRequestValidator validator = new();
		List<IValidator<TestRequest>> validators = [validator];
		ValidationBehavior<TestRequest, TestResponse> behavior = new(validators);
		TestRequest request = new() { Name = "ValidName" };
		TestResponse expectedResponse = new() { Result = "Success" };

		// Act
		TestResponse result = await behavior.Handle(
			request,
			CancellationToken.None,
			async () => await Task.FromResult(expectedResponse));

		// Assert
		result.Should().Be(expectedResponse);
	}

	[Test]
	public async Task Handle_InvalidRequest_ShouldThrowValidationException()
	{
		// Arrange
		TestRequestValidator validator = new();
		List<IValidator<TestRequest>> validators = [validator];
		ValidationBehavior<TestRequest, TestResponse> behavior = new(validators);
		TestRequest request = new() { Name = "" }; // Invalid

		// Act
		Func<Task<TestResponse>> act = async () => await behavior.Handle(
			request,
			CancellationToken.None,
			async () => await Task.FromResult(new TestResponse()));

		// Assert
		await act.Should().ThrowAsync<AppValidationException>();
	}

	[Test]
	public async Task Handle_MultipleValidators_ShouldCombineErrors()
	{
		// Arrange
		TestRequestValidator validator1 = new();
		TestRequestValidator2 validator2 = new();
		List<IValidator<TestRequest>> validators = [validator1, validator2];
		ValidationBehavior<TestRequest, TestResponse> behavior = new(validators);
		TestRequest request = new() { Name = "", Age = -1 }; // Both invalid

		// Act
		Func<Task<TestResponse>> act = async () => await behavior.Handle(
			request,
			CancellationToken.None,
			async () => await Task.FromResult(new TestResponse()));

		// Assert
		ExceptionAssertions<AppValidationException> exception = await act.Should().ThrowAsync<AppValidationException>();
		exception.Which.Errors.Should().HaveCountGreaterThan(1);
	}

	private class TestRequest : IRequest<TestResponse>
	{
		public string Name { get; set; } = string.Empty;
		public int Age { get; set; }
	}

	private class TestResponse
	{
		public string Result { get; set; } = string.Empty;
	}

	private class TestRequestValidator : AbstractValidator<TestRequest>
	{
		public TestRequestValidator()
		{
			RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required");
		}
	}

	private class TestRequestValidator2 : AbstractValidator<TestRequest>
	{
		public TestRequestValidator2()
		{
			RuleFor(x => x.Age).GreaterThan(0).WithMessage("Age must be greater than 0");
		}
	}
}

