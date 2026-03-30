using Arc.Api.DTOs;
using FluentAssertions;
using Arc.Api.Validators;
namespace Arc.UnitTests.Validators;


public sealed class ExecuteRequestDtoValidatorTests
{
    private readonly ExecuteRequestDtoValidator _validator = new();

    [Fact]
    public void Validate_WithValidInput_ShouldPass()
    {
        var request = new ExecuteRequestDto("Write a function to calculate fibonacci numbers");

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_WithEmptyInput_ShouldFail(string input)
    {
        var request = new ExecuteRequestDto(input);

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Input" && 
                                            e.ErrorMessage == "Input is required.");
    }

    [Fact]
    public void Validate_WithVeryLongInput_ShouldPass()
    {
        var request = new ExecuteRequestDto(new string('a', 10000));

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }
}