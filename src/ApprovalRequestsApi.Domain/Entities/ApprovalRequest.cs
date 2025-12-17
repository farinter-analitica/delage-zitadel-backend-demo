using ApprovalRequestsApi.Domain.Enums;

namespace ApprovalRequestsApi.Domain.Entities;

public class ApprovalRequest
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string RequesterId { get; set; } = string.Empty; // Zitadel user ID
    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewerId { get; set; } // Zitadel user ID
    public string? AdminComments { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
