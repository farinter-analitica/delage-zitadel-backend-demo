using ApprovalRequestsApi.Application.DTOs.Requests;
using ApprovalRequestsApi.Application.Interfaces;
using ApprovalRequestsApi.Domain.Entities;
using ApprovalRequestsApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ApprovalRequestsApi.Infrastructure.Repositories;

public class ApprovalRequestRepository : IApprovalRequestRepository
{
    private readonly ApplicationDbContext _context;

    public ApprovalRequestRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApprovalRequest> CreateAsync(ApprovalRequest request)
    {
        request.CreatedAt = DateTime.UtcNow;
        request.UpdatedAt = DateTime.UtcNow;

        _context.ApprovalRequests.Add(request);
        await _context.SaveChangesAsync();
        return request;
    }

    public async Task<(List<ApprovalRequest> Items, int TotalCount)> GetPagedAsync(
        SearchRequestDto searchDto)
    {
        var query = _context.ApprovalRequests.AsQueryable();

        // Aplicar filtros
        if (searchDto.Status.HasValue)
        {
            query = query.Where(r => r.Status == searchDto.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchDto.RequesterId))
        {
            query = query.Where(r => r.RequesterId == searchDto.RequesterId);
        }

        if (!string.IsNullOrWhiteSpace(searchDto.Search))
        {
            var search = searchDto.Search.ToLower();
            query = query.Where(r =>
                r.Title.ToLower().Contains(search) ||
                r.Description.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(r => r.RequestedAt)
            .Skip((searchDto.Page - 1) * searchDto.PageSize)
            .Take(searchDto.PageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<ApprovalRequest?> GetByIdAsync(Guid id)
    {
        return await _context.ApprovalRequests.FindAsync(id);
    }

    public async Task<ApprovalRequest> UpdateAsync(ApprovalRequest request)
    {
        request.UpdatedAt = DateTime.UtcNow;
        _context.Entry(request).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return request;
    }
}
