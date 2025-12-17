using ApprovalRequestsApi.Application.DTOs.Requests;
using ApprovalRequestsApi.Application.DTOs.Responses;

namespace ApprovalRequestsApi.Application.Interfaces;

public interface IApprovalRequestService
{
    Task<ApprovalRequestResponseDto> CreateAsync(CreateApprovalRequestDto dto, string userId);
    Task<ApprovalRequestListDto> GetUserRequestsAsync(string userId, SearchRequestDto searchDto);
    Task<ApprovalRequestListDto> GetAllRequestsAsync(SearchRequestDto searchDto);
    Task<ApprovalRequestResponseDto> GetByIdAsync(Guid id, string userId, bool isAdmin);
    Task<ApprovalRequestResponseDto> ApproveAsync(Guid id, string adminId, ApprovalDecisionDto dto);
    Task<ApprovalRequestResponseDto> RejectAsync(Guid id, string adminId, ApprovalDecisionDto dto);
}
