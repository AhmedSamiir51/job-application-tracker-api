using JobTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobTracker.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("users");
        b.HasKey(x => x.Id);
        b.Property(x => x.Email).HasMaxLength(256).IsRequired();
        b.Property(x => x.FullName).HasMaxLength(150).IsRequired();
        b.Property(x => x.PasswordHash).HasMaxLength(512).IsRequired();
        b.HasIndex(x => x.Email).IsUnique();
    }
}

internal sealed class JobApplicationConfiguration : IEntityTypeConfiguration<JobApplication>
{
    public void Configure(EntityTypeBuilder<JobApplication> b)
    {
        b.ToTable("job_applications");
        b.HasKey(x => x.Id);
        b.Property(x => x.CompanyName).HasMaxLength(150).IsRequired();
        b.Property(x => x.Position).HasMaxLength(150).IsRequired();
        b.Property(x => x.Location).HasMaxLength(150);
        b.Property(x => x.JobUrl).HasMaxLength(2048);
        b.Property(x => x.Currency).HasMaxLength(3);
        b.Property(x => x.SalaryMin).HasPrecision(18, 2);
        b.Property(x => x.SalaryMax).HasPrecision(18, 2);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.WorkMode).HasConversion<string>().HasMaxLength(20);

        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);

        b.HasMany(x => x.Notes).WithOne().HasForeignKey(n => n.JobApplicationId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.History).WithOne().HasForeignKey(h => h.JobApplicationId).OnDelete(DeleteBehavior.Cascade);
        b.Navigation(x => x.Notes).UsePropertyAccessMode(PropertyAccessMode.Field);
        b.Navigation(x => x.History).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Every query is scoped to a user and usually filtered/sorted by these columns.
        b.HasIndex(x => new { x.UserId, x.Status });
        b.HasIndex(x => new { x.UserId, x.CreatedAtUtc });
    }
}

internal sealed class ApplicationNoteConfiguration : IEntityTypeConfiguration<ApplicationNote>
{
    public void Configure(EntityTypeBuilder<ApplicationNote> b)
    {
        b.ToTable("application_notes");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Text).HasMaxLength(2000).IsRequired();
    }
}

internal sealed class StatusChangeConfiguration : IEntityTypeConfiguration<StatusChange>
{
    public void Configure(EntityTypeBuilder<StatusChange> b)
    {
        b.ToTable("status_changes");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.From).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.To).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Comment).HasMaxLength(500);
    }
}
