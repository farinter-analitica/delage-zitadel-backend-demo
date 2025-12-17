using ApprovalRequestsApi.Application.DTOs.Requests;
using FluentValidation;

namespace ApprovalRequestsApi.Application.Validators;

public class CreateApprovalRequestValidator : AbstractValidator<CreateApprovalRequestDto>
{
    public CreateApprovalRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("El título es requerido")
            .MaximumLength(200).WithMessage("El título no puede exceder los 200 caracteres");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("La descripción es requerida")
            .MaximumLength(2000).WithMessage("La descripción no puede exceder los 2000 caracteres");
    }
}
