using FluentAssertions;
using Arc.Api.DTOs.Auth;
using Arc.Api.Validators.Auth;
namespace Arc.UnitTests.Validators;


public sealed class RegisterRequestDtoValidatorTests
{
    private readonly RegisterRequestDtoValidator _validator = new();

    [Fact]
    public void Validate_WithValidData_ShouldPass()
    {
        var request = new RegisterRequestDto
        {
            Username = "john_doe",
            Email = "john@example.com",
            Password = "Test123!",
            Role = "User"
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_WithEmptyUsername_ShouldFail(string username)
    {
        var request = new RegisterRequestDto
        {
            Username = username,
            Email = "valid@example.com",
            Password = "Test123!"
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Username" && 
                                            e.ErrorMessage == "Username is required");
    }

    [Fact]
    public void Validate_WithUsernameTooShort_ShouldFail()
    {
        var request = new RegisterRequestDto
        {
            Username = "ab",
            Email = "valid@example.com",
            Password = "Test123!"
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Username" && 
                                            e.ErrorMessage == "Username must be at least 3 characters long");
    }

    [Fact]
    public void Validate_WithUsernameTooLong_ShouldFail()
    {
        var request = new RegisterRequestDto
        {
            Username = new string('a', 51),
            Email = "valid@example.com",
            Password = "Test123!"
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Username" && 
                                            e.ErrorMessage == "Username must not exceed 50 characters");
    }

    [Theory]
    [InlineData("invalid username")]
    [InlineData("invalid@username")]
    [InlineData("invalid.username")]
    public void Validate_WithInvalidUsernameCharacters_ShouldFail(string username)
    {
        var request = new RegisterRequestDto
        {
            Username = username,
            Email = "valid@example.com",
            Password = "Test123!"
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Username" && 
                                            e.ErrorMessage == "Username can only contain letters, numbers, underscores, and hyphens");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_WithEmptyEmail_ShouldFail(string email)
    {
        var request = new RegisterRequestDto
        {
            Username = "valid_user",
            Email = email,
            Password = "Test123!"
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
        var request = new RegisterRequestDto
        {
            Username = "valid_user",
            Email = email,
            Password = "Test123!"
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email" && 
                                            e.ErrorMessage == "Invalid email format");
    }

    [Fact]
    public void Validate_WithEmailTooLong_ShouldFail()
    {
        var request = new RegisterRequestDto
        {
            Username = "valid_user",
            Email = new string('a', 250) + "@example.com",
            Password = "Test123!"
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email" && 
                                            e.ErrorMessage == "Email must not exceed 255 characters");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_WithEmptyPassword_ShouldFail(string password)
    {
        var request = new RegisterRequestDto
        {
            Username = "valid_user",
            Email = "valid@example.com",
            Password = password
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password" && 
                                            e.ErrorMessage == "Password is required");
    }

    [Fact]
    public void Validate_WithPasswordTooShort_ShouldFail()
    {
        var request = new RegisterRequestDto
        {
            Username = "valid_user",
            Email = "valid@example.com",
            Password = "Test1!"
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password" && 
                                            e.ErrorMessage == "Password must be at least 8 characters long");
    }

    [Fact]
    public void Validate_WithPasswordTooLong_ShouldFail()
    {
        var request = new RegisterRequestDto
        {
            Username = "valid_user",
            Email = "valid@example.com",
            Password = "Test123!" + new string('a', 130)
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password" && 
                                            e.ErrorMessage == "Password must not exceed 128 characters");
    }

    [Theory]
    [InlineData("testtest")]
    [InlineData("Test test")]
    [InlineData("test1234")]
    [InlineData("TEST1234")]
    public void Validate_WithPasswordMissingRequiredCharacters_ShouldFail(string password)
    {
        var request = new RegisterRequestDto
        {
            Username = "valid_user",
            Email = "valid@example.com",
            Password = password
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password" && 
                                            e.ErrorMessage == "Password must contain at least one lowercase letter, one uppercase letter, and one digit");
    }

    [Theory]
    [InlineData("User")]
    [InlineData("Admin")]
    public void Validate_WithValidRole_ShouldPass(string role)
    {
        var request = new RegisterRequestDto
        {
            Username = "valid_user",
            Email = "valid@example.com",
            Password = "Test123!",
            Role = role
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithInvalidRole_ShouldFail()
    {
        var request = new RegisterRequestDto
        {
            Username = "valid_user",
            Email = "valid@example.com",
            Password = "Test123!",
            Role = "InvalidRole"
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Role" && 
                                            e.ErrorMessage == "Role must be either 'User' or 'Admin'");
    }

    [Fact]
    public void Validate_WithNullRole_ShouldPass()
    {
        var request = new RegisterRequestDto
        {
            Username = "valid_user",
            Email = "valid@example.com",
            Password = "Test123!",
            Role = null
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }
}

public sealed class LoginRequestDtoValidatorTests
{
    private readonly LoginRequestDtoValidator _validator = new();

    [Fact]
    public void Validate_WithValidData_ShouldPass()
    {
        var request = new LoginRequestDto
        {
            Email = "test@example.com",
            Password = "password123"
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
        var request = new LoginRequestDto
        {
            Email = email,
            Password = "password123"
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
    public void Validate_WithInvalidEmailFormat_ShouldFail(string email)
    {
        var request = new LoginRequestDto
        {
            Email = email,
            Password = "password123"
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email" && 
                                            e.ErrorMessage == "Invalid email format");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_WithEmptyPassword_ShouldFail(string password)
    {
        var request = new LoginRequestDto
        {
            Email = "valid@example.com",
            Password = password
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password" && 
                                            e.ErrorMessage == "Password is required");
    }
}

public sealed class UpdateProfileRequestDtoValidatorTests
{
    private readonly UpdateProfileRequestDtoValidator _validator = new();

    [Fact]
    public void Validate_WithValidData_ShouldPass()
    {
        var request = new UpdateProfileRequestDto
        {
            Username = "john_doe",
            Email = "john@example.com"
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_WithEmptyUsername_ShouldFail(string username)
    {
        var request = new UpdateProfileRequestDto
        {
            Username = username,
            Email = "valid@example.com"
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Username" && 
                                            e.ErrorMessage == "Username is required");
    }

    [Fact]
    public void Validate_WithUsernameTooShort_ShouldFail()
    {
        var request = new UpdateProfileRequestDto
        {
            Username = "ab",
            Email = "valid@example.com"
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Username" && 
                                            e.ErrorMessage == "Username must be at least 3 characters long");
    }

    [Fact]
    public void Validate_WithUsernameTooLong_ShouldFail()
    {
        var request = new UpdateProfileRequestDto
        {
            Username = new string('a', 51),
            Email = "valid@example.com"
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Username" && 
                                            e.ErrorMessage == "Username must not exceed 50 characters");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_WithEmptyEmail_ShouldFail(string email)
    {
        var request = new UpdateProfileRequestDto
        {
            Username = "valid_user",
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
    public void Validate_WithInvalidEmailFormat_ShouldFail(string email)
    {
        var request = new UpdateProfileRequestDto
        {
            Username = "valid_user",
            Email = email
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email" && 
                                            e.ErrorMessage == "Invalid email format");
    }
}