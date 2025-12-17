using ApprovalRequestsApi.Domain.Enums;

namespace ApprovalRequestsApi.Application.DTOs.Requests;

public class SearchRequestDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public ApprovalStatus? Status { get; set; }
    public string? RequesterId { get; set; }
    public string? Search { get; set; } // Búsqueda en título/descripción
}
