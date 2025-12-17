using ApprovalRequestsApi.Domain.Entities;
using ApprovalRequestsApi.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApprovalRequestsApi.Infrastructure.Data.Configurations;

public class ApprovalRequestConfiguration : IEntityTypeConfiguration<ApprovalRequest>
{
    public void Configure(EntityTypeBuilder<ApprovalRequest> builder)
    {
        builder.ToTable("approval_requests");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(e => e.Title)
            .HasColumnName("title")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasColumnName("description")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(e => e.RequesterId)
            .HasColumnName("requester_id")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(e => e.RequestedAt)
            .HasColumnName("requested_at")
            .IsRequired();

        builder.Property(e => e.ReviewedAt)
            .HasColumnName("reviewed_at");

        builder.Property(e => e.ReviewerId)
            .HasColumnName("reviewer_id")
            .HasMaxLength(100);

        builder.Property(e => e.AdminComments)
            .HasColumnName("admin_comments")
            .HasColumnType("text");

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        // Índices para búsqueda eficiente
        builder.HasIndex(e => e.RequesterId)
            .HasDatabaseName("idx_requester_id");

        builder.HasIndex(e => e.Status)
            .HasDatabaseName("idx_status");

        builder.HasIndex(e => e.RequestedAt)
            .HasDatabaseName("idx_requested_at");
    }
}
