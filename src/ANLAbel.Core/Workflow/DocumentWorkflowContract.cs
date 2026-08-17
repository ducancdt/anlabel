namespace ANLAbel.Core.Workflow;

/// <summary>Local document-review vocabulary; distinct from print-job/reprint state.</summary>
public enum DocumentWorkflowState { Unknown, Draft, InReview, Approved, Published, Rejected }

public sealed record DocumentWorkflowTransition(
    DocumentWorkflowState From,
    DocumentWorkflowState To,
    string Action,
    bool RequiresComment);

/// <summary>
/// Pure fail-closed transition graph. It deliberately has no actor, persistence,
/// print policy or scheduler responsibility.
/// </summary>
public static class DocumentWorkflowContract
{
    private static readonly DocumentWorkflowTransition[] Transitions =
    [
        new(DocumentWorkflowState.Draft, DocumentWorkflowState.InReview, "Request approval", false),
        new(DocumentWorkflowState.InReview, DocumentWorkflowState.Approved, "Approve", false),
        new(DocumentWorkflowState.InReview, DocumentWorkflowState.Rejected, "Request changes", true),
        new(DocumentWorkflowState.Approved, DocumentWorkflowState.Published, "Publish", false),
        new(DocumentWorkflowState.Rejected, DocumentWorkflowState.Draft, "Return to draft", false),
        new(DocumentWorkflowState.Approved, DocumentWorkflowState.Draft, "Reopen", true),
        new(DocumentWorkflowState.Published, DocumentWorkflowState.Draft, "Create draft revision", true)
    ];

    public static IReadOnlyList<DocumentWorkflowTransition> GetAvailable(DocumentWorkflowState state)
        => Transitions.Where(item => item.From == state).ToArray();

    public static bool TryTransition(DocumentWorkflowState from, DocumentWorkflowState to, string? comment, out string diagnostic)
    {
        var transition = Transitions.FirstOrDefault(item => item.From == from && item.To == to);
        if (transition is null) { diagnostic = $"Workflow transition {from} -> {to} is not allowed."; return false; }
        if (transition.RequiresComment && string.IsNullOrWhiteSpace(comment)) { diagnostic = $"{transition.Action} requires a comment."; return false; }
        diagnostic = string.Empty;
        return true;
    }
}
