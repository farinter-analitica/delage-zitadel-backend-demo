using ApprovalRequestsApi.Domain.Enums;

namespace ApprovalRequestsApi.Application.DTOs.Responses;

public class ApprovalRequestResponseDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public UserInfoDto Requester { get; set; } = null!;
    public ApprovalStatus Status { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public UserInfoDto? Reviewer { get; set; }
    public string? AdminComments { get; set; }
}
