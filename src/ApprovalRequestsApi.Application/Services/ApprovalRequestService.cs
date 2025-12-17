using ApprovalRequestsApi.Application.DTOs.Requests;
using ApprovalRequestsApi.Application.DTOs.Responses;
using ApprovalRequestsApi.Application.Interfaces;
using ApprovalRequestsApi.Domain.Entities;
using ApprovalRequestsApi.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace ApprovalRequestsApi.Application.Services;

public class ApprovalRequestService : IApprovalRequestService
{
    private readonly IApprovalRequestRepository _repository;
    private readonly IUserInfoService _userInfoService;
    private readonly ILogger<ApprovalRequestService> _logger;

    public ApprovalRequestService(
        IApprovalRequestRepository repository,
        IUserInfoService userInfoService,
        ILogger<ApprovalRequestService> logger)
    {
        _repository = repository;
        _userInfoService = userInfoService;
        _logger = logger;
    }

    public async Task<ApprovalRequestResponseDto> CreateAsync(
        CreateApprovalRequestDto dto, string userId)
    {
        var request = new ApprovalRequest
        {
            Title = dto.Title,
            Description = dto.Description,
            RequesterId = userId,
            Status = ApprovalStatus.Pending,
            RequestedAt = DateTime.UtcNow
        };

        var created = await _repository.CreateAsync(request);
        return await MapToResponseDtoAsync(created);
    }

    public async Task<ApprovalRequestListDto> GetUserRequestsAsync(
        string userId, SearchRequestDto searchDto)
    {
        // Forzar el filtro por requesterId
        searchDto.RequesterId = userId;
        return await GetFilteredRequestsAsync(searchDto);
    }

    public async Task<ApprovalRequestListDto> GetAllRequestsAsync(
        SearchRequestDto searchDto)
    {
        return await GetFilteredRequestsAsync(searchDto);
    }

    public async Task<ApprovalRequestResponseDto> GetByIdAsync(
        Guid id, string userId, bool isAdmin)
    {
        var request = await _repository.GetByIdAsync(id);

        if (request == null)
            throw new KeyNotFoundException($"Solicitud con ID {id} no encontrada");

        // Validar que el usuario pueda ver la solicitud
        if (!isAdmin && request.RequesterId != userId)
            throw new UnauthorizedAccessException("No tiene permisos para ver esta solicitud");

        return await MapToResponseDtoAsync(request);
    }

    public async Task<ApprovalRequestResponseDto> ApproveAsync(
        Guid id, string adminId, ApprovalDecisionDto dto)
    {
        var request = await _repository.GetByIdAsync(id);

        if (request == null)
            throw new KeyNotFoundException($"Solicitud con ID {id} no encontrada");

        if (request.Status != ApprovalStatus.Pending)
            throw new InvalidOperationException("Solo se pueden aprobar solicitudes pendientes");

        request.Status = ApprovalStatus.Approved;
        request.ReviewerId = adminId;
        request.ReviewedAt = DateTime.UtcNow;
        request.AdminComments = dto.AdminComments;

        var updated = await _repository.UpdateAsync(request);

        _logger.LogInformation(
            "Solicitud {RequestId} aprobada por admin {AdminId}",
            id, adminId);

        return await MapToResponseDtoAsync(updated);
    }

    public async Task<ApprovalRequestResponseDto> RejectAsync(
        Guid id, string adminId, ApprovalDecisionDto dto)
    {
        var request = await _repository.GetByIdAsync(id);

        if (request == null)
            throw new KeyNotFoundException($"Solicitud con ID {id} no encontrada");

        if (request.Status != ApprovalStatus.Pending)
            throw new InvalidOperationException("Solo se pueden rechazar solicitudes pendientes");

        request.Status = ApprovalStatus.Rejected;
        request.ReviewerId = adminId;
        request.ReviewedAt = DateTime.UtcNow;
        request.AdminComments = dto.AdminComments;

        var updated = await _repository.UpdateAsync(request);

        _logger.LogInformation(
            "Solicitud {RequestId} rechazada por admin {AdminId}",
            id, adminId);

        return await MapToResponseDtoAsync(updated);
    }

    private async Task<ApprovalRequestListDto> GetFilteredRequestsAsync(
        SearchRequestDto searchDto)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(searchDto);

        // Obtener información de todos los usuarios únicos
        var userIds = items
            .Select(r => r.RequesterId)
            .Concat(items.Where(r => r.ReviewerId != null).Select(r => r.ReviewerId!))
            .Distinct()
            .ToList();

        var usersInfo = await _userInfoService.GetUsersInfoAsync(userIds);

        var responseDtos = items.Select(request => MapToResponseDtoSync(request, usersInfo)).ToList();

        return new ApprovalRequestListDto
        {
            Items = responseDtos,
            TotalCount = totalCount,
            Page = searchDto.Page,
            PageSize = searchDto.PageSize
        };
    }

    private async Task<ApprovalRequestResponseDto> MapToResponseDtoAsync(
        ApprovalRequest request)
    {
        var userIds = new List<string> { request.RequesterId };
        if (!string.IsNullOrEmpty(request.ReviewerId))
            userIds.Add(request.ReviewerId);

        var usersInfo = await _userInfoService.GetUsersInfoAsync(userIds);

        return MapToResponseDtoSync(request, usersInfo);
    }

    private static ApprovalRequestResponseDto MapToResponseDtoSync(
        ApprovalRequest request,
        Dictionary<string, UserInfoDto> usersInfo)
    {
        return new ApprovalRequestResponseDto
        {
            Id = request.Id,
            Title = request.Title,
            Description = request.Description,
            Requester = usersInfo.GetValueOrDefault(request.RequesterId) ?? new UserInfoDto
            {
                UserId = request.RequesterId,
                Name = "Usuario Desconocido",
                Email = "unknown@email.com"
            },
            Status = request.Status,
            RequestedAt = request.RequestedAt,
            ReviewedAt = request.ReviewedAt,
            Reviewer = request.ReviewerId != null
                ? usersInfo.GetValueOrDefault(request.ReviewerId)
                : null,
            AdminComments = request.AdminComments
        };
    }
}
