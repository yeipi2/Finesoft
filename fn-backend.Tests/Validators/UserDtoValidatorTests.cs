using FluentAssertions;
using fn_backend.DTO;
using fs_backend.Validators;

namespace fn_backend.Tests.Validators;

public class UserDtoValidatorTests
{
    private readonly UserDtoValidator _userValidator;
    private readonly ChangePasswordDtoValidator _changePasswordValidator;
    private readonly ProfileUpdateDtoValidator _profileUpdateValidator;

    public UserDtoValidatorTests()
    {
        _userValidator = new UserDtoValidator();
        _changePasswordValidator = new ChangePasswordDtoValidator();
        _profileUpdateValidator = new ProfileUpdateDtoValidator();
    }

    private UserDto CreateValidUser()
    {
        return new UserDto
        {
            Email = "test@user.com",
            UserName = "testuser",
            Password = "Password123",
            RoleName = "Admin"
        };
    }

    [Fact]
    public void UserValidator_ValidUser_ShouldPass()
    {
        var user = CreateValidUser();
        var result = _userValidator.TestValidate(user);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void UserValidator_EmptyEmail_ShouldFail()
    {
        var user = CreateValidUser();
        user.Email = "";
        var result = _userValidator.TestValidate(user);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void UserValidator_InvalidEmail_ShouldFail()
    {
        var user = CreateValidUser();
        user.Email = "not-email";
        var result = _userValidator.TestValidate(user);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void UserValidator_ShortUserName_ShouldFail()
    {
        var user = CreateValidUser();
        user.UserName = "ab";
        var result = _userValidator.TestValidate(user);
        result.ShouldHaveValidationErrorFor(x => x.UserName);
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("Administracion")]
    [InlineData("Empleado")]
    [InlineData("Supervisor")]
    [InlineData("Cliente")]
    public void UserValidator_ValidRole_ShouldPass(string role)
    {
        var user = CreateValidUser();
        user.RoleName = role;
        var result = _userValidator.TestValidate(user);
        result.ShouldNotHaveValidationErrorFor(x => x.RoleName);
    }

    [Fact]
    public void UserValidator_InvalidRole_ShouldFail()
    {
        var user = CreateValidUser();
        user.RoleName = "InvalidRole";
        var result = _userValidator.TestValidate(user);
        result.ShouldHaveValidationErrorFor(x => x.RoleName);
    }

    [Fact]
    public void ChangePasswordValidator_ValidRequest_ShouldPass()
    {
        var dto = new ChangePasswordDto
        {
            CurrentPassword = "OldPassword123",
            NewPassword = "NewPassword123"
        };
        var result = _changePasswordValidator.TestValidate(dto);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ChangePasswordValidator_EmptyCurrentPassword_ShouldFail()
    {
        var dto = new ChangePasswordDto
        {
            CurrentPassword = "",
            NewPassword = "NewPassword123"
        };
        var result = _changePasswordValidator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.CurrentPassword);
    }

    [Fact]
    public void ChangePasswordValidator_WeakNewPassword_ShouldFail()
    {
        var dto = new ChangePasswordDto
        {
            CurrentPassword = "OldPassword123",
            NewPassword = "weak"
        };
        var result = _changePasswordValidator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    [Fact]
    public void ProfileUpdateValidator_ValidProfile_ShouldPass()
    {
        var dto = new ProfileUpdateDto
        {
            FullName = "Juan Perez",
            Phone = "+525512345678"
        };
        var result = _profileUpdateValidator.TestValidate(dto);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ProfileUpdateValidator_FullNameWithSpecialChars_ShouldFail()
    {
        var dto = new ProfileUpdateDto
        {
            FullName = "Juan <script>alert('xss')</script>"
        };
        var result = _profileUpdateValidator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.FullName);
    }
}
