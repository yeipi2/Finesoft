using FluentAssertions;
using fn_backend.DTO;
using fs_backend.Validators;

namespace fn_backend.Tests.Validators;

public class EmployeeDtoValidatorTests
{
    private readonly EmployeeDtoValidator _validator;

    public EmployeeDtoValidatorTests()
    {
        _validator = new EmployeeDtoValidator();
    }

    private EmployeeDto CreateValidEmployee()
    {
        return new EmployeeDto
        {
            Email = "empleado@test.com",
            Password = "Password123",
            RoleName = "Empleado",
            FullName = "Juan Perez",
            Phone = "+525512345678",
            Position = "Desarrollador",
            Department = "IT",
            HireDate = DateTime.Today
        };
    }

    [Fact]
    public void Validate_ValidEmployee_ShouldPass()
    {
        var employee = CreateValidEmployee();
        var result = _validator.TestValidate(employee);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyEmail_ShouldFail()
    {
        var employee = CreateValidEmployee();
        employee.Email = "";
        var result = _validator.TestValidate(employee);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_InvalidEmail_ShouldFail()
    {
        var employee = CreateValidEmployee();
        employee.Email = "not-an-email";
        var result = _validator.TestValidate(employee);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_WeakPassword_ShouldFail()
    {
        var employee = CreateValidEmployee();
        employee.Password = "weak";
        var result = _validator.TestValidate(employee);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validate_PasswordWithoutUppercase_ShouldFail()
    {
        var employee = CreateValidEmployee();
        employee.Password = "password123";
        var result = _validator.TestValidate(employee);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Theory]
    [InlineData("Empleado")]
    [InlineData("Supervisor")]
    [InlineData("Administracion")]
    public void Validate_ValidRole_ShouldPass(string role)
    {
        var employee = CreateValidEmployee();
        employee.RoleName = role;
        var result = _validator.TestValidate(employee);
        result.ShouldNotHaveValidationErrorFor(x => x.RoleName);
    }

    [Fact]
    public void Validate_InvalidRole_ShouldFail()
    {
        var employee = CreateValidEmployee();
        employee.RoleName = "InvalidRole";
        var result = _validator.TestValidate(employee);
        result.ShouldHaveValidationErrorFor(x => x.RoleName);
    }

    [Fact]
    public void Validate_FutureHireDate_ShouldFail()
    {
        var employee = CreateValidEmployee();
        employee.HireDate = DateTime.Today.AddDays(1);
        var result = _validator.TestValidate(employee);
        result.ShouldHaveValidationErrorFor(x => x.HireDate);
    }

    [Fact]
    public void Validate_EmptyFullName_ShouldFail()
    {
        var employee = CreateValidEmployee();
        employee.FullName = "";
        var result = _validator.TestValidate(employee);
        result.ShouldHaveValidationErrorFor(x => x.FullName);
    }

    [Fact]
    public void Validate_FullNameWithNumbers_ShouldFail()
    {
        var employee = CreateValidEmployee();
        employee.FullName = "Juan123 Perez";
        var result = _validator.TestValidate(employee);
        result.ShouldHaveValidationErrorFor(x => x.FullName);
    }
}
