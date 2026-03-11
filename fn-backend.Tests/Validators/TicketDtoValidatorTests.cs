using FluentAssertions;
using fn_backend.DTO;
using fs_backend.Validators;

namespace fn_backend.Tests.Validators;

public class TicketDtoValidatorTests
{
    private readonly TicketDtoValidator _ticketValidator;
    private readonly TicketCommentDtoValidator _commentValidator;

    public TicketDtoValidatorTests()
    {
        _ticketValidator = new TicketDtoValidator();
        _commentValidator = new TicketCommentDtoValidator();
    }

    private TicketDto CreateValidTicket()
    {
        return new TicketDto
        {
            Title = "Problema con el sistema",
            Description = "El sistema no responde correctamente",
            Status = "Abierto",
            Priority = "Alta",
            EstimatedHours = 8
        };
    }

    [Fact]
    public void TicketValidator_ValidTicket_ShouldPass()
    {
        var ticket = CreateValidTicket();
        var result = _ticketValidator.TestValidate(ticket);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void TicketValidator_EmptyTitle_ShouldFail()
    {
        var ticket = CreateValidTicket();
        ticket.Title = "";
        var result = _ticketValidator.TestValidate(ticket);
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void TicketValidator_TitleExceedMaxLength_ShouldFail()
    {
        var ticket = CreateValidTicket();
        ticket.Title = new string('a', 201);
        var result = _ticketValidator.TestValidate(ticket);
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void TicketValidator_EmptyDescription_ShouldFail()
    {
        var ticket = CreateValidTicket();
        ticket.Description = "";
        var result = _ticketValidator.TestValidate(ticket);
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Theory]
    [InlineData("Abierto")]
    [InlineData("En Progreso")]
    [InlineData("En Revisión")]
    [InlineData("Cerrado")]
    public void TicketValidator_ValidStatus_ShouldPass(string status)
    {
        var ticket = CreateValidTicket();
        ticket.Status = status;
        var result = _ticketValidator.TestValidate(ticket);
        result.ShouldNotHaveValidationErrorFor(x => x.Status);
    }

    [Fact]
    public void TicketValidator_InvalidStatus_ShouldFail()
    {
        var ticket = CreateValidTicket();
        ticket.Status = "InvalidStatus";
        var result = _ticketValidator.TestValidate(ticket);
        result.ShouldHaveValidationErrorFor(x => x.Status);
    }

    [Theory]
    [InlineData("Baja")]
    [InlineData("Media")]
    [InlineData("Alta")]
    [InlineData("Urgente")]
    public void TicketValidator_ValidPriority_ShouldPass(string priority)
    {
        var ticket = CreateValidTicket();
        ticket.Priority = priority;
        var result = _ticketValidator.TestValidate(ticket);
        result.ShouldNotHaveValidationErrorFor(x => x.Priority);
    }

    [Fact]
    public void TicketValidator_InvalidPriority_ShouldFail()
    {
        var ticket = CreateValidTicket();
        ticket.Priority = "InvalidPriority";
        var result = _ticketValidator.TestValidate(ticket);
        result.ShouldHaveValidationErrorFor(x => x.Priority);
    }

    [Fact]
    public void TicketValidator_ZeroEstimatedHours_ShouldFail()
    {
        var ticket = CreateValidTicket();
        ticket.EstimatedHours = 0;
        var result = _ticketValidator.TestValidate(ticket);
        result.ShouldHaveValidationErrorFor(x => x.EstimatedHours);
    }

    [Fact]
    public void TicketValidator_ExceedMaxEstimatedHours_ShouldFail()
    {
        var ticket = CreateValidTicket();
        ticket.EstimatedHours = 1001;
        var result = _ticketValidator.TestValidate(ticket);
        result.ShouldHaveValidationErrorFor(x => x.EstimatedHours);
    }

    [Fact]
    public void CommentValidator_ValidComment_ShouldPass()
    {
        var comment = new TicketCommentDto
        {
            Comment = "Este es un comentario válido"
        };
        var result = _commentValidator.TestValidate(comment);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CommentValidator_EmptyComment_ShouldFail()
    {
        var comment = new TicketCommentDto
        {
            Comment = ""
        };
        var result = _commentValidator.TestValidate(comment);
        result.ShouldHaveValidationErrorFor(x => x.Comment);
    }

    [Fact]
    public void CommentValidator_CommentExceedMaxLength_ShouldFail()
    {
        var comment = new TicketCommentDto
        {
            Comment = new string('a', 5001)
        };
        var result = _commentValidator.TestValidate(comment);
        result.ShouldHaveValidationErrorFor(x => x.Comment);
    }
}
