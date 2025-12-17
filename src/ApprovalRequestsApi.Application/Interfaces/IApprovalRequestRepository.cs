using ApprovalRequestsApi.Application.DTOs.Requests;
using ApprovalRequestsApi.Domain.Entities;

namespace ApprovalRequestsApi.Application.Interfaces;

public interface IApprovalRequestRepository
{
    Task<ApprovalRequest> CreateAsync(ApprovalRequest request);
    Task<(List<ApprovalRequest> Items, int TotalCount)> GetPagedAsync(SearchRequestDto searchDto);
    Task<ApprovalRequest?> GetByIdAsync(Guid id);
    Task<ApprovalRequest> UpdateAsync(ApprovalRequest request);
}
