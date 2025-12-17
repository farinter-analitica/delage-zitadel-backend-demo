namespace ApprovalRequestsApi.Application.DTOs.Requests;

public class CreateApprovalRequestDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
