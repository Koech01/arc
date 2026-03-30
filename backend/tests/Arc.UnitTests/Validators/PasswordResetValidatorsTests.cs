using FluentAssertions;
using Arc.Api.DTOs.Auth;
using Arc.Api.Validators.Auth;
namespace Arc.UnitTests.Validators;


public sealed class ForgotPasswordRequestDtoValidatorTests
{
    private readonly ForgotPasswordRequestDtoValidator _validator = new();

    [Fact]
    public void Validate_WithValidEmail_ShouldPass()
    {
        var request = new ForgotPasswordRequestDto
        {
            Email = "test@example.com"
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_WithEmptyEmail_ShouldFail(string email)
    {
        var request = new ForgotPasswordRequestDto
        {
            Email = email
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email" && 
                                            e.ErrorMessage == "Email is required");
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("@example.com")]
    [InlineData("test@")]
    [InlineData("test@@example.com")]
    public void Validate_WithInvalidEmailFormat_ShouldFail(string email)
    {
        var request = new ForgotPasswordRequestDto
        {
            Email = email
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email" && 
                                            e.ErrorMessage == "Invalid email format");
    }
}

public sealed class ResetPasswordRequestDtoValidatorTests
{
    private readonly ResetPasswordRequestDtoValidator _validator = new();

    [Fact]
    public void Validate_WithValidData_ShouldPass()
    {
        var request = new ResetPasswordRequestDto
        {
            Token = "valid-token-string",
            NewPassword = "NewPass123!"
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_WithEmptyToken_ShouldFail(string token)
    {
        var request = new ResetPasswordRequestDto
        {
            Token = token,
            NewPassword = "NewPass123!"
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Token" && 
                                            e.ErrorMessage == "Token is required");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_WithEmptyPassword_ShouldFail(string password)
    {
        var request = new ResetPasswordRequestDto
        {
            Token = "valid-token",
            NewPassword = password
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NewPassword" && 
                                            e.ErrorMessage == "Password is required");
    }

    [Fact]
    public void Validate_WithPasswordTooShort_ShouldFail()
    {
        var request = new ResetPasswordRequestDto
        {
            Token = "valid-token",
            NewPassword = "Test1!"
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NewPassword" && 
                                            e.ErrorMessage == "Password must be at least 8 characters long");
    }

    [Fact]
    public void Validate_WithPasswordTooLong_ShouldFail()
    {
        var request = new ResetPasswordRequestDto
        {
            Token = "valid-token",
            NewPassword = "Test123!" + new string('a', 130)
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NewPassword" && 
                                            e.ErrorMessage == "Password must not exceed 128 characters");
    }

    [Theory]
    [InlineData("testtest")]
    [InlineData("test1234")]
    [InlineData("TEST1234")]
    [InlineData("TestTest")]
    public void Validate_WithPasswordMissingRequiredCharacters_ShouldFail(string password)
    {
        var request = new ResetPasswordRequestDto
        {
            Token = "valid-token",
            NewPassword = password
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NewPassword" && 
                                            e.ErrorMessage == "Password must contain at least one lowercase letter, one uppercase letter, and one digit");
    }
}