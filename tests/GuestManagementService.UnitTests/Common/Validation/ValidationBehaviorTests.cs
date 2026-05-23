using FluentValidation;
using GuestManagementService.Application.Common.Validation;
using MediatR;

namespace GuestManagementService.UnitTests.Common.Validation;

public sealed class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_WhenNoValidators_CallsNext()
    {
        var behavior = new ValidationBehavior<TestRequest, string>([]);

        var result = await behavior.Handle(
            new TestRequest("valid"),
            () => Task.FromResult("next-called"),
            CancellationToken.None);

        Assert.Equal("next-called", result);
    }

    [Fact]
    public async Task Handle_WhenValidationPasses_CallsNext()
    {
        var behavior = new ValidationBehavior<TestRequest, string>([new TestRequestValidator()]);

        var result = await behavior.Handle(
            new TestRequest("valid"),
            () => Task.FromResult("next-called"),
            CancellationToken.None);

        Assert.Equal("next-called", result);
    }

    [Fact]
    public async Task Handle_WhenValidationFails_ThrowsValidationException()
    {
        var behavior = new ValidationBehavior<TestRequest, string>([new TestRequestValidator()]);

        var exception = await Assert.ThrowsAsync<ValidationException>(() => behavior.Handle(
            new TestRequest(""),
            () => Task.FromResult("next-called"),
            CancellationToken.None));

        Assert.Contains(exception.Errors, error => error.PropertyName == nameof(TestRequest.Name));
    }

    private sealed record TestRequest(string Name) : IRequest<string>;

    private sealed class TestRequestValidator : AbstractValidator<TestRequest>
    {
        public TestRequestValidator()
        {
            RuleFor(request => request.Name).NotEmpty();
        }
    }
}
