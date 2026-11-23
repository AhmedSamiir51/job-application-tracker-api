using JobTracker.Domain.Entities;
using JobTracker.Domain.Enums;

namespace JobTracker.Domain.Tests;

public class ApplicationStatusRulesTests
{
    [Theory]
    [InlineData(ApplicationStatus.Wishlist, ApplicationStatus.Applied, true)]
    [InlineData(ApplicationStatus.Applied, ApplicationStatus.PhoneScreen, true)]
    [InlineData(ApplicationStatus.PhoneScreen, ApplicationStatus.Interview, true)]
    [InlineData(ApplicationStatus.Interview, ApplicationStatus.Offer, true)]
    [InlineData(ApplicationStatus.Offer, ApplicationStatus.Accepted, true)]
    [InlineData(ApplicationStatus.Wishlist, ApplicationStatus.Interview, false)]
    [InlineData(ApplicationStatus.Applied, ApplicationStatus.Offer, false)]
    [InlineData(ApplicationStatus.Interview, ApplicationStatus.Applied, false)]
    [InlineData(ApplicationStatus.Rejected, ApplicationStatus.Applied, false)]
    [InlineData(ApplicationStatus.Applied, ApplicationStatus.Applied, false)]
    public void CanTransition_matches_the_pipeline(ApplicationStatus from, ApplicationStatus to, bool expected)
    {
        Assert.Equal(expected, ApplicationStatusRules.CanTransition(from, to));
    }

    [Fact]
    public void Every_status_is_defined_in_the_rules()
    {
        foreach (var status in Enum.GetValues<ApplicationStatus>())
        {
            Assert.NotNull(ApplicationStatusRules.AllowedFrom(status));
        }
    }

    [Fact]
    public void Active_statuses_can_always_be_withdrawn()
    {
        foreach (var status in new[] { ApplicationStatus.Wishlist, ApplicationStatus.Applied, ApplicationStatus.PhoneScreen, ApplicationStatus.Interview, ApplicationStatus.Offer })
        {
            Assert.True(ApplicationStatusRules.CanTransition(status, ApplicationStatus.Withdrawn));
        }
    }
}
