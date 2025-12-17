using ApprovalRequestsApi.Application.DTOs.Responses;

namespace ApprovalRequestsApi.Application.Interfaces;

public interface IUserInfoService
{
    Task<UserInfoDto?> GetUserInfoAsync(string userId);
    Task<Dictionary<string, UserInfoDto>> GetUsersInfoAsync(IEnumerable<string> userIds);
}
