namespace ApprovalRequestsApi.Application.DTOs.Responses;

public class ApprovalRequestListDto
{
    public List<ApprovalRequestResponseDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
