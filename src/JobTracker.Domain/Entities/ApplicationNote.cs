namespace JobTracker.Domain.Entities;

public class ApplicationNote
{
    private ApplicationNote() { }

    public Guid Id { get; private set; }
    public Guid JobApplicationId { get; private set; }
    public string Text { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }

    internal static ApplicationNote Create(Guid applicationId, string text, DateTime nowUtc) => new()
    {
        Id = Guid.NewGuid(),
        JobApplicationId = applicationId,
        Text = text,
        CreatedAtUtc = nowUtc
    };
}
