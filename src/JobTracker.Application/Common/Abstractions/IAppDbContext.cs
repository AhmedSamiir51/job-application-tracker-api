using JobTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Application.Common.Abstractions;

public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<JobApplication> JobApplications { get; }
    DbSet<ApplicationNote> Notes { get; }
    DbSet<StatusChange> StatusChanges { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
