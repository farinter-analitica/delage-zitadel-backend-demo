using FluentValidation;

namespace ApprovalRequestsApi.Application.Validators;

public class ReviewApprovalRequestValidator : AbstractValidator<DTOs.Requests.ReviewApprovalRequestDto>
{
    public ReviewApprovalRequestValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty()
            .WithMessage("El estado es requerido")
            .Must(status => status?.ToLower() == "approved" || status?.ToLower() == "rejected")
            .WithMessage("El estado debe ser 'Approved' o 'Rejected'");

        RuleFor(x => x.AdminComments)
            .MaximumLength(1000)
            .WithMessage("Los comentarios del administrador no pueden exceder 1000 caracteres");
    }
}
