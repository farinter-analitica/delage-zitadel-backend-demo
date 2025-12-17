using ApprovalRequestsApi.Application.DTOs.Requests;
using FluentValidation;

namespace ApprovalRequestsApi.Application.Validators;

public class ApprovalDecisionValidator : AbstractValidator<ApprovalDecisionDto>
{
    public ApprovalDecisionValidator()
    {
        RuleFor(x => x.AdminComments)
            .MaximumLength(1000)
            .WithMessage("Los comentarios no pueden exceder los 1000 caracteres");
    }
}
