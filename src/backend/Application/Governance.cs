using BerImpact.Domain;

namespace BerImpact.Application;

public static class Governance
{
    public static void Submit(ChangeRequest change)
    {
        if (change.State != ChangeState.Draft) throw new InvalidOperationException("Only drafts can be submitted. Edit a rejected change to create a new review version.");
        if (string.IsNullOrWhiteSpace(change.RollbackPlan) || string.IsNullOrWhiteSpace(change.SuccessCriteria)) throw new InvalidOperationException("Rollback plan and success criteria are required.");
        change.State = ChangeState.Submitted;
    }

    public static void Decide(ChangeRequest change, string actorId, bool approve, bool reviewRequired)
    {
        if (change.OwnerId == actorId) throw new InvalidOperationException("Change owner cannot approve or reject their own change.");
        if (change.State is not (ChangeState.Submitted or ChangeState.UnderReview)) throw new InvalidOperationException("Change is not awaiting review.");
        if (approve && reviewRequired) throw new InvalidOperationException("Evidence gates are unresolved; approval is blocked.");
        change.State = approve ? ChangeState.Approved : ChangeState.Rejected;
    }

    public static void InvalidateApproval(ChangeRequest change)
    {
        change.Version++;
        change.State = ChangeState.Draft;
        change.UpdatedAt = DateTime.UtcNow;
    }
}
