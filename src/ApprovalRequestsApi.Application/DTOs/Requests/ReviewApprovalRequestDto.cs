namespace ApprovalRequestsApi.Application.DTOs.Requests;

public class ReviewApprovalRequestDto
{
    public string? Status { get; set; } // "Approved" or "Rejected"
    public string? ReviewerId { get; set; } // Opcional, se puede ignorar ya que se toma del token
    public string? AdminComments { get; set; }
}
